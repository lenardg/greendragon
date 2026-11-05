using System.Diagnostics.CodeAnalysis;

namespace GreenDragon.Core.Sonos;

/// <summary>Shares stable metadata across requests with absolute, configurable retention.</summary>
public interface ISonosMetadataCache
{
    /// <summary>Reads metadata without starting any upstream work.</summary>
    /// <typeparam name="T">The stored reference type.</typeparam>
    /// <param name="area">The catalog or result category.</param>
    /// <param name="key">The household/source and complete query identity.</param>
    /// <param name="value">The unexpired entry, if present.</param>
    /// <returns>Whether unexpired metadata was found.</returns>
    bool TryGet<T>(string area, string key, [NotNullWhen(true)] out T? value) where T : class;

    /// <summary>Stores a successful result with absolute expiration of at least one hour.</summary>
    /// <typeparam name="T">The stored reference type.</typeparam>
    /// <param name="area">The result category.</param>
    /// <param name="key">The complete scoped identity.</param>
    /// <param name="value">The metadata to retain.</param>
    void Set<T>(string area, string key, T value) where T : class;

    /// <summary>Invalidates metadata after an explicit refresh or mutation.</summary>
    /// <typeparam name="T">The stored reference type.</typeparam>
    /// <param name="area">The result category.</param>
    /// <param name="key">The complete scoped identity.</param>
    void Remove<T>(string area, string key) where T : class;
}
