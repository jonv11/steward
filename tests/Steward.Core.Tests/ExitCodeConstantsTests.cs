using FluentAssertions;
using Steward.Core;
using Xunit;

namespace Steward.Core.Tests;

public class ExitCodeConstantsTests
{
    [Fact]
    public void Success_IsZero()
    {
        ExitCodes.Success.Should().Be(0);
    }

    [Fact]
    public void ValidationFailure_IsOne()
    {
        ExitCodes.ValidationFailure.Should().Be(1);
    }

    [Fact]
    public void UsageError_IsTwo()
    {
        ExitCodes.UsageError.Should().Be(2);
    }

    [Fact]
    public void InternalError_IsThree()
    {
        ExitCodes.InternalError.Should().Be(3);
    }
}
