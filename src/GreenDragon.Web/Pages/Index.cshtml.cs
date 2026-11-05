using GreenDragon.Core;
using GreenDragon.Core.BusinessLogic;
using GreenDragon.Core.Sonos;
using GreenDragon.Web.Localization;
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
    public async Task<IActionResult> OnPostDiscoverAsync(CancellationToken cancellationToken)
    {
        try
        {
            Speakers = await sonos.DiscoverAsync(cancellationToken);
            HasDiscovered = true;
        }
        catch (BusinessLogicException)
        {
            ModelState.AddModelError(string.Empty, text["home.discovery.error"]);
        }
        return Page();
    }
}
