namespace GridKrafter.Models;

/// <summary>
/// Applies what a person saved to the board as the host defines it now. The host stays in charge
/// of content and limits; the person keeps their arrangement, titles, colours and settings.
/// </summary>
public static class BoardMerge
{
    /// <summary>Copies saved state onto the current board.</summary>
    /// <param name="current">The board as the host defines it; changed in place.</param>
    /// <param name="saved">The board that was saved earlier.</param>
    /// <param name="keep">
    /// Whether a saved item the host no longer defines should stay, typically because it was added
    /// from a catalog whose definition still exists. Without this, such items are dropped.
    /// </param>
    /// <returns><paramref name="current"/>.</returns>
    public static DashboardBoard Apply(DashboardBoard current, DashboardBoard saved, Func<DashboardItem, bool>? keep = null)
    {
        List<DashboardSection> sections = [];
        foreach (var savedSection in saved.Sections)
        {
            var section = current.FindSection(savedSection.Id) ?? savedSection;
            section.Title = savedSection.Title;
            section.Collapsed = savedSection.Collapsed && section.Collapsible;
            sections.Add(section);
        }

        sections.AddRange(current.Sections.Where(s => !sections.Exists(k => string.Equals(k.Id, s.Id, StringComparison.Ordinal))));
        current.Sections = sections;

        foreach (var savedItem in saved.Items)
        {
            var item = current.Find(savedItem.Id);
            if (item is null)
            {
                if (keep?.Invoke(savedItem) == true)
                {
                    current.Items.Add(savedItem);
                }

                continue;
            }

            item.Section = savedItem.Section;
            item.Title = savedItem.Title;
            item.Color = savedItem.Color;
            item.Collapsed = savedItem.Collapsed;
            item.Hidden = savedItem.Hidden;
            item.Placements = savedItem.Placements;
            item.Options = savedItem.Options ?? item.Options;
        }

        return current.Tidy();
    }
}
