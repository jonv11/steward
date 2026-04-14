using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Formatting;
using Xunit;

namespace Steward.Cli.Tests;

public class JsonFormatterTests
{
    [Fact]
    public void WriteObject_ProducesCamelCaseJson()
    {
        var writer = new StringWriter();
        var formatter = new JsonOutputFormatter(writer);

        formatter.WriteObject(new { PropertyName = "value", NestedObject = new { InnerProp = 42 } });

        var json = writer.ToString().Trim();
        json.Should().Contain("\"propertyName\"");
        json.Should().Contain("\"nestedObject\"");
        json.Should().Contain("\"innerProp\"");
    }

    [Fact]
    public void WriteObject_ProducesValidJson()
    {
        var writer = new StringWriter();
        var formatter = new JsonOutputFormatter(writer);

        formatter.WriteObject(new { Name = "test", Count = 5 });

        var json = writer.ToString().Trim();
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void WriteObject_OmitsNullProperties()
    {
        var writer = new StringWriter();
        var formatter = new JsonOutputFormatter(writer);

        formatter.WriteObject(new { Name = "test", Nullable = (string?)null });

        var json = writer.ToString().Trim();
        json.Should().NotContain("\"nullable\"");
    }
}
