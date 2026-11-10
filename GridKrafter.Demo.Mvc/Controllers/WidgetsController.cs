using Microsoft.AspNetCore.Mvc;

namespace GridKrafter.Demo.Mvc.Controllers;

/// <summary>
/// The partial views URL widgets fetch into their body, as JarvisWidgets' <c>data-widget-load</c>
/// did. Refreshing a widget fetches its URL again.
/// </summary>
[Route("widgets")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WidgetsController : Controller
{
    /// <summary>The latest orders.</summary>
    /// <returns>A table.</returns>
    [HttpGet("orders")]
    public IActionResult Orders() => PartialView("_Orders", DemoData.RecentOrders());

    /// <summary>The server's load.</summary>
    /// <returns>A list of meters.</returns>
    [HttpGet("server-load")]
    public IActionResult ServerLoad() => PartialView("_ServerLoad", DemoData.ServerLoad());

    /// <summary>What happened lately.</summary>
    /// <returns>A list, with a script that shows the times in the browser's language.</returns>
    [HttpGet("activity")]
    public IActionResult Activity() => PartialView("_Activity", DemoData.Activity());
}
