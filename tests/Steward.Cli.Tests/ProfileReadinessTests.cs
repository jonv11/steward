using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ProfileReadinessTests : IDisposable
{
    private readonly string _originalDirectory;
    private readonly List<string> _workingCopies = [];

    public ProfileReadinessTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);

        foreach (var path in _workingCopies)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    public static IEnumerable<object[]> RepresentativeProfiles()
    {
        // Only advertised profiles per ADR-014; mixed/knowledge deferred
        yield return ["docs", "docs-repo", "documentation", new[] { "README.md", "docs/" }, new[] { "README.md" }];
        yield return ["minimal", "minimal-repo", "general", Array.Empty<string>(), Array.Empty<string>()];
    }

    public static IEnumerable<object[]> CheckProfiles()
    {
        yield return ["docs", "docs-repo"];
        yield return ["minimal", "minimal-repo"];
    }

    public static IEnumerable<object[]> FailureCases()
    {
        yield return ["docs", "docs-repo", "docs", "docs/"];
        // minimal profile: README.md is now importance: optional, so removing it does not fail.
    }

    [Theory]
    [MemberData(nameof(RepresentativeProfiles))]
    public void ProfileFixture_InitValidateStatusAndOrient_SurfaceExpectedContract(
        string profile,
        string fixtureName,
        string expectedRepositoryType,
        string[] expectedRequiredArtifacts,
        string[] expectedStartHere)
    {
        InWorkingCopy(fixtureName, _ =>
        {
            var (initExitCode, _, initError) = CliTestHelper.InvokeCapture("init", "--profile", profile);
            initExitCode.Should().Be(0, initError);

            var (validateExitCode, _, validateError) = CliTestHelper.InvokeCapture("config", "validate");
            validateExitCode.Should().Be(0, validateError);

            var (showExitCode, showOutput, showError) = CliTestHelper.InvokeCapture("config", "show", "--effective", "--output", "json");
            showExitCode.Should().Be(0, showError);

            using var showDoc = JsonDocument.Parse(showOutput);
            var showData = showDoc.RootElement.GetProperty("data");
            showData.GetProperty("config").GetProperty("profile").GetString().Should().Be(profile);
            showData.GetProperty("policy").GetProperty("repository").GetProperty("type").GetString().Should().Be(expectedRepositoryType);
            if (expectedRequiredArtifacts.Length > 0)
                GetArtifactPaths(showData.GetProperty("policy").GetProperty("artifacts"))
                    .Should()
                    .Contain(expectedRequiredArtifacts);

            var (statusExitCode, statusOutput, statusError) = CliTestHelper.InvokeCapture("status", "--output", "json");
            statusExitCode.Should().Be(0, statusError);

            using var statusDoc = JsonDocument.Parse(statusOutput);
            var statusData = statusDoc.RootElement.GetProperty("data");
            statusData.GetProperty("profile").GetString().Should().Be(profile);
            GetArtifactPaths(statusData.GetProperty("requiredArtifacts"))
                .Should()
                .BeEquivalentTo(expectedRequiredArtifacts);
            GetStringArray(statusData.GetProperty("startHere"))
                .Should()
                .BeEquivalentTo(expectedStartHere);
            statusData.GetProperty("presentCount").GetInt32().Should().Be(expectedRequiredArtifacts.Length);
            statusData.GetProperty("requiredCount").GetInt32().Should().Be(expectedRequiredArtifacts.Length);

            var (orientExitCode, orientOutput, orientError) = CliTestHelper.InvokeCapture("orient", "--output", "json");
            orientExitCode.Should().Be(0, orientError);

            using var orientDoc = JsonDocument.Parse(orientOutput);
            var orientData = orientDoc.RootElement.GetProperty("data");
            orientData.GetProperty("profile").GetString().Should().Be(profile);
            orientData.GetProperty("repositoryType").GetString().Should().Be(expectedRepositoryType);
            GetStringArray(orientData.GetProperty("startHere"))
                .Should()
                .BeEquivalentTo(expectedStartHere);

            var (doctorExitCode, doctorOutput, doctorError) = CliTestHelper.InvokeCapture("config", "doctor", "--output", "json");
            doctorExitCode.Should().Be(0, doctorError);

            using var doctorDoc = JsonDocument.Parse(doctorOutput);
            doctorDoc.RootElement.GetProperty("data").GetProperty("findings").GetArrayLength().Should().Be(0);
        });
    }

    [Theory]
    [MemberData(nameof(CheckProfiles))]
    public void Check_ProfileFixture_PassesRepresentativeRepository(
        string profile,
        string fixtureName)
    {
        InWorkingCopy(fixtureName, _ =>
        {
            var (initExitCode, _, initError) = CliTestHelper.InvokeCapture("init", "--profile", profile);
            initExitCode.Should().Be(0, initError);

            var (checkExitCode, checkOutput, checkError) = CliTestHelper.InvokeCapture("check", "--output", "json");
            checkExitCode.Should().Be(0, checkError);

            using var checkDoc = JsonDocument.Parse(checkOutput);
            var checkData = checkDoc.RootElement.GetProperty("data");
            checkData.GetProperty("summary").GetProperty("pass").GetBoolean().Should().BeTrue();
            checkData.GetProperty("summary").GetProperty("errors").GetInt32().Should().Be(0);
            checkData.GetProperty("summary").GetProperty("warnings").GetInt32().Should().Be(0);
            checkData.GetProperty("diagnostics").GetArrayLength().Should().Be(0);
        });
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    public void Check_ProfileFixture_WhenKeyArtifactIsMissing_FailsWithRequiredArtifactDiagnostic(
        string profile,
        string fixtureName,
        string pathToRemove,
        string expectedMissingArtifact)
    {
        InWorkingCopy(fixtureName, repoPath =>
        {
            var (initExitCode, _, initError) = CliTestHelper.InvokeCapture("init", "--profile", profile);
            initExitCode.Should().Be(0, initError);

            var fullPath = Path.Combine(repoPath, pathToRemove);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
            }
            else
            {
                File.Delete(fullPath);
            }

            var (checkExitCode, checkOutput, checkError) = CliTestHelper.InvokeCapture("check", "--output", "json");
            checkExitCode.Should().Be(1, checkError);

            using var checkDoc = JsonDocument.Parse(checkOutput);
            var checkData = checkDoc.RootElement.GetProperty("data");
            checkData.GetProperty("summary").GetProperty("pass").GetBoolean().Should().BeFalse();

            checkData.GetProperty("diagnostics")
                .EnumerateArray()
                .Should()
                .Contain(diagnostic =>
                    diagnostic.GetProperty("ruleId").GetString() == "STWD-001" &&
                    diagnostic.GetProperty("path").GetString() == expectedMissingArtifact);
        });
    }

    [Fact]
    public void MinimalProfile_StatusTreatsReadmeAsOptional()
    {
        // ProfileDefaults sets importance: optional explicitly on README.md for the minimal
        // profile, preventing the authoritative role default from making it required.
        InWorkingCopy("minimal-repo", _ =>
        {
            var (initExitCode, _, initError) = CliTestHelper.InvokeCapture("init", "--profile", "minimal");
            initExitCode.Should().Be(0, initError);

            var (_, showOutput, _) = CliTestHelper.InvokeCapture("config", "show", "--effective", "--output", "json");
            using var showDoc = JsonDocument.Parse(showOutput);
            var readmeArtifact = showDoc.RootElement
                .GetProperty("data")
                .GetProperty("policy")
                .GetProperty("artifacts")
                .EnumerateArray()
                .Single(artifact => artifact.GetProperty("path").GetString() == "README.md");

            // required: false and explicit importance: optional
            readmeArtifact.GetProperty("required").GetBoolean().Should().BeFalse();
            readmeArtifact.GetProperty("importance").GetString().Should().Be("optional");

            var (_, statusOutput, _) = CliTestHelper.InvokeCapture("status", "--output", "json");
            using var statusDoc = JsonDocument.Parse(statusOutput);

            // README.md must NOT appear in requiredArtifacts
            GetArtifactPaths(statusDoc.RootElement.GetProperty("data").GetProperty("requiredArtifacts"))
                .Should()
                .NotContain("README.md");
        });
    }

    private void InWorkingCopy(string fixtureName, Action<string> action)
    {
        var workingCopy = Path.Combine(Path.GetTempPath(), "steward-profile-" + Guid.NewGuid().ToString("N")[..8]);
        RepositoryFixture.CopyFixtureTo(fixtureName, workingCopy);
        Directory.CreateDirectory(Path.Combine(workingCopy, ".git"));
        _workingCopies.Add(workingCopy);

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(workingCopy);

        try
        {
            action(workingCopy);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }
    }

    private static string[] GetArtifactPaths(JsonElement artifacts)
    {
        return
        [
            .. artifacts.EnumerateArray()
                .Select(static artifact => artifact.GetProperty("path").GetString())
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .Cast<string>()
        ];
    }

    private static string[] GetStringArray(JsonElement element)
    {
        return
        [
            .. element.EnumerateArray()
                .Select(static item => item.GetString())
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
        ];
    }
}
