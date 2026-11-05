using System.Diagnostics.CodeAnalysis;
using GreenDragon.Core.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GreenDragon.Core.Sonos;

/// <summary>Stores process-local metadata with separate player-catalog and metadata policies.</summary>
/// <param name="cache">The host's shared ASP.NET Core memory cache.</param>
/// <param name="settings">The configured absolute retention for non-catalog metadata.</param>
/// <param name="clock">Provides injectable time for expiration checks.</param>
public sealed class SonosMetadataCache(IMemoryCache cache, IOptions<SonosSettings> settings, TimeProvider clock) : ISonosMetadataCache
{
    /// <inheritdoc />
    public bool TryGet<T>(string area, string key, [NotNullWhen(true)] out T? value) where T : class
    {
        var identity = (typeof(SonosMetadataCache), typeof(T), area, key);
        if (cache.TryGetValue<Entry<T>>(identity, out var entry) && entry is not null)
        {
            lock (entry)
            {
                var now = clock.GetUtcNow();
                if (entry.ExpiresAt > now)
                {
                    if (entry.SlidingLifetime is { } lifetime)
                    {
                        entry.ExpiresAt = now + lifetime;
                    }
                    value = entry.Value;
                    return true;
                }
                cache.Remove(identity);
            }
        }
        value = null;
        return false;
    }

    /// <inheritdoc />
    public void Set<T>(string area, string key, T value, SonosCachePolicy policy = SonosCachePolicy.MetadataAbsolute) where T : class
    {
        var sliding = policy == SonosCachePolicy.PlayerCatalog;
        var duration = sliding ? TimeSpan.FromHours(BusinessRules.Sonos.PlayerCatalogCacheHours)
            : TimeSpan.FromMinutes(Math.Max(settings.Value.MetadataCacheDurationMinutes, BusinessRules.Sonos.MetadataCacheMinutes));
        var options = sliding ? new MemoryCacheEntryOptions { SlidingExpiration = duration }
            : new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };
        cache.Set((typeof(SonosMetadataCache), typeof(T), area, key),
            new Entry<T>(value, clock.GetUtcNow() + duration, sliding ? duration : null), options);
    }

    /// <inheritdoc />
    public void Remove<T>(string area, string key) where T : class
    {
        cache.Remove((typeof(SonosMetadataCache), typeof(T), area, key));
    }

    private sealed class Entry<T>(T value, DateTimeOffset expiresAt, TimeSpan? slidingLifetime)
    {
        /// <summary>The retained metadata.</summary>
        public T Value { get; } = value;
        /// <summary>The absolute deadline or current idle deadline, protected by the entry lock.</summary>
        public DateTimeOffset ExpiresAt { get; set; } = expiresAt;
        /// <summary>The idle duration renewed on reads, or null for absolute expiration.</summary>
        public TimeSpan? SlidingLifetime { get; } = slidingLifetime;
    }
}
