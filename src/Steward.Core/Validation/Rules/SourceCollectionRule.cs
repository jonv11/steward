using System.Security.Cryptography;
using System.Text;
using DotNet.Globbing;

namespace Steward.Core.Validation.Rules;

/// <summary>STWD-024: reconstruct immutable source bodies in the declared order.</summary>
public sealed class SourceCollectionRule : IValidationRule
{
    public string RuleId => "STWD-024";
    public string Category => "source-collection";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Source collections must contain exactly their declared parts and reconstruct the recorded SHA-256.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var files = (context.AllDiscoveredFiles ?? context.TargetFiles).Where(f => !f.IsDirectory)
            .Select(f => f.RelativePath).ToHashSet(StringComparer.Ordinal);
        foreach (var collection in context.Policy?.DocumentContracts?.SourceCollections ?? [])
        {
            var parts = collection.Parts!;
            var actual = files.Where(Glob.Parse(collection.PartsPattern!).IsMatch).ToHashSet(StringComparer.Ordinal);
            if (!actual.SetEquals(parts))
            {
                Add($"Source collection '{collection.Id}' has missing or unlisted parts: " +
                    string.Join(", ", actual.Except(parts).Concat(parts.Except(actual)).Order(StringComparer.Ordinal)));
                continue;
            }
            try
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                foreach (var part in parts)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    var content = context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, part));
                    hash.AppendData(Encoding.UTF8.GetBytes(DocumentContractSupport.Body(part, content)));
                }
                var digest = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                if (!string.Equals(digest, collection.Sha256, StringComparison.OrdinalIgnoreCase))
                    Add($"Source collection '{collection.Id}' reconstructs SHA-256 {digest}, expected {collection.Sha256}.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Add($"Cannot reconstruct source collection '{collection.Id}': {ex.Message}");
            }
            void Add(string message) => diagnostics.Add(DocumentContractSupport.Error(RuleId, Category,
                null, message, "Restore the original source bodies and declared part order; verify provenance before changing its digest.",
                $"source:{collection.Id}"));
        }
        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
