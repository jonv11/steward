using Steward.Core.Abstractions;
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
        return Deserialize<RepositoryPolicy>(yaml, path);
    }

    public PathPolicyDocument? LoadPathPolicy(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "path-policy.yaml");
        if (!_fileSystem.FileExists(path)) return null;

        var yaml = _fileSystem.ReadAllText(path);
        return Deserialize<PathPolicyDocument>(yaml, path);
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
        if (string.IsNullOrWhiteSpace(config.Profile))
            return;

        if (ProfileDefaults.GetProfilePolicy(config.Profile) != null)
            return;

        var validProfiles = string.Join(", ", ProfileDefaults.GetValidProfileNames());
        throw new StewardConfigException(
            $"Invalid profile '{config.Profile}' in '{path}'. Valid profiles: {validProfiles}.",
            path);
    }
}
