using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class SearchCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public SearchCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-search-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\n");
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Search_FindsContentMatch()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello\nThis is a test document.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", "test");

        exitCode.Should().Be(0);
        output.Should().Contain("README.md");
        output.Should().Contain("test");
    }

    [Fact]
    public void Search_Regex_FindsPattern()
    {
        File.WriteAllText(Path.Combine(_tempDir, "doc.md"), "# Notes\nVersion 1.2.3 released.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", @"\d+\.\d+\.\d+", "--regex");

        exitCode.Should().Be(0);
        output.Should().Contain("doc.md");
        output.Should().Contain("1.2.3");
    }

    [Fact]
    public void Search_Regex_InvalidPattern_ReturnsError()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello\n");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("search", "[invalid", "--regex");

        exitCode.Should().NotBe(0);
        error.Should().Contain("Invalid regex");
    }

    [Fact]
    public void Search_JsonOutput()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello\nSearchable content.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", "Searchable", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"query\"");
        output.Should().Contain("\"matches\"");
        output.Should().Contain("\"totalMatches\"");
    }

    [Fact]
    public void Search_JsonOutput_ProvidesHandoffFields()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Intro\n\nSearchable content.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", "Searchable", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"sectionHeading\"");
        output.Should().Contain("\"sectionRange\"");
        output.Should().Contain("\"mdQuerySelector\"");
        output.Should().Contain("\"sectionHeading\": \"Intro\"");
        output.Should().Contain("\"mdQuerySelector\": \"#intro\"");
    }

    [Fact]
    public void Search_HeadingsMode_MatchesOnlyHeadings()
    {
        File.WriteAllText(Path.Combine(_tempDir, "doc.md"), "# Goals\nWe have goals here.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", "Goals", "--mode", "headings");

        exitCode.Should().Be(0);
        output.Should().Contain("(heading)");
    }

    [Fact]
    public void Search_RoleOption_IsAccepted()
    {
        // Verify --role is the correct option name (not the old --scope)
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello\nTest content.\n");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        // --role readme should restrict search to artifacts with the readme role
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("search", "Test", "--role", "readme");

        exitCode.Should().Be(0);
    }
}
