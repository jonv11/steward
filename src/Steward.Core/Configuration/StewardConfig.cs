using YamlDotNet.Serialization;

namespace Steward.Core.Configuration;

public sealed class StewardConfig
{
    [YamlMember(Alias = "profile")]
    public string? Profile { get; set; }

    [YamlMember(Alias = "output")]
    public OutputConfig? Output { get; set; }

    [YamlMember(Alias = "discovery")]
    public DiscoveryConfig? Discovery { get; set; }
}

public sealed class OutputConfig
{
    [YamlMember(Alias = "format")]
    public string? Format { get; set; }

    [YamlMember(Alias = "verbosity")]
    public string? Verbosity { get; set; }

    [YamlMember(Alias = "no_color")]
    public bool? NoColor { get; set; }
}

public sealed class DiscoveryConfig
{
    [YamlMember(Alias = "exclude")]
    public List<string>? Exclude { get; set; }
}
