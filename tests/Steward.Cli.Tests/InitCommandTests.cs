using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class InitCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public InitCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-init-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private (int ExitCode, string Output, string Error) InvokeInit(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(InitCommand.Create());

        var stdOut = new StringWriter();
        var stdErr = new StringWriter();
        var origOut = Console.Out;
        var origErr = Console.Error;
        Console.SetOut(stdOut);
        Console.SetError(stdErr);

        try
        {
            var exitCode = rootCommand.Parse(args).Invoke();
            return (exitCode, stdOut.ToString(), stdErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Init_CreatesConfigAndPolicyFiles()
    {
        var (exitCode, _, _) = InvokeInit("init");

        exitCode.Should().Be(0);
        File.Exists(Path.Combine(_tempDir, ".steward", "config.yaml")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, ".steward", "policy.yaml")).Should().BeTrue();
    }

    [Fact]
    public void Init_SoftwareProfile_ScaffoldsRequiredArtifacts()
    {
        var (exitCode, output, _) = InvokeInit("init", "--profile", "software");

        exitCode.Should().Be(0);
        File.Exists(Path.Combine(_tempDir, "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(_tempDir, "LICENSE")).Should().BeTrue();
        output.Should().Contain("Scaffolded required artifacts");
        output.Should().Contain("README.md");
        output.Should().Contain("LICENSE");
    }

    [Fact]
    public void Init_DoesNotOverwriteExistingArtifacts()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Existing");
        var (exitCode, _, _) = InvokeInit("init", "--profile", "software");

        exitCode.Should().Be(0);
        File.ReadAllText(Path.Combine(_tempDir, "README.md")).Should().Be("# Existing");
    }

    [Fact]
    public void Init_ShowsNextStepGuidance()
    {
        var (exitCode, output, _) = InvokeInit("init");

        exitCode.Should().Be(0);
        output.Should().Contain("Next steps");
        output.Should().Contain("policy.yaml");
        output.Should().Contain("steward check");
    }

    [Fact]
    public void Init_WhenAlreadyInitialized_ReportsError()
    {
        // First init
        InvokeInit("init");

        // Second init in the same directory
        var (exitCode, _, error) = InvokeInit("init");

        exitCode.Should().Be(2); // UsageError
        error.Should().Contain("already exists");
    }

    [Fact]
    public void Init_WithProfile_UsesProfile()
    {
        var (exitCode, output, _) = InvokeInit("init", "--profile", "docs");

        exitCode.Should().Be(0);
        output.Should().Contain("docs");

        var policyContent = File.ReadAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"));
        policyContent.Should().Contain("documentation");
    }

    [Fact]
    public void Init_MinimalProfile_CreatesMinimalPolicy()
    {
        var (exitCode, _, _) = InvokeInit("init", "--profile", "minimal");

        exitCode.Should().Be(0);
        var policyContent = File.ReadAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"));
        policyContent.Should().Contain("README.md");
    }
}
