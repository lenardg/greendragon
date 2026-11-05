using System.Text.Json;

namespace GreenDragon.Web.Localization;

/// <summary>Provides the shared English JSON catalog with key fallback for missing translations.</summary>
public sealed class UiText
{
    /// <summary>The supported culture used when browser preferences are unsupported.</summary>
    public const string DefaultCulture = "en";
    private readonly IReadOnlyDictionary<string, string> _translations;

    /// <summary>Loads the portal's shared server/client catalog.</summary>
    /// <param name="environment">Locates the static translation file.</param>
    public UiText(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.WebRootPath, "i18n", $"{DefaultCulture}.json");
        _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("The UI translation catalog could not be loaded.");
    }

    /// <summary>Looks up display text, falling back to its key.</summary>
    /// <param name="key">The dotted component translation key.</param>
    /// <returns>The localized text.</returns>
    public string this[string key]
    {
        get => _translations.GetValueOrDefault(key, key);
    }
}
