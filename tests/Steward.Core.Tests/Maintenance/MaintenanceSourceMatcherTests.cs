using FluentAssertions;
using Steward.Core.Maintenance;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class MaintenanceSourceMatcherTests
{
    [Fact]
    public void Matches_DirectorySource_MatchesFilesUnderDirectory()
    {
        MaintenanceSourceMatcher.Matches("src/", "src/Program.cs").Should().BeTrue();
        MaintenanceSourceMatcher.Matches("src", "src/Program.cs").Should().BeTrue();
    }

    [Fact]
    public void Matches_ExactFileSource_MatchesOnlyThatFile()
    {
        MaintenanceSourceMatcher.Matches("README.md", "README.md").Should().BeTrue();
        MaintenanceSourceMatcher.Matches("README.md", "README.md/child").Should().BeFalse();
        MaintenanceSourceMatcher.Matches("README.md", "docs/README.md").Should().BeFalse();
    }

    [Fact]
    public void Matches_GlobSource_UsesGlobMatching()
    {
        MaintenanceSourceMatcher.Matches("docs/**/*.md", "docs/guides/setup.md").Should().BeTrue();
        MaintenanceSourceMatcher.Matches("docs/**/*.md", "src/Program.cs").Should().BeFalse();
    }
}
