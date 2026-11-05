using Microsoft.AspNetCore.HttpOverrides;

namespace GreenDragon.Web.Application;

/// <summary>Configures antiforgery, proxy awareness and browser response protections.</summary>
public static class WebSecurity
{
    /// <summary>Registers secure form cookies and trusted-proxy header processing.</summary>
    /// <param name="builder">The web host being composed.</param>
    public static void AddWebSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    /// <summary>Applies the portal's browser security headers.</summary>
    /// <param name="app">The built web host.</param>
    public static void UseWebSecurity(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers[WebSecurityHeaders.ContentSecurityPolicy] = WebSecurityHeaders.Policy;
            context.Response.Headers[WebSecurityHeaders.ContentTypeOptions] = WebSecurityHeaders.NoSniff;
            context.Response.Headers[WebSecurityHeaders.ReferrerPolicy] = WebSecurityHeaders.ReferrerPolicyValue;
            await next(context);
        });
    }
}
