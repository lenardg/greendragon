using System.Diagnostics.CodeAnalysis;

namespace GreenDragon.Core.Sonos;

/// <summary>Shares metadata with absolute retention or sliding player-catalog retention.</summary>
public interface ISonosMetadataCache
{
    /// <summary>Reads metadata without upstream work and renews player-catalog idle retention.</summary>
    /// <typeparam name="T">The stored reference type.</typeparam>
    /// <param name="area">The catalog or result category.</param>
    /// <param name="key">The household/source and complete query identity.</param>
    /// <param name="value">The unexpired entry, if present.</param>
    /// <returns>Whether unexpired metadata was found.</returns>
    bool TryGet<T>(string area, string key, [NotNullWhen(true)] out T? value) where T : class;

    /// <summary>Stores successful metadata with the selected expiration policy.</summary>
    /// <typeparam name="T">The stored reference type.</typeparam>
    /// <param name="area">The result category.</param>
    /// <param name="key">The complete scoped identity.</param>
    /// <param name="value">The metadata to retain.</param>
    /// <param name="policy">Absolute metadata retention or 24-hour sliding player-catalog retention.</param>
    void Set<T>(string area, string key, T value, SonosCachePolicy policy = SonosCachePolicy.MetadataAbsolute) where T : class;

    /// <summary>Invalidates metadata after an explicit refresh or mutation.</summary>
    /// <typeparam name="T">The stored reference type.</typeparam>
    /// <param name="area">The result category.</param>
    /// <param name="key">The complete scoped identity.</param>
    void Remove<T>(string area, string key) where T : class;
}
