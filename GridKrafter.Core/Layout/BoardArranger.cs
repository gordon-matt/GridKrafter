namespace GridKrafter.Layout;

/// <summary>
/// Works out where a section's widgets sit in one layout. A layout only stores positions once
/// someone arranges widgets in it; until then they're derived from the next larger layout, so the
/// phone layout keeps following the desktop one until it's been customised.
/// </summary>
public static class BoardArranger
{
    /// <summary>The section's nodes for a layout, from its saved placements or derived from a larger layout.</summary>
    /// <param name="items">The section's visible items.</param>
    /// <param name="layouts">All the dashboard's layouts.</param>
    /// <param name="layout">The layout to arrange.</param>
    /// <param name="compaction">The dashboard's compaction.</param>
    /// <param name="describe">
    /// Turns an item into a node with its limits; the node's rectangle is the item's starting
    /// position (<see cref="DashboardItem.X"/>, <see cref="DashboardItem.Y"/>) and size.
    /// </param>
    /// <returns>A valid layout of every item.</returns>
    public static IReadOnlyList<GridNode> Arrange(
        IReadOnlyList<DashboardItem> items,
        IReadOnlyList<DashboardLayout> layouts,
        DashboardLayout layout,
        GridCompaction compaction,
        Func<DashboardItem, GridNode> describe)
    {
        var ordered = DashboardLayout.Order(layouts).ToList();
        int index = ordered.FindIndex(l => string.Equals(l.Id, layout.Id, StringComparison.Ordinal));
        if (index < 0)
        {
            ordered.Insert(0, layout);
            index = 0;
        }

        return ArrangeAt(items, ordered, index, compaction, describe);
    }

    private static IReadOnlyList<GridNode> ArrangeAt(
        IReadOnlyList<DashboardItem> items,
        List<DashboardLayout> ordered,
        int index,
        GridCompaction compaction,
        Func<DashboardItem, GridNode> describe)
    {
        var layout = ordered[index];
        var engine = new GridEngine(layout.Columns, compaction);
        List<GridNode> placed = [];
        List<DashboardItem> missing = [];
        foreach (var item in items)
        {
            if (item.Placements.TryGetValue(layout.Id, out var rect))
            {
                placed.Add(describe(item) with { Rect = rect });
            }
            else
            {
                missing.Add(item);
            }
        }

        if (missing.Count == 0)
        {
            return engine.Normalize(placed);
        }

        if (index == 0)
        {
            var nodes = engine.Normalize([.. placed, .. missing.Where(HasPosition).Select(describe)]);
            foreach (var item in missing.Where(i => !HasPosition(i)))
            {
                nodes = engine.Add(nodes, describe(item));
            }

            return nodes;
        }

        var parent = ArrangeAt(items, ordered, index - 1, compaction, describe);
        int parentColumns = ordered[index - 1].Columns;
        if (placed.Count == 0)
        {
            return engine.Project(parent, parentColumns);
        }

        HashSet<string> unplaced = new(missing.Select(i => i.Id), StringComparer.Ordinal);
        double ratio = (double)layout.Columns / parentColumns;
        var result = engine.Normalize(placed);
        foreach (var node in parent.OrderBy(n => n.Rect.Y).ThenBy(n => n.Rect.X).Where(n => unplaced.Contains(n.Id)))
        {
            int w = Math.Max(1, (int)Math.Round(node.Rect.W * ratio, MidpointRounding.AwayFromZero));
            result = engine.Add(result, node with { Rect = node.Rect with { W = w } });
        }

        return result;
    }

    private static bool HasPosition(DashboardItem item) => item.X is not null && item.Y is not null;
}
