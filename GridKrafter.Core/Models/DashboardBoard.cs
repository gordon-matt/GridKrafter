namespace GridKrafter.Models;

/// <summary>
/// Everything a dashboard shows and everything a person changed about it, in one serializable
/// object. Save it as a whole; <see cref="BoardMerge"/> applies a saved board to a newer definition.
/// </summary>
public sealed class DashboardBoard
{
    /// <summary>
    /// Gets or sets the format version, so older saved boards can be recognised.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the sections in display order. A board without any shows one untitled section.
    /// </summary>
    public List<DashboardSection> Sections { get; set; } = [];

    /// <summary>
    /// Gets or sets the widgets.
    /// </summary>
    public List<DashboardItem> Items { get; set; } = [];

    /// <summary>The item with an id, if there is one.</summary>
    /// <param name="id">The item's id.</param>
    /// <returns>The item, or <see langword="null"/>.</returns>
    public DashboardItem? Find(string? id) =>
        id is null ? null : Items.Find(i => string.Equals(i.Id, id, StringComparison.Ordinal));

    /// <summary>The section with an id, if there is one.</summary>
    /// <param name="id">The section's id.</param>
    /// <returns>The section, or <see langword="null"/>.</returns>
    public DashboardSection? FindSection(string? id) =>
        id is null ? null : Sections.Find(s => string.Equals(s.Id, id, StringComparison.Ordinal));

    /// <summary>The section an item is shown in: its own when that exists, otherwise the first.</summary>
    /// <param name="item">The item.</param>
    /// <returns>A section id.</returns>
    public string SectionOf(DashboardItem item) =>
        FindSection(item.Section)?.Id ?? (Sections.Count > 0 ? Sections[0].Id : Constants.DefaultSection);

    /// <summary>Makes sure there's at least one section and every id is unique, dropping later duplicates.</summary>
    /// <returns>This board.</returns>
    public DashboardBoard Tidy()
    {
        if (Sections.Count == 0)
        {
            Sections.Add(new DashboardSection { Id = Constants.DefaultSection });
        }

        HashSet<string> sections = new(StringComparer.Ordinal);
        Sections.RemoveAll(s => string.IsNullOrWhiteSpace(s.Id) || !sections.Add(s.Id));

        HashSet<string> items = new(StringComparer.Ordinal);
        Items.RemoveAll(i => string.IsNullOrWhiteSpace(i.Id) || !items.Add(i.Id));
        return this;
    }
}
