using GreenDragon.Core;
using GreenDragon.Core.BusinessLogic;
using GreenDragon.Core.Sonos;
using GreenDragon.Web.Configuration;
using GreenDragon.Web.Localization;
using GreenDragon.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GreenDragon.Web.Pages;

/// <summary>Displays speaker snapshots using antiforgery-protected requests.</summary>
/// <param name="sonos">The Core Sonos business boundary.</param>
/// <param name="text">Localized display text.</param>
public sealed class IndexModel(ISonosBL sonos, UiText text) : PageModel
{
    /// <summary>Indicates whether speaker requests are enabled.</summary>
    public bool SonosEnabled => sonos.IsEnabled;

    /// <summary>The delay between completed browser status reads.</summary>
    public int RefreshIntervalSeconds => sonos.RefreshIntervalSeconds;

    /// <summary>Requests one automatic browser load on the initial page visit.</summary>
    public bool AutoLoad { get; private set; }

    /// <summary>Renders the shell without contacting speakers.</summary>
    public void OnGet()
    {
        AutoLoad = SonosEnabled;
    }

    /// <summary>The rooms returned for this request only.</summary>
    public IReadOnlyList<SpeakerSnapshot> Speakers { get; private set; } = [];

    /// <summary>Indicates that discovery completed, including an empty result.</summary>
    public bool HasDiscovered { get; private set; }

    /// <summary>Handles a user-requested discovery; GET never contacts speakers.</summary>
    /// <param name="cancellationToken">Cancels the waiting request on disconnect.</param>
    /// <returns>The page with discovered rooms or a retryable error.</returns>
    /// <param name="rediscover">Replaces discovery when the user presses the rediscovery button.</param>
    public Task<IActionResult> OnPostDiscoverAsync(CancellationToken cancellationToken, bool rediscover = false)
    {
        return LoadAsync(() => sonos.DiscoverAsync(cancellationToken, rediscover));
    }

    /// <summary>Refreshes status using the shared player catalog.</summary>
    /// <param name="cancellationToken">Cancels the caller's wait.</param>
    /// <returns>Updated cards or a retryable error.</returns>
    public Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
    {
        return LoadAsync(() => sonos.RefreshAsync(cancellationToken));
    }

    /// <summary>Binds validated input and delegates control decisions to Core.</summary>
    /// <param name="command">The submitted speaker command.</param>
    /// <param name="cancellationToken">Cancels the caller's wait.</param>
    /// <returns>Updated cards or a safe validation/operation error.</returns>
    public Task<IActionResult> OnPostCommandAsync(SpeakerCommandAIM command, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult(Result());
        }
        return LoadAsync(() => sonos.ExecuteAsync(new SonosCommand(command.SpeakerId, command.Kind,
            command.Kind == SonosCommandKind.SetVolume ? command.Volume : null), cancellationToken));
    }

    private async Task<IActionResult> LoadAsync(Func<Task<IReadOnlyList<SpeakerSnapshot>>> operation)
    {
        try
        {
            Speakers = await operation();
            HasDiscovered = true;
        }
        catch (BusinessLogicException)
        {
            ModelState.AddModelError(string.Empty, text["home.discovery.error"]);
        }
        return Result();
    }

    private IActionResult Result()
    {
        Response.Headers.CacheControl = PortalRequests.NoCacheNoStore;
        return Request.Headers[PortalRequests.RequestedWith] == PortalRequests.Fetch
            ? Partial(PortalRequests.ResultsPartial, this) : Page();
    }
}
