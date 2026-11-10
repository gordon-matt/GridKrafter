namespace GridKrafter.Models;

/// <summary>
/// One widget on a dashboard: what it shows, how it looks, and where it sits in each layout.
/// </summary>
public sealed class DashboardItem
{
    /// <summary>
    /// Gets or sets the identifier; unique within the board and stable, since saved state is keyed by it.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the widget definition that renders it, when it comes from a catalog.
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the section it's in; <see langword="null"/> or unknown means the first section.
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// Gets or sets the header text. People can rename widgets, so this is part of their saved state.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a built-in icon name, or CSS classes for an icon font such as <c>fa fa-chart-line</c>.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a palette name from <see cref="WidgetColors"/> or a CSS colour, used for the header.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the header shows.
    /// </summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether it was deleted. Declared widgets are hidden rather than
    /// removed so they can be brought back and don't reappear when the page declares them again.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets where it sits in each layout, keyed by <see cref="DashboardLayout.Id"/>. A layout
    /// without an entry derives the position from the next larger layout.
    /// </summary>
    public Dictionary<string, GridRect> Placements { get; set; } = [];

    /// <summary>
    /// Gets or sets the starting column in the largest layout, before anyone has moved it; <see langword="null"/> finds the first free spot.
    /// </summary>
    public int? X { get; set; }

    /// <summary>
    /// Gets or sets the starting row in the largest layout, before anyone has moved it.
    /// </summary>
    public int? Y { get; set; }

    /// <summary>
    /// Gets or sets the starting width in columns of the largest layout.
    /// </summary>
    public int? W { get; set; }

    /// <summary>
    /// Gets or sets the starting height in rows.
    /// </summary>
    public int? H { get; set; }

    /// <summary>
    /// Gets or sets the minimum width in columns.
    /// </summary>
    public int? MinW { get; set; }

    /// <summary>
    /// Gets or sets the minimum height in rows.
    /// </summary>
    public int? MinH { get; set; }

    /// <summary>
    /// Gets or sets the maximum width in columns.
    /// </summary>
    public int? MaxW { get; set; }

    /// <summary>
    /// Gets or sets the maximum height in rows.
    /// </summary>
    public int? MaxH { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether it stays put: it can't be dragged or resized and other widgets flow around it.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the height follows the content instead of being resizable.
    /// </summary>
    public bool AutoHeight { get; set; }

    /// <summary>
    /// Gets or sets the header buttons, replacing the dashboard's defaults.
    /// </summary>
    public WidgetButtons? Buttons { get; set; }

    /// <summary>
    /// Gets or sets the frame style, replacing the dashboard's default.
    /// </summary>
    public WidgetChrome? Chrome { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the body has no padding, for tables, maps and charts that should reach the edges.
    /// </summary>
    public bool NoPadding { get; set; }

    /// <summary>
    /// Gets or sets the widget's own settings, as edited in its settings panel.
    /// </summary>
    public JsonObject? Options { get; set; }

    /// <summary>
    /// Gets or sets markup to show as the body, for hosts that aren't Blazor. It's trusted as-is, so never put user input here.
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// Gets or sets an address whose HTML is fetched into the body, and fetched again on refresh.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a CSS selector for a <c>&lt;template&gt;</c> on the page whose content becomes the body.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets how often, in seconds, to refresh automatically; zero or <see langword="null"/> never does.
    /// </summary>
    public int? Refresh { get; set; }
}
