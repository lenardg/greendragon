namespace GreenDragon.Core;

/// <summary>Centralizes limits and conversion rules shared across the application.</summary>
public static class BusinessRules
{
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
