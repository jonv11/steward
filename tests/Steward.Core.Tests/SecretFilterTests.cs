using FluentAssertions;
using Steward.Core.Validation;
using Xunit;

namespace Steward.Core.Tests;

public class SecretFilterTests
{
    [Theory]
    [InlineData("key is AKIAIOSFODNN7EXAMPLE here", "key is AKIA***REDACTED*** here")]
    [InlineData("password=SuperSecret123", "password=***REDACTED***")]
    [InlineData("api_key=abc123def456ghi789jkl", "api_key=***REDACTED***")]
    [InlineData("Server=myserver;Password=secret123456;", "Server=myserver;Password=***REDACTED***;")]
    [InlineData("secret=my_secret_value_here", "secret=***REDACTED***")]
    [InlineData("token: abcdef1234567890", "token: ***REDACTED***")]
    [InlineData("API-KEY = 'abcdefghijklmnopqrst'", "API-KEY = ***REDACTED***")]
    [InlineData("passwd: \"abcdefgh\"", "passwd: ***REDACTED***")]
    [InlineData("pwd=abcdefgh;Database=docs;", "pwd=***REDACTED***;Database=docs;")]
    [InlineData("password=abcdefgh", "password=***REDACTED***")]
    [InlineData("password=abcdefgh token: ijklmnop", "password=***REDACTED*** token: ***REDACTED***")]
    public void Redact_RemovesEntireSecretAndPreservesSurroundingText(string input, string expected)
    {
        SecretFilter.ContainsSecret(input).Should().BeTrue();

        var result = SecretFilter.Redact(input);

        result.Should().Be(expected);
        SecretFilter.Redact(result).Should().Be(expected, "redaction should be idempotent");
    }

    [Theory]
    [InlineData("This is a normal log line")]
    [InlineData("version = 1.0.0")]
    [InlineData("README.md")]
    [InlineData("")]
    [InlineData("api_key=abcdefghijklmnopqrs")]
    [InlineData("password=abcdefg")]
    [InlineData("AKIAIOSFODNN7EXAMPL")]
    [InlineData("token:")]
    public void Redact_PreservesContentOutsideDetectionPatterns(string input)
    {
        SecretFilter.ContainsSecret(input).Should().BeFalse();

        var result = SecretFilter.Redact(input);

        result.Should().Be(input);
    }
}
