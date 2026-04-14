namespace Steward.Core.Configuration;

/// <summary>
/// Merges profile defaults into a repository-local policy.
/// Repository-local values always take precedence over profile defaults.
/// </summary>
public static class ProfileMerger
{
    /// <summary>
    /// Returns a policy that applies profile defaults wherever the repo-local policy
    /// does not specify a value. If repoPolicy is null, returns the profile policy as-is.
    /// </summary>
    public static RepositoryPolicy Merge(RepositoryPolicy? repoPolicy, RepositoryPolicy profilePolicy)
    {
        ArgumentNullException.ThrowIfNull(profilePolicy);

        if (repoPolicy == null)
            return profilePolicy;

        return new RepositoryPolicy
        {
            Repository = MergeRepository(repoPolicy.Repository, profilePolicy.Repository),
            Artifacts = repoPolicy.Artifacts ?? profilePolicy.Artifacts,
            Governance = MergeGovernance(repoPolicy.Governance, profilePolicy.Governance),
            Validation = repoPolicy.Validation ?? profilePolicy.Validation,
            Maintenance = repoPolicy.Maintenance ?? profilePolicy.Maintenance
        };
    }

    private static RepositoryInfo? MergeRepository(RepositoryInfo? repo, RepositoryInfo? profile)
    {
        if (profile == null) return repo;
        if (repo == null) return profile;

        return new RepositoryInfo
        {
            Name = repo.Name ?? profile.Name,
            Description = repo.Description ?? profile.Description,
            Type = repo.Type ?? profile.Type,
            Terminology = repo.Terminology ?? profile.Terminology
        };
    }

    private static GovernanceConfig? MergeGovernance(GovernanceConfig? repo, GovernanceConfig? profile)
    {
        if (profile == null) return repo;
        if (repo == null) return profile;

        return new GovernanceConfig
        {
            SectionSizeWarningThreshold = repo.SectionSizeWarningThreshold != 500
                ? repo.SectionSizeWarningThreshold
                : profile.SectionSizeWarningThreshold,
            StartHere = repo.StartHere ?? profile.StartHere,
            Frontmatter = repo.Frontmatter ?? profile.Frontmatter,
            ManagedRegions = repo.ManagedRegions ?? profile.ManagedRegions,
            CompletionPolicy = repo.CompletionPolicy ?? profile.CompletionPolicy
        };
    }
}
