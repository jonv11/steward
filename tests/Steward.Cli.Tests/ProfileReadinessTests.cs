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
        yield return ["docs", "docs-repo", "documentation", new[] { "README.md", "docs/" }, new[] { "README.md" }];
        yield return ["mixed", "mixed-repo", "mixed", new[] { "README.md" }, new[] { "README.md" }];
        yield return ["knowledge", "knowledge-repo", "knowledge", new[] { "README.md" }, new[] { "README.md" }];
        yield return ["minimal", "minimal-repo", "general", new[] { "README.md" }, Array.Empty<string>()];
    }

    public static IEnumerable<object[]> CheckProfiles()
    {
        yield return ["docs", "docs-repo"];
        yield return ["mixed", "mixed-repo"];
        yield return ["knowledge", "knowledge-repo"];
        yield return ["minimal", "minimal-repo"];
    }

    public static IEnumerable<object[]> FailureCases()
    {
        yield return ["docs", "docs-repo", "docs", "docs/"];
        yield return ["mixed", "mixed-repo", "README.md", "README.md"];
        yield return ["knowledge", "knowledge-repo", "README.md", "README.md"];
        yield return ["minimal", "minimal-repo", "README.md", "README.md"];
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
            showDoc.RootElement.GetProperty("config").GetProperty("profile").GetString().Should().Be(profile);
            showDoc.RootElement.GetProperty("policy").GetProperty("repository").GetProperty("type").GetString().Should().Be(expectedRepositoryType);
            GetArtifactPaths(showDoc.RootElement.GetProperty("policy").GetProperty("artifacts"))
                .Should()
                .Contain(expectedRequiredArtifacts);

            var (statusExitCode, statusOutput, statusError) = CliTestHelper.InvokeCapture("status", "--output", "json");
            statusExitCode.Should().Be(0, statusError);

            using var statusDoc = JsonDocument.Parse(statusOutput);
            statusDoc.RootElement.GetProperty("profile").GetString().Should().Be(profile);
            GetArtifactPaths(statusDoc.RootElement.GetProperty("requiredArtifacts"))
                .Should()
                .BeEquivalentTo(expectedRequiredArtifacts);
            GetStringArray(statusDoc.RootElement.GetProperty("startHere"))
                .Should()
                .BeEquivalentTo(expectedStartHere);
            statusDoc.RootElement.GetProperty("presentCount").GetInt32().Should().Be(expectedRequiredArtifacts.Length);
            statusDoc.RootElement.GetProperty("requiredCount").GetInt32().Should().Be(expectedRequiredArtifacts.Length);

            var (orientExitCode, orientOutput, orientError) = CliTestHelper.InvokeCapture("orient", "--output", "json");
            orientExitCode.Should().Be(0, orientError);

            using var orientDoc = JsonDocument.Parse(orientOutput);
            orientDoc.RootElement.GetProperty("profile").GetString().Should().Be(profile);
            orientDoc.RootElement.GetProperty("repositoryType").GetString().Should().Be(expectedRepositoryType);
            GetStringArray(orientDoc.RootElement.GetProperty("startHere"))
                .Should()
                .BeEquivalentTo(expectedStartHere);

            var (doctorExitCode, doctorOutput, doctorError) = CliTestHelper.InvokeCapture("config", "doctor", "--output", "json");
            doctorExitCode.Should().Be(0, doctorError);

            using var doctorDoc = JsonDocument.Parse(doctorOutput);
            doctorDoc.RootElement.GetProperty("findings").GetArrayLength().Should().Be(0);
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
            checkDoc.RootElement.GetProperty("summary").GetProperty("pass").GetBoolean().Should().BeTrue();
            checkDoc.RootElement.GetProperty("summary").GetProperty("errors").GetInt32().Should().Be(0);
            checkDoc.RootElement.GetProperty("summary").GetProperty("warnings").GetInt32().Should().Be(0);
            checkDoc.RootElement.GetProperty("diagnostics").GetArrayLength().Should().Be(0);
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
            checkDoc.RootElement.GetProperty("summary").GetProperty("pass").GetBoolean().Should().BeFalse();

            checkDoc.RootElement.GetProperty("diagnostics")
                .EnumerateArray()
                .Should()
                .Contain(diagnostic =>
                    diagnostic.GetProperty("ruleId").GetString() == "STWD-001" &&
                    diagnostic.GetProperty("path").GetString() == expectedMissingArtifact);
        });
    }

    [Fact]
    public void MinimalProfile_StatusTreatsReadmeAsRequiredViaRoleDefaults()
    {
        InWorkingCopy("minimal-repo", _ =>
        {
            var (initExitCode, _, initError) = CliTestHelper.InvokeCapture("init", "--profile", "minimal");
            initExitCode.Should().Be(0, initError);

            var (_, showOutput, _) = CliTestHelper.InvokeCapture("config", "show", "--effective", "--output", "json");
            using var showDoc = JsonDocument.Parse(showOutput);
            var readmeArtifact = showDoc.RootElement
                .GetProperty("policy")
                .GetProperty("artifacts")
                .EnumerateArray()
                .Single(artifact => artifact.GetProperty("path").GetString() == "README.md");

            readmeArtifact.GetProperty("required").GetBoolean().Should().BeFalse();

            var (_, statusOutput, _) = CliTestHelper.InvokeCapture("status", "--output", "json");
            using var statusDoc = JsonDocument.Parse(statusOutput);

            GetArtifactPaths(statusDoc.RootElement.GetProperty("requiredArtifacts"))
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Be("README.md");
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
