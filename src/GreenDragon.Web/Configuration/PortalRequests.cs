namespace GreenDragon.Web.Configuration;

/// <summary>Shared identifiers for card fragment requests.</summary>
public static class PortalRequests
{
    /// <summary>The header selecting a fragment representation.</summary>
    public const string RequestedWith = "X-Requested-With";
    /// <summary>The browser fetch representation identifier.</summary>
    public const string Fetch = "fetch";
    /// <summary>The partial that renders card results and errors.</summary>
    public const string ResultsPartial = "_SpeakerResults";
    /// <summary>The selected-player results fragment.</summary>
    public const string PlayerResultsPartial = "_PlayerResults";
    /// <summary>The view-data key selecting player-specific card form routes.</summary>
    public const string PlayerIdKey = "PlayerId";
    /// <summary>Prevents browser caching of live status and antiforgery-bearing fragments.</summary>
    public const string NoCacheNoStore = "no-cache, no-store";
}
