using GreenDragon.Web.Application;

namespace GreenDragon.Web;

/// <summary>Starts the Razor Pages portal and embedded hosted services.</summary>
public partial class WebProgram
{
    /// <summary>Composes and runs the application.</summary>
    /// <param name="args">Command-line hosting overrides.</param>
    /// <returns>A task completed when the host stops.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddApplication();
        var app = builder.Build();
        app.UseApplication();
        await app.RunAsync();
    }
}
