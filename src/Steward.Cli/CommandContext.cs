using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;

namespace Steward.Cli;

/// <summary>
/// Shared context assembled once during command setup.
/// Commands receive this instead of independently resolving config, policy, and files.
/// </summary>
public sealed class CommandContext
{
    public required string RootPath { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required IOutputFormatter Formatter { get; init; }
    public required OutputFormat OutputFormat { get; init; }
    public required Verbosity Verbosity { get; init; }
    public required bool NoColor { get; init; }
    public string? ConfigDirectory { get; init; }
    public StewardConfig? Config { get; init; }
    public RepositoryPolicy? Policy { get; init; }
    public PathPolicyDocument? PathPolicy { get; init; }
    public IReadOnlyList<DiscoveredFile>? Files { get; init; }
    public IReadOnlyList<string> EffectiveDiscoveryExcludes { get; init; } = [];
}
