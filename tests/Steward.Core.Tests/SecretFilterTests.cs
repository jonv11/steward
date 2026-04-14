using FluentAssertions;
using Steward.Core.Validation;
using Xunit;

namespace Steward.Core.Tests;

public class SecretFilterTests
{
    [Theory]
    [InlineData("AKIAIOSFODNN7EXAMPLE")]
    [InlineData("password=SuperSecret123")]
    [InlineData("api_key=abc123def456ghi789jkl")]
    [InlineData("Server=myserver;Password=secret123456;")]
    [InlineData("secret=my_secret_value_here")]
    [InlineData("token: abcdef1234567890")]
    public void Redact_DetectsSecrets(string input)
    {
        var result = SecretFilter.Redact(input);

        result.Should().Contain("***REDACTED***");
        result.Should().NotBe(input);
    }

    [Theory]
    [InlineData("This is a normal log line")]
    [InlineData("version = 1.0.0")]
    [InlineData("README.md")]
    [InlineData("")]
    public void Redact_PassesCleanContent(string input)
    {
        var result = SecretFilter.Redact(input);

        result.Should().Be(input);
    }

    [Fact]
    public void Redact_AwsKey_PreservesPrefix()
    {
        var input = "key is AKIAIOSFODNN7EXAMPLE here";
        var result = SecretFilter.Redact(input);

        result.Should().Contain("AKIA***REDACTED***");
        result.Should().NotContain("IOSFODNN7EXAMPLE");
    }

    [Fact]
    public void Redact_PasswordField_PreservesLabel()
    {
        var input = "password=SuperSecret123";
        var result = SecretFilter.Redact(input);

        result.Should().Contain("password=***REDACTED***");
        result.Should().NotContain("SuperSecret123");
    }

    [Fact]
    public void ContainsSecret_ReturnsTrueForSecrets()
    {
        SecretFilter.ContainsSecret("password=SuperSecret123").Should().BeTrue();
    }

    [Fact]
    public void ContainsSecret_ReturnsFalseForClean()
    {
        SecretFilter.ContainsSecret("normal text").Should().BeFalse();
    }
}
