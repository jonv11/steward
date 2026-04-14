namespace Steward.Core.Configuration;

public static class ProfileDefaults
{
    public static readonly IReadOnlyDictionary<string, RepositoryPolicy> Profiles = new Dictionary<string, RepositoryPolicy>(StringComparer.OrdinalIgnoreCase)
    {
        ["software"] = new RepositoryPolicy
        {
            Repository = new RepositoryInfo { Type = "software" },
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = true, Description = "Project overview" },
                new() { Path = "LICENSE", Role = "authoritative", Required = true, Description = "License file" },
                new() { Path = "CHANGELOG.md", Role = "changelog", Required = false, Description = "Change log" },
                new() { Path = "CONTRIBUTING.md", Role = "governance", Required = false, Description = "Contribution guidelines" },
            ],
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 500,
                StartHere = ["README.md"]
            }
        },
        ["docs"] = new RepositoryPolicy
        {
            Repository = new RepositoryInfo { Type = "documentation" },
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = true, Description = "Documentation index" },
                new() { Path = "docs/", Role = "documentation", Required = true, Description = "Documentation root" },
            ],
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 300,
                StartHere = ["README.md"]
            }
        },
        ["mixed"] = new RepositoryPolicy
        {
            Repository = new RepositoryInfo { Type = "mixed" },
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = true, Description = "Project overview" },
                new() { Path = "docs/", Role = "documentation", Required = false, Description = "Documentation" },
            ],
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 500,
                StartHere = ["README.md"]
            }
        },
        ["knowledge"] = new RepositoryPolicy
        {
            Repository = new RepositoryInfo { Type = "knowledge" },
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = true, Description = "Knowledge base index" },
            ],
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 1000,
                StartHere = ["README.md"]
            }
        },
        ["minimal"] = new RepositoryPolicy
        {
            Repository = new RepositoryInfo { Type = "general" },
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = false, Description = "Project overview" },
            ],
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 500
            }
        }
    };

    public static StewardConfig CreateDefaultConfig(string profileName)
    {
        return new StewardConfig
        {
            Profile = profileName,
            Discovery = new DiscoveryConfig
            {
                Exclude = ["node_modules/", ".vs/", "*.user"]
            }
        };
    }

    public static RepositoryPolicy? GetProfilePolicy(string profileName)
    {
        return Profiles.TryGetValue(profileName, out var policy) ? policy : null;
    }

    public static IReadOnlyList<string> GetValidProfileNames()
    {
        return Profiles.Keys
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
