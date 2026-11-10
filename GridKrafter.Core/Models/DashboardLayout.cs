namespace GridKrafter.Models;

/// <summary>
/// A breakpoint: from a minimum width up, the dashboard uses this many columns and remembers its
/// own widget positions.
/// </summary>
/// <param name="Id">Identifies the layout in saved placements, so keep it stable.</param>
/// <param name="MinWidth">The narrowest dashboard, in CSS pixels, that uses this layout.</param>
/// <param name="Columns">The number of grid columns.</param>
public sealed record DashboardLayout(string Id, int MinWidth, int Columns)
{
    /// <summary>
    /// Gets the name to show, when the language catalog doesn't have one for <see cref="Id"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>Twelve columns from 1100px, six from 640px and a single column below that.</summary>
    public static IReadOnlyList<DashboardLayout> Defaults { get; } =
    [
        new("large", 1100, 12),
        new("medium", 640, 6),
        new("small", 0, 1),
    ];

    /// <summary>The layouts from widest to narrowest, which is the order positions are derived in.</summary>
    /// <param name="layouts">The layouts, in any order.</param>
    /// <returns>The layouts ordered by <see cref="MinWidth"/>, largest first.</returns>
    public static IReadOnlyList<DashboardLayout> Order(IEnumerable<DashboardLayout> layouts) =>
        [.. layouts.OrderByDescending(l => l.MinWidth).ThenByDescending(l => l.Columns)];

    /// <summary>The layout a dashboard of a given width uses.</summary>
    /// <param name="layouts">The layouts to choose from; when empty, <see cref="Defaults"/>.</param>
    /// <param name="width">The dashboard's width in CSS pixels.</param>
    /// <returns>The widest layout whose <see cref="MinWidth"/> fits, or the narrowest one.</returns>
    public static DashboardLayout ForWidth(IReadOnlyList<DashboardLayout> layouts, double width)
    {
        var ordered = Order(layouts.Count > 0 ? layouts : Defaults);
        return ordered.FirstOrDefault(l => width >= l.MinWidth) ?? ordered[^1];
    }
}
