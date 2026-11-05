using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GreenDragon.Web.Pages;

/// <summary>Renders a generic error without exposing implementation details.</summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErrorModel : PageModel
{
}
