using System.ComponentModel.DataAnnotations;
using GreenDragon.Core;
using GreenDragon.Core.BusinessLogic;
using GreenDragon.Core.Sonos;
using GreenDragon.Web.Configuration;
using GreenDragon.Web.Localization;
using GreenDragon.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GreenDragon.Web.Pages;

/// <summary>Displays one discovered room without network activity on GET.</summary>
/// <param name="sonos">The serialized Core Sonos boundary.</param>
/// <param name="text">Shared localized text.</param>
public sealed class PlayerModel(ISonosBL sonos, UiText text) : PageModel
{
    /// <summary>The selected stable player identifier, validated before POST operations.</summary>
    [BindProperty(SupportsGet = true), Required, StringLength(BusinessRules.Sonos.MaximumIdLength)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Whether the browser may request player information.</summary>
    public bool SonosEnabled => sonos.IsEnabled;
    /// <summary>The delay after each completed browser refresh.</summary>
    public int RefreshIntervalSeconds => sonos.RefreshIntervalSeconds;
    /// <summary>Whether the initial shell should load through an antiforgery-protected POST.</summary>
    public bool AutoLoad { get; private set; }
    /// <summary>The selected room and queue returned by this request.</summary>
    public SpeakerDetailsSnapshot? Details { get; private set; }

    /// <summary>Renders the shell without contacting Sonos.</summary>
    /// <returns>The shell, or 404 for an invalid player identifier.</returns>
    public IActionResult OnGet()
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Id))
        {
            return NotFound();
        }
        AutoLoad = SonosEnabled;
        Response.Headers.CacheControl = PortalRequests.NoCacheNoStore;
        return Page();
    }

    /// <summary>Loads the selected room and queue from the existing catalog.</summary>
    /// <param name="cancellationToken">Cancels the request's wait.</param>
    /// <returns>A player fragment or a full page with retryable errors.</returns>
    public Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
    {
        return ModelState.IsValid ? LoadAsync(cancellationToken) : Task.FromResult(Result());
    }

    /// <summary>Validates a command for this room and refreshes its details.</summary>
    /// <param name="command">The server-validated control input.</param>
    /// <param name="cancellationToken">Cancels the request's wait.</param>
    /// <returns>The player page or fragment with updated state or an error.</returns>
    public async Task<IActionResult> OnPostCommandAsync(SpeakerCommandAIM command, CancellationToken cancellationToken)
    {
        if (!string.Equals(command.SpeakerId, Id, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, text["player.command.invalid"]);
        }
        if (!ModelState.IsValid)
        {
            return Result();
        }
        try
        {
            await sonos.ExecuteAsync(new SonosCommand(command.SpeakerId, command.Kind,
                command.Kind == SonosCommandKind.SetVolume ? command.Volume : null), cancellationToken);
        }
        catch (BusinessLogicException)
        {
            ModelState.AddModelError(string.Empty, text["home.discovery.error"]);
            return Result();
        }
        return await LoadAsync(cancellationToken);
    }

    private async Task<IActionResult> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            Details = await sonos.ReadPlayerAsync(Id, cancellationToken);
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
            ? Partial(PortalRequests.PlayerResultsPartial, this) : Page();
    }
}
