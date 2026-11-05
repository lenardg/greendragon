using System.Diagnostics.CodeAnalysis;
using GreenDragon.Core.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GreenDragon.Core.Sonos;

/// <summary>Stores process-local metadata without extending its expiration on reads.</summary>
/// <param name="cache">The host's shared ASP.NET Core memory cache.</param>
/// <param name="settings">The configured absolute retention.</param>
/// <param name="clock">Provides injectable time for expiration checks.</param>
public sealed class SonosMetadataCache(IMemoryCache cache, IOptions<SonosSettings> settings, TimeProvider clock) : ISonosMetadataCache
{
    /// <inheritdoc />
    public bool TryGet<T>(string area, string key, [NotNullWhen(true)] out T? value) where T : class
    {
        var identity = (typeof(SonosMetadataCache), typeof(T), area, key);
        if (cache.TryGetValue<Entry<T>>(identity, out var entry) && entry is not null)
        {
            if (entry.ExpiresAt > clock.GetUtcNow())
            {
                value = entry.Value;
                return true;
            }
            cache.Remove(identity);
        }
        value = null;
        return false;
    }

    /// <inheritdoc />
    public void Set<T>(string area, string key, T value) where T : class
    {
        var duration = TimeSpan.FromMinutes(Math.Max(settings.Value.MetadataCacheDurationMinutes,
            BusinessRules.Sonos.MetadataCacheMinutes));
        cache.Set((typeof(SonosMetadataCache), typeof(T), area, key),
            new Entry<T>(value, clock.GetUtcNow() + duration), duration);
    }

    /// <inheritdoc />
    public void Remove<T>(string area, string key) where T : class
    {
        cache.Remove((typeof(SonosMetadataCache), typeof(T), area, key));
    }

    private sealed record Entry<T>(T Value, DateTimeOffset ExpiresAt);
}
