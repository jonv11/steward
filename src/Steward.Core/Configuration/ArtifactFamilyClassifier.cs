using DotNet.Globbing;

namespace Steward.Core.Configuration;

/// <summary>
/// Deterministic file-to-family classification engine.
///
/// Callers are responsible for checking explicit <c>artifacts[]</c> entries first;
/// this classifier applies only to files not already matched by an explicit declaration.
///
/// Within families, declaration order determines precedence: the first matching family wins.
/// A family matches when ALL of its specified <c>match</c> criteria are satisfied (AND semantics).
/// </summary>
public sealed class ArtifactFamilyClassifier
{
    private readonly IReadOnlyList<CompiledFamily> _families;

    public ArtifactFamilyClassifier(IReadOnlyList<ArtifactFamilyDefinition>? families)
    {
        if (families == null || families.Count == 0)
        {
            _families = [];
            return;
        }

        var compiled = new List<CompiledFamily>(families.Count);
        foreach (var def in families)
        {
            if (def.Match == null) continue;

            Glob? pathGlob = null;
            if (!string.IsNullOrWhiteSpace(def.Match.PathPattern))
                pathGlob = Glob.Parse(def.Match.PathPattern);

            compiled.Add(new CompiledFamily(def, pathGlob));
        }
        _families = compiled;
    }

    /// <summary>
    /// Returns the first family whose match criteria are all satisfied, or <c>null</c> if none match.
    /// </summary>
    /// <param name="relativePath">Forward-slash relative path of the file.</param>
    /// <param name="frontmatterFields">Parsed frontmatter fields (key→raw value), or null if no frontmatter.</param>
    public ArtifactFamilyDefinition? Classify(
        string relativePath,
        IReadOnlyDictionary<string, object?>? frontmatterFields)
    {
        foreach (var compiled in _families)
        {
            if (Matches(compiled, relativePath, frontmatterFields))
                return compiled.Definition;
        }
        return null;
    }

    /// <summary>Returns true if any family has a path_pattern that matches the given path.</summary>
    public bool HasFamilyWithPathPattern(string relativePath)
    {
        foreach (var compiled in _families)
        {
            if (compiled.PathGlob != null && compiled.PathGlob.IsMatch(relativePath))
                return true;
        }
        return false;
    }

    public IReadOnlyList<ArtifactFamilyDefinition> AllFamilies =>
        _families.Select(c => c.Definition).ToList();

    private static bool Matches(
        CompiledFamily compiled,
        string relativePath,
        IReadOnlyDictionary<string, object?>? frontmatterFields)
    {
        var match = compiled.Definition.Match!;

        // Path pattern criterion
        if (compiled.PathGlob != null && !compiled.PathGlob.IsMatch(relativePath))
            return false;

        // Frontmatter criteria (all must be satisfied)
        if (match.Frontmatter != null && match.Frontmatter.Count > 0)
        {
            if (frontmatterFields == null)
                return false;

            foreach (var (key, requiredValue) in match.Frontmatter)
            {
                if (!frontmatterFields.TryGetValue(key, out var actualRaw))
                    return false;

                var actual = actualRaw?.ToString();
                if (!string.Equals(actual, requiredValue, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    private sealed record CompiledFamily(ArtifactFamilyDefinition Definition, Glob? PathGlob);
}
