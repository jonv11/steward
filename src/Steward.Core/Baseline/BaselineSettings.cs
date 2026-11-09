using Steward.Core.Configuration;

namespace Steward.Core.Baseline;

/// <summary>
/// Where the baseline lives and whether it applies. The baseline is active when its file exists,
/// unless policy sets <c>baseline.enabled: false</c>. An explicit <c>baseline.path</c> is relative
/// to the repository root; the default is <c>baseline.json</c> inside the configuration directory,
/// so a draft configuration passed with <c>--config</c> carries its own baseline.
/// </summary>
public sealed record BaselineSettings(string FullPath, string DisplayPath, bool? Enabled)
{
    public const string DefaultFileName = "baseline.json";

    public static BaselineSettings Resolve(RepositoryPolicy? policy, string repositoryRoot, string? configDirectory)
    {
        var configured = policy?.Baseline?.Path;
        string fullPath;
        if (!string.IsNullOrWhiteSpace(configured))
            fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, configured));
        else
            fullPath = Path.GetFullPath(Path.Combine(configDirectory ?? Path.Combine(repositoryRoot, ".steward"), DefaultFileName));

        var display = PathHelper.NormalizeSeparators(Path.GetRelativePath(repositoryRoot, fullPath));
        return new BaselineSettings(fullPath, display, policy?.Baseline?.Enabled);
    }

    /// <summary>True when the baseline should be applied: not disabled, and the file exists.</summary>
    public bool IsActive(Abstractions.IFileSystem fileSystem) => Enabled != false && fileSystem.FileExists(FullPath);
}
