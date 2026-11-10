using Microsoft.AspNetCore.Mvc;

namespace GridKrafter.Demo.Mvc.Controllers;

/// <summary>The dashboard page.</summary>
public sealed class HomeController(BoardStore store) : Controller
{
    private static readonly string[] Themes = [Constants.Themes.Auto, Constants.Themes.Light, Constants.Themes.Dark];

    /// <summary>Shows the dashboard with the board saved on the server, if there is one.</summary>
    /// <param name="theme">The theme: <c>auto</c>, <c>light</c> or <c>dark</c>.</param>
    /// <param name="cancellationToken">Cancels loading the saved board.</param>
    /// <returns>The page.</returns>
    [HttpGet("/")]
    public async Task<IActionResult> Index(string? theme, CancellationToken cancellationToken) =>
        View(new DashboardPage(DemoBoard.Json, await store.LoadAsync(cancellationToken), ThemeOrDefault(theme)));

    /// <summary>Forgets the saved board and shows the page's own again.</summary>
    /// <param name="theme">The theme to come back to.</param>
    /// <param name="cancellationToken">Cancels the reset.</param>
    /// <returns>A redirect to the dashboard.</returns>
    [HttpPost("/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(string? theme, CancellationToken cancellationToken)
    {
        await store.ResetAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { theme = ThemeOrDefault(theme) });
    }

    /// <summary>Shown when something goes wrong outside development.</summary>
    /// <returns>The error page.</returns>
    [HttpGet("/error")]
    public IActionResult Error() => View();

    private static string ThemeOrDefault(string? theme) =>
        Themes.FirstOrDefault(t => string.Equals(t, theme, StringComparison.Ordinal)) ?? Constants.Themes.Auto;
}
