using GreenDragon.Core.Sonos;
using GreenDragon.ServiceDefaults;

namespace GreenDragon.Web.Application;

/// <summary>Owns application composition and middleware ordering.</summary>
public static class ApplicationSetup
{
    /// <summary>Registers configuration and application services.</summary>
    /// <param name="builder">The web host being composed.</param>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        builder.AddConfiguration();
        builder.AddKeyVault();
        builder.AddServiceDefaults();
        builder.AddApplicationLogging();
        builder.AddWebSecurity();
        builder.AddPages();
        builder.Services.AddSonos(builder.Configuration);
    }

    /// <summary>Configures the ordered request pipeline.</summary>
    /// <param name="app">The built web host.</param>
    public static void UseApplication(this WebApplication app)
    {
        app.UseForwardedHeaders();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseWebSecurity();
        app.UseRouting();
        app.UseRequestLocalization();
        app.MapStaticAssets();
        app.MapDefaultEndpoints();
        app.MapControllers();
        app.MapRazorPages();
    }
}
