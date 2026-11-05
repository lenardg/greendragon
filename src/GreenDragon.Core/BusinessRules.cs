namespace GreenDragon.Core;

/// <summary>Centralizes limits and conversion rules shared across the application.</summary>
public static class BusinessRules
{
    /// <summary>Defaults and limits for browser-driven Sonos operations.</summary>
    public static class Sonos
    {
        /// <summary>Seconds between completed browser refreshes.</summary>
        public const int RefreshIntervalSeconds = 5;
        /// <summary>Shortest supported refresh interval.</summary>
        public const int MinimumRefreshSeconds = 2;
        /// <summary>Longest supported refresh interval.</summary>
        public const int MaximumRefreshSeconds = 60;
        /// <summary>Minimum and default stable metadata retention in minutes.</summary>
        public const int MetadataCacheMinutes = 60;
        /// <summary>Hours of inactivity before the shared player-ID catalog expires.</summary>
        public const int PlayerCatalogCacheHours = 24;
        /// <summary>Lowest room volume percentage.</summary>
        public const int MinimumVolume = 0;
        /// <summary>Highest room volume percentage.</summary>
        public const int MaximumVolume = 100;
        /// <summary>Percentage points changed by volume buttons.</summary>
        public const int VolumeStep = 1;
        /// <summary>Maximum accepted player identifier length.</summary>
        public const int MaximumIdLength = 256;
    }
    /// <summary>Defines the scaffold's example conversion rules.</summary>
    public static class Example
    {
        /// <summary>The lowest physically valid Celsius temperature.</summary>
        public const double AbsoluteZeroCelsius = -273.15;
        /// <summary>The ratio between Fahrenheit and Celsius degree sizes.</summary>
        public const double FahrenheitScale = 1.8;
        /// <summary>The Fahrenheit temperature corresponding to zero Celsius.</summary>
        public const double FahrenheitOffset = 32;
    }
    /// <summary>Bounds waiting work in the single Sonos worker.</summary>
    public const int SonosQueueCapacity = 8;

    /// <summary>Bounds embedded artwork to one mebibyte per group.</summary>
    public const int SonosArtworkMaximumBytes = 1024 * 1024;

    /// <summary>Limits time spent loading optional artwork.</summary>
    public static readonly TimeSpan SonosArtworkTimeout = TimeSpan.FromSeconds(5);
}
