using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Maintenance;
using Steward.Core.Validation;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Steward.Core.Configuration;

public sealed class ConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private readonly IFileSystem _fileSystem;

    public ConfigLoader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string? FindConfigDirectory(string startPath, string? overridePath = null)
    {
        if (overridePath != null)
        {
            return _fileSystem.DirectoryExists(overridePath) ? overridePath : null;
        }

        var current = startPath;
        while (current != null)
        {
            var stewardDir = Path.Combine(current, ".steward");
            if (_fileSystem.DirectoryExists(stewardDir))
                return stewardDir;

            // Stop at repository root
            var gitDir = Path.Combine(current, ".git");
            if (_fileSystem.DirectoryExists(gitDir))
                return null;

            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent;
        }

        return null;
    }

    public StewardConfig? LoadConfig(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "config.yaml");
        if (!_fileSystem.FileExists(path)) return null;

        var yaml = _fileSystem.ReadAllText(path);
        var config = Deserialize<StewardConfig>(yaml, path);
        ValidateConfig(config, path);
        return config;
    }

    public RepositoryPolicy? LoadPolicy(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "policy.yaml");
        if (!_fileSystem.FileExists(path)) return null;

        var yaml = _fileSystem.ReadAllText(path);
        var policy = Deserialize<RepositoryPolicy>(yaml, path);
        ValidatePolicy(policy, path);
        return policy;
    }

    public PathPolicyDocument? LoadPathPolicy(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "path-policy.yaml");
        if (!_fileSystem.FileExists(path)) return null;

        var yaml = _fileSystem.ReadAllText(path);
        var pathPolicy = Deserialize<PathPolicyDocument>(yaml, path);
        ValidatePathPolicy(pathPolicy, path);
        return pathPolicy;
    }

    public static string SerializeConfig(StewardConfig config) => Serializer.Serialize(config);
    public static string SerializePolicy(RepositoryPolicy policy) => Serializer.Serialize(policy);

    private static T Deserialize<T>(string yaml, string path)
    {
        try
        {
            return Deserializer.Deserialize<T>(yaml);
        }
        catch (YamlException ex)
        {
            throw new StewardConfigException($"Failed to parse '{path}': {ex.Message}", path, ex);
        }
    }

    private static void ValidateConfig(StewardConfig config, string path)
    {
        if (!string.IsNullOrWhiteSpace(config.Profile) &&
            ProfileDefaults.GetProfilePolicy(config.Profile) == null)
        {
            var validProfiles = string.Join(", ", ProfileDefaults.GetValidProfileNames());
            throw new StewardConfigException(
                $"Invalid profile '{config.Profile}' in '{path}'. Valid profiles: {validProfiles}.",
                path);
        }

        if (!string.IsNullOrWhiteSpace(config.Output?.Format) &&
            !Enum.TryParse<OutputFormat>(config.Output.Format, ignoreCase: true, out _))
        {
            throw new StewardConfigException(
                $"Invalid output.format '{config.Output.Format}' in '{path}'. Valid values: text, json.",
                path);
        }

        if (!string.IsNullOrWhiteSpace(config.Output?.Verbosity) &&
            !Enum.TryParse<Verbosity>(config.Output.Verbosity, ignoreCase: true, out _))
        {
            throw new StewardConfigException(
                $"Invalid output.verbosity '{config.Output.Verbosity}' in '{path}'. Valid values: quiet, normal, verbose, debug.",
                path);
        }
    }

    private static void ValidatePolicy(RepositoryPolicy policy, string path)
    {
        var validRules = new HashSet<string>(
            RuleRegistry.CreateAllRules().Select(static rule => rule.RuleId),
            StringComparer.OrdinalIgnoreCase);
        var validMaintainerTypes = new HashSet<string>(
            MaintenanceEngine.GetSupportedArtifactTypes(),
            StringComparer.OrdinalIgnoreCase);
        var maintenanceArtifactIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var artifact in policy.Artifacts ?? [])
        {
            if (!string.IsNullOrWhiteSpace(artifact.Importance) &&
                artifact.Importance is not ("required" or "recommended" or "optional"))
            {
                throw new StewardConfigException(
                    $"Invalid artifact importance '{artifact.Importance}' in '{path}'. Valid values: required, recommended, optional.",
                    path);
            }

            if (artifact.Freshness is { MaxAgeDays: <= 0 })
            {
                throw new StewardConfigException(
                    $"Artifact freshness.max_age_days must be greater than zero in '{path}'.",
                    path);
            }
        }

        foreach (var ruleId in policy.Validation?.DisabledRules ?? [])
            EnsureRuleIdExists(ruleId, validRules, path, "validation.disabled_rules");

        foreach (var (ruleId, severity) in policy.Validation?.SeverityOverrides ?? [])
        {
            EnsureRuleIdExists(ruleId, validRules, path, "validation.severity_overrides");
            if (!string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(severity, "warning", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(severity, "info", StringComparison.OrdinalIgnoreCase))
            {
                throw new StewardConfigException(
                    $"Invalid severity override '{severity}' for rule '{ruleId}' in '{path}'. Valid values: error, warning, info.",
                    path);
            }
        }

        foreach (var pathOverride in policy.Validation?.PathOverrides ?? [])
        {
            EnsureValidGlob(pathOverride.Pattern, path, "validation.path_overrides[].pattern");
            foreach (var ruleId in pathOverride.DisabledRules ?? [])
                EnsureRuleIdExists(ruleId, validRules, path, "validation.path_overrides[].disabled_rules");
        }

        foreach (var requirement in policy.Validation?.FrontmatterRequirements ?? [])
        {
            EnsureValidGlob(requirement.Pattern, path, "validation.frontmatter_requirements[].pattern");
            foreach (var field in requirement.RequiredFields ?? [])
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    throw new StewardConfigException(
                        $"validation.frontmatter_requirements[].required_fields entries must not be blank in '{path}'.",
                        path);
                }
            }
        }

        foreach (var completionRule in policy.Governance?.CompletionPolicy?.Rules ?? [])
        {
            if (string.IsNullOrWhiteSpace(completionRule.Id))
            {
                throw new StewardConfigException(
                    $"governance.completion_policy.rules[].id must not be blank in '{path}'.",
                    path);
            }

            EnsureRuleIdExists(completionRule.Id, validRules, path, "governance.completion_policy.rules");
        }

        foreach (var artifact in policy.Maintenance?.Artifacts ?? [])
        {
            if (string.IsNullOrWhiteSpace(artifact.Id))
            {
                throw new StewardConfigException(
                    $"maintenance.artifacts[].id must not be blank in '{path}'.",
                    path);
            }

            if (!maintenanceArtifactIds.Add(artifact.Id))
            {
                throw new StewardConfigException(
                    $"Duplicate maintenance artifact id '{artifact.Id}' in '{path}'.",
                    path);
            }

            if (string.IsNullOrWhiteSpace(artifact.Type) || !validMaintainerTypes.Contains(artifact.Type))
            {
                var supported = string.Join(", ", validMaintainerTypes.OrderBy(static type => type, StringComparer.OrdinalIgnoreCase));
                throw new StewardConfigException(
                    $"Invalid maintenance artifact type '{artifact.Type ?? "(blank)"}' in '{path}'. Supported types: {supported}.",
                    path);
            }

            if (!string.IsNullOrWhiteSpace(artifact.Source))
                EnsureValidGlob(artifact.Source, path, $"maintenance artifact '{artifact.Id}' source");

            foreach (var exclude in artifact.Options?.Exclude ?? [])
                EnsureValidGlob(exclude, path, $"maintenance artifact '{artifact.Id}' options.exclude");
        }

        foreach (var artifact in policy.Maintenance?.Artifacts ?? [])
        {
            foreach (var dependencyId in artifact.DependsOn ?? [])
            {
                if (!maintenanceArtifactIds.Contains(dependencyId))
                {
                    throw new StewardConfigException(
                        $"Maintenance artifact '{artifact.Id}' depends_on '{dependencyId}', but no maintenance artifact with that id exists in '{path}'.",
                        path);
                }
            }
        }

        ValidateArtifactFamilies(policy, path);
    }

    private static void ValidateArtifactFamilies(RepositoryPolicy policy, string path)
    {
        var families = policy.ArtifactFamilies;
        if (families == null || families.Count == 0)
            return;

        var seenFamilyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var family in families)
        {
            if (string.IsNullOrWhiteSpace(family.Family))
            {
                throw new StewardConfigException(
                    $"artifact_families entries must declare a non-blank 'family' name in '{path}'.",
                    path);
            }

            if (!seenFamilyNames.Add(family.Family))
            {
                throw new StewardConfigException(
                    $"Duplicate artifact family name '{family.Family}' in '{path}'. Family names must be unique.",
                    path);
            }

            if (family.Match == null)
            {
                throw new StewardConfigException(
                    $"Artifact family '{family.Family}' must declare a 'match' section in '{path}'.",
                    path);
            }

            var hasPathPattern = !string.IsNullOrWhiteSpace(family.Match.PathPattern);
            var hasFrontmatter = family.Match.Frontmatter is { Count: > 0 };

            if (!hasPathPattern && !hasFrontmatter)
            {
                throw new StewardConfigException(
                    $"Artifact family '{family.Family}' match section must declare at least one of 'path_pattern' or 'frontmatter' in '{path}'.",
                    path);
            }

            if (hasPathPattern)
                EnsureValidGlob(family.Match.PathPattern, path, $"artifact_families['{family.Family}'].match.path_pattern");

            if (!string.IsNullOrWhiteSpace(family.Importance) &&
                family.Importance is not ("required" or "recommended" or "optional"))
            {
                throw new StewardConfigException(
                    $"Invalid importance '{family.Importance}' for artifact family '{family.Family}' in '{path}'. Valid values: required, recommended, optional.",
                    path);
            }

            if (family.FrontmatterSchema?.Required != null)
            {
                foreach (var field in family.FrontmatterSchema.Required)
                {
                    if (string.IsNullOrWhiteSpace(field))
                    {
                        throw new StewardConfigException(
                            $"artifact_families['{family.Family}'].frontmatter_schema.required entries must not be blank in '{path}'.",
                            path);
                    }
                }
            }

            if (family.FrontmatterSchema?.AllowedValues != null)
            {
                foreach (var key in family.FrontmatterSchema.AllowedValues.Keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        throw new StewardConfigException(
                            $"artifact_families['{family.Family}'].frontmatter_schema.allowed_values keys must not be blank in '{path}'.",
                            path);
                    }
                }
            }
        }
    }

    private static void ValidatePathPolicy(PathPolicyDocument pathPolicy, string path)
    {
        foreach (var ruleset in pathPolicy.Rulesets ?? [])
        {
            foreach (var rule in ruleset.Rules ?? [])
            {
                if (string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    throw new StewardConfigException(
                        $"path-policy rules must declare a non-empty pattern in '{path}'.",
                        path);
                }

                if (!rule.Exact)
                    EnsureValidGlob(rule.Pattern, path, "path-policy.rules[].pattern");

                if (!string.IsNullOrWhiteSpace(rule.MustMatch))
                {
                    try
                    {
                        _ = new Regex(rule.MustMatch, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                    }
                    catch (ArgumentException ex)
                    {
                        throw new StewardConfigException(
                            $"Invalid path-policy must_match regex '{rule.MustMatch}' in '{path}': {ex.Message}",
                            path,
                            ex);
                    }
                }
            }
        }
    }

    private static void EnsureRuleIdExists(string? ruleId, HashSet<string> validRules, string path, string settingName)
    {
        if (string.IsNullOrWhiteSpace(ruleId) || !validRules.Contains(ruleId))
        {
            var valid = string.Join(", ", validRules.OrderBy(static rule => rule, StringComparer.OrdinalIgnoreCase));
            throw new StewardConfigException(
                $"Unknown rule id '{ruleId ?? "(blank)"}' in {settingName} of '{path}'. Valid rule ids: {valid}.",
                path);
        }
    }

    private static void EnsureValidGlob(string? pattern, string path, string settingName)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new StewardConfigException(
                $"{settingName} must not be blank in '{path}'.",
                path);
        }

        try
        {
            _ = Glob.Parse(pattern);
        }
        catch (Exception ex)
        {
            throw new StewardConfigException(
                $"Invalid glob '{pattern}' in {settingName} of '{path}': {ex.Message}",
                path,
                ex);
        }

        // DotNet.Glob silently accepts malformed character classes (e.g. "[invalid").
        // Validate structural correctness manually for the cases the library ignores.
        ValidateGlobStructure(pattern, path, settingName);
    }

    private static void ValidateGlobStructure(string pattern, string path, string settingName)
    {
        var depth = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '[') depth++;
            else if (pattern[i] == ']') depth = 0;
        }
        if (depth > 0)
            throw new StewardConfigException(
                $"Invalid glob '{pattern}' in {settingName} of '{path}': unclosed '[' character class.",
                path);
    }
}
