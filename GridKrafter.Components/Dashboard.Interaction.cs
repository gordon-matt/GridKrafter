namespace GridKrafter.Components;

/// <content>
/// Dragging, resizing and keyboard moves. The browser reports the pointer in grid cells; every
/// decision about where widgets go is made here and rendered, so the preview is exactly the result.
/// </content>
public sealed partial class Dashboard
{
    internal async Task SetWidthAsync(double width)
    {
        var next = DashboardLayout.ForWidth(layouts, width);
        if (string.Equals(next.Id, layout.Id, StringComparison.Ordinal))
        {
            return;
        }

        layout = next;
        interaction = null;
        engine = null;
        revision++;
        Invalidate();
        Render();
        await OnLayoutChanged.InvokeAsync(layout);
    }

    internal bool StartInteraction(string id, bool isResize)
    {
        if (interaction is not null || board.Find(id) is not { } item || !(isResize ? CanResize(item) : CanMove(item)))
        {
            return false;
        }

        string section = SectionOfItem(item);
        var snapshot = Arrangement.GetValueOrDefault(section) ?? [];
        var start = snapshot.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.Ordinal));
        if (start is null)
        {
            return false;
        }

        interaction = new Interaction(id, isResize, section, start, snapshot);
        Render();
        return true;
    }

    internal void MoveTo(string section, int x, int y)
    {
        if (interaction is not { IsResize: false } active
            || Sections.FirstOrDefault(s => string.Equals(s.Id, section, StringComparison.Ordinal)) is not DashboardSection destination
            || (!UsesTabs && destination.Collapsed))
        {
            return;
        }

        if (active.Last == (section, x, y))
        {
            return;
        }

        active.Last = (section, x, y);
        if (string.Equals(section, active.SourceSection, StringComparison.Ordinal))
        {
            var moved = Engine.Move(active.Snapshot, active.ItemId, x, y);
            if (moved is null && !string.Equals(active.TargetSection, section, StringComparison.Ordinal))
            {
                moved = active.Snapshot;
            }

            if (moved is null)
            {
                return;
            }

            active.TargetSection = section;
            active.Preview = moved;
            active.SourcePreview = null;
        }
        else
        {
            var target = Arrangement.GetValueOrDefault(section) ?? [];
            var node = active.Start with { Rect = active.Start.Rect with { X = x, Y = y }, Locked = false };
            active.TargetSection = section;
            active.Preview = Engine.Insert(target, node);
            active.SourcePreview ??= Engine.Normalize(active.Snapshot.Where(n => !string.Equals(n.Id, active.ItemId, StringComparison.Ordinal)));
        }

        if (UsesTabs)
        {
            activeSectionId = active.TargetSection;
        }

        Render();
    }

    internal void ResizeTo(GridRect rect)
    {
        if (interaction is not { IsResize: true } active || board.Find(active.ItemId) is not { } item)
        {
            return;
        }

        if (HasDisplayHeight(item))
        {
            rect = rect with { Y = active.Start.Rect.Y, H = active.Start.Rect.H };
        }

        var resized = Engine.Resize(active.Snapshot, active.ItemId, rect);
        if (resized is null || ReferenceEquals(resized, active.Preview))
        {
            return;
        }

        active.Preview = resized;
        Render();
    }

    internal async Task EndInteractionAsync(bool commit)
    {
        if (interaction is not { } active)
        {
            return;
        }

        interaction = null;
        bool crossed = !string.Equals(active.TargetSection, active.SourceSection, StringComparison.Ordinal);
        if (!commit || board.Find(active.ItemId) is not { } item || (!crossed && ReferenceEquals(active.Preview, active.Snapshot)))
        {
            // The tab followed the pointer; cancelling puts it back on the section the widget never left.
            if (UsesTabs)
            {
                activeSectionId = active.SourceSection;
            }

            FlushMeasurements();
            Render();
            return;
        }

        if (UsesTabs && crossed)
        {
            activeSectionId = active.TargetSection;
        }

        if (crossed)
        {
            item.Section = active.TargetSection;
            item.Placements = [];
            Freeze(active.SourcePreview ?? []);
        }

        Freeze(active.Preview);
        FlushMeasurements();
        await CommitAsync(active.IsResize ? DashboardChangeKind.Resized : DashboardChangeKind.Moved, active.ItemId, active.TargetSection);
    }

    internal async Task NudgeAsync(string id, int dx, int dy, bool resize)
    {
        if (interaction is not null || board.Find(id) is not { } item || !(resize ? CanResize(item) : CanMove(item)))
        {
            return;
        }

        string section = SectionOfItem(item);
        var nodes = Arrangement.GetValueOrDefault(section) ?? [];
        var node = nodes.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.Ordinal));
        if (node is null)
        {
            return;
        }

        var result = resize
            ? Engine.Resize(nodes, id, node.Rect with { W = node.Rect.W + dx, H = HasDisplayHeight(item) ? node.Rect.H : node.Rect.H + dy })
            : Engine.Nudge(nodes, id, dx, dy);
        if (result is null || ReferenceEquals(result, nodes))
        {
            return;
        }

        Freeze(result);
        await CommitAsync(resize ? DashboardChangeKind.Resized : DashboardChangeKind.Moved, id, section);
    }

    internal void SetMeasured(string json)
    {
        JsonObject? rows;
        try
        {
            rows = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return;
        }

        bool changed = false;
        foreach (var (id, value) in rows ?? [])
        {
            if (value is JsonValue number && number.TryGetValue(out int count) && count > 0 && measuredRows.GetValueOrDefault(id) != count)
            {
                measuredRows[id] = count;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        if (interaction is not null)
        {
            measuredWhileBusy = true;
            return;
        }

        Invalidate();
        Render();
    }

    /// <summary>Extra rows a fixed-height widget needs while its edit panel is open. Zero clears the extra.</summary>
    internal void SetEditGrown(string json)
    {
        JsonObject? rows;
        try
        {
            rows = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return;
        }

        bool changed = false;
        foreach (var (id, value) in rows ?? [])
        {
            if (value is not JsonValue number || !number.TryGetValue(out int extra) || extra < 0)
            {
                continue;
            }

            if (extra == 0)
            {
                changed |= editRows.Remove(id);
            }
            else if (editRows.GetValueOrDefault(id) != extra)
            {
                editRows[id] = extra;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        if (interaction is not null)
        {
            measuredWhileBusy = true;
            return;
        }

        Invalidate();
        Render();
    }

    private void FlushMeasurements()
    {
        if (measuredWhileBusy)
        {
            measuredWhileBusy = false;
            Invalidate();
        }
    }

    /// <summary>A drag or resize in progress. Previews are always worked out from the snapshot taken when it started.</summary>
    private sealed class Interaction(string itemId, bool isResize, string section, GridNode start, IReadOnlyList<GridNode> snapshot)
    {
        public string ItemId { get; } = itemId;

        public bool IsResize { get; } = isResize;

        public string SourceSection { get; } = section;

        public GridNode Start { get; } = start;

        public IReadOnlyList<GridNode> Snapshot { get; } = snapshot;

        public string TargetSection { get; set; } = section;

        public IReadOnlyList<GridNode> Preview { get; set; } = snapshot;

        /// <summary>Gets or sets the source section without the widget, while it's over another section.</summary>
        public IReadOnlyList<GridNode>? SourcePreview { get; set; }

        public (string Section, int X, int Y)? Last { get; set; }
    }
}
