using GreenDragon.Web.Localization;

namespace GreenDragon.Web.Application;

/// <summary>Registers the Razor Pages UI, English localization and infrastructure controller.</summary>
public static class PagesSetup
{
    /// <summary>Adds the portal rendering services.</summary>
    /// <param name="builder">The web host being composed.</param>
    public static void AddPages(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        builder.Services.AddControllers();
        builder.Services.AddSingleton<UiText>();
        builder.Services.Configure<RequestLocalizationOptions>(options =>
            options.SetDefaultCulture(UiText.DefaultCulture)
                .AddSupportedCultures(UiText.DefaultCulture)
                .AddSupportedUICultures(UiText.DefaultCulture));
    }
}
