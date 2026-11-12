using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Schema;
using Steward.Cli.Commands;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// <c>steward schema</c>, and the contract it publishes: real <c>--output json</c> output must match
/// the embedded schemas, so a field added without its schema fails here.
/// </summary>
[Collection("Console")]
public class SchemaCommandTests : IDisposable
{
    private static readonly Lazy<EvaluationOptions> Options = new(LoadSchemas);

    private readonly string _tempDir;
    private readonly string _origDir;

    public SchemaCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-schema-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        try { Directory.Delete(_tempDir, true); } catch (IOException) { }
    }

    [Fact]
    public void Schema_WithoutArgument_ListsEveryPublishedSchema()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("schema");

        exitCode.Should().Be(0);
        output.Should().Be(string.Join("", SchemaCommand.Schemas.Select(s => $"{s.Command}\t{s.File}\n")));
    }

    [Fact]
    public void Schema_Json_ListMatchesItsOwnSchema()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("schema", "--output", "json");

        exitCode.Should().Be(0);
        Problems("schema.schema.json", output).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(SchemaFiles))]
    public void EveryEmbeddedSchema_IsAValidDraft202012Schema(string file)
    {
        var node = JsonNode.Parse(SchemaCommand.Read(file))!;

        MetaSchemas.Draft202012.Evaluate(node).IsValid.Should().BeTrue();
        node["$id"]!.GetValue<string>().Should().EndWith("/schemas/v1/" + file);
    }

    [Fact]
    public void Schema_Check_PrintsTheSchemaDocument()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("schema", "check");

        exitCode.Should().Be(0);
        output.Should().Be(SchemaCommand.Read("check.schema.json"));
    }

    [Fact]
    public void Schema_UnknownCommand_IsAUsageErrorThatMatchesTheErrorShape()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("schema", "nope", "--output", "json");

        exitCode.Should().Be(2);
        Problems("schema.schema.json", output).Should().BeEmpty();
        JsonNode.Parse(output)!["data"]!["error"]!["kind"]!.GetValue<string>().Should().Be("unknown-schema");
    }

    [Fact]
    public void FailingCheck_MatchesTheCheckSchema()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(1);
        Problems("check.schema.json", output).Should().BeEmpty();
    }

    [Fact]
    public void CheckSchema_RejectsAnUndocumentedDiagnosticField()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");
        var envelope = JsonNode.Parse(output)!;
        envelope["data"]!["diagnostics"]![0]!["filePath"] = "README.md";

        Problems("check.schema.json", envelope.ToJsonString()).Should().NotBeEmpty();
    }

    [Fact]
    public void PassingCheck_MatchesTheCheckSchema()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(0);
        Problems("check.schema.json", output).Should().BeEmpty();
    }

    [Fact]
    public void CheckWithBaselineDrift_MatchesTheCheckSchema()
    {
        CliTestHelper.InvokeCapture("baseline", "generate").ExitCode.Should().Be(0);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n");

        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        var data = JsonNode.Parse(output)!["data"]!;
        data["summary"]!["baseline"].Should().NotBeNull();
        data["baselineDrift"]!.AsArray().Should().NotBeEmpty();
        Problems("check.schema.json", output).Should().BeEmpty();
    }

    [Fact]
    public void CheckConfigurationError_MatchesTheCheckSchema()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), "artifacts: [}\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(2);
        JsonNode.Parse(output)!["data"]!["error"]!["kind"]!.GetValue<string>().Should().Be("configuration-error");
        Problems("check.schema.json", output).Should().BeEmpty();
    }

    [Fact]
    public void UsageError_MatchesTheCommonEnvelope()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--scope", "nonsense", "--output", "json");

        exitCode.Should().Be(2);
        var envelope = JsonNode.Parse(output)!;
        envelope["command"]!.GetValue<string>().Should().Be("steward");
        Problems("common.schema.json#/$defs/envelope", output).Should().BeEmpty();
        Problems("common.schema.json#/$defs/errorData", envelope["data"]!.ToJsonString()).Should().BeEmpty();
    }

    [Fact]
    public void Completions_MatchesTheCompletionsSchema()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("completions", "zsh", "--output", "json");

        Problems("completions.schema.json", output).Should().BeEmpty();
    }

    [Fact]
    public void Version_MatchesTheVersionSchema()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("version", "--output", "json");

        Problems("version.schema.json", output).Should().BeEmpty();
    }

    public static TheoryData<string> SchemaFiles()
    {
        var data = new TheoryData<string>();
        foreach (var (_, file) in SchemaCommand.Schemas)
            data.Add(file);
        return data;
    }

    private static IReadOnlyList<string> Problems(string file, string output)
    {
        var options = Options.Value;
        var schema = file.Contains('#', StringComparison.Ordinal)
            ? JsonSchema.FromText($$"""{ "$ref": "https://steward.invalid/schemas/v1/{{file}}" }""")
            : (JsonSchema)options.SchemaRegistry.Get(new Uri("https://steward.invalid/schemas/v1/" + file))!;
        var result = schema.Evaluate(JsonNode.Parse(output), options);
        return result.IsValid
            ? []
            : [.. (result.Details ?? [])
                .Where(static d => d.Errors is { Count: > 0 })
                .SelectMany(static d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Key}: {e.Value}"))
                .Distinct()];
    }

    private static EvaluationOptions LoadSchemas()
    {
        var options = new EvaluationOptions { OutputFormat = OutputFormat.List };
        foreach (var (_, file) in SchemaCommand.Schemas)
            options.SchemaRegistry.Register(JsonSchema.FromText(SchemaCommand.Read(file)));
        return options;
    }
}
