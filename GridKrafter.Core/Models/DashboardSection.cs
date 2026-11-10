namespace GridKrafter.Models;

/// <summary>A titled band of the dashboard with its own grid. Widgets can be dragged between sections.</summary>
public sealed class DashboardSection
{
    /// <summary>
    /// Gets or sets the identifier items use to say they belong here.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the heading. A section without one has no header outside edit mode.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a built-in icon name, or CSS classes for an icon font.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the header shows.
    /// </summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether people can collapse the section.
    /// </summary>
    public bool Collapsible { get; set; } = true;
}
