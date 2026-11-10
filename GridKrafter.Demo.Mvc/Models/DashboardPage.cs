namespace GridKrafter.Demo.Mvc.Models;

/// <summary>What the dashboard view needs.</summary>
/// <param name="Board">The board as the page defines it, as JSON.</param>
/// <param name="Saved">The board saved on the server, as JSON, or <see langword="null"/>.</param>
/// <param name="Theme">The theme: <c>auto</c>, <c>light</c> or <c>dark</c>.</param>
public sealed record DashboardPage(string Board, string? Saved, string Theme);
