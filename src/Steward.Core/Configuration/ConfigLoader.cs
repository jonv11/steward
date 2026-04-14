using Steward.Core.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Steward.Core.Configuration;

public sealed class ConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
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
        return Deserializer.Deserialize<StewardConfig>(yaml);
    }

    public RepositoryPolicy? LoadPolicy(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "policy.yaml");
        if (!_fileSystem.FileExists(path)) return null;

        var yaml = _fileSystem.ReadAllText(path);
        return Deserializer.Deserialize<RepositoryPolicy>(yaml);
    }

    public PathPolicyDocument? LoadPathPolicy(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "path-policy.yaml");
        if (!_fileSystem.FileExists(path)) return null;

        var yaml = _fileSystem.ReadAllText(path);
        return Deserializer.Deserialize<PathPolicyDocument>(yaml);
    }

    public static string SerializeConfig(StewardConfig config) => Serializer.Serialize(config);
    public static string SerializePolicy(RepositoryPolicy policy) => Serializer.Serialize(policy);
}
