namespace GreenDragon.Web.Application;

/// <summary>Defines the browser protection headers applied to portal responses.</summary>
public static class WebSecurityHeaders
{
    /// <summary>The content security policy header name.</summary>
    public const string ContentSecurityPolicy = "Content-Security-Policy";
    /// <summary>Restricts scripts, framing, objects and form submission to trusted origins.</summary>
    public const string Policy = "default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; style-src 'self' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com";
    /// <summary>The MIME sniffing protection header name.</summary>
    public const string ContentTypeOptions = "X-Content-Type-Options";
    /// <summary>Disables MIME sniffing.</summary>
    public const string NoSniff = "nosniff";
    /// <summary>The outgoing referrer policy header name.</summary>
    public const string ReferrerPolicy = "Referrer-Policy";
    /// <summary>Limits cross-origin referrer information.</summary>
    public const string ReferrerPolicyValue = "strict-origin-when-cross-origin";
}
