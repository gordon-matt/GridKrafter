namespace GridKrafter.Layout;

/// <summary>
/// Places nodes on a grid with a fixed number of columns and as many rows as they need. Every
/// operation is pure: it returns new nodes in the order it was given them, so a drag is always
/// previewed from the layout it started with instead of compounding each step's adjustments.
/// </summary>
public sealed class GridEngine
{
    /// <summary>
    /// A collision only counts once the dragged node covers more than this share of what it hits
    /// (or of itself, when that's smaller), so grazing a neighbour doesn't shove it aside.
    /// </summary>
    private const double CoverageThreshold = 0.5;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridEngine"/> class.
    /// </summary>
    /// <param name="columns">The number of columns; at least one.</param>
    /// <param name="compaction">What happens to empty space above a node.</param>
    public GridEngine(int columns, GridCompaction compaction = GridCompaction.Vertical)
    {
        Columns = Math.Max(1, columns);
        Compaction = compaction;
    }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets what happens to empty space above a node.
    /// </summary>
    public GridCompaction Compaction { get; }

    /// <summary>The first row below every node; zero for an empty grid.</summary>
    /// <param name="nodes">The nodes.</param>
    /// <returns>The number of rows in use.</returns>
    public static int Bottom(IEnumerable<GridNode> nodes)
    {
        int bottom = 0;
        foreach (var node in nodes)
        {
            bottom = Math.Max(bottom, node.Rect.Bottom);
        }

        return bottom;
    }

    /// <summary>Fits a rectangle inside the grid and the node's own size limits.</summary>
    /// <param name="node">The node whose limits apply.</param>
    /// <param name="rect">Where it would like to be.</param>
    /// <returns>The nearest rectangle that's allowed.</returns>
    public GridRect Clamp(GridNode node, GridRect rect)
    {
        int maxW = Math.Max(1, Math.Min(node.MaxW ?? Columns, Columns));
        int minW = Math.Min(Math.Max(1, node.MinW), maxW);
        int w = Math.Clamp(rect.W, minW, maxW);

        int minH = Math.Max(1, node.MinH);
        int maxH = Math.Max(minH, node.MaxH ?? int.MaxValue);
        int h = Math.Clamp(rect.H, minH, maxH);

        return new GridRect(Math.Clamp(rect.X, 0, Columns - w), Math.Max(0, rect.Y), w, h);
    }

    /// <summary>
    /// Makes a layout valid: everything inside the grid and its size limits, nothing overlapping
    /// (later nodes in reading order move down), and with vertical compaction no gaps above.
    /// </summary>
    /// <param name="nodes">The nodes, in any state.</param>
    /// <returns>The same nodes, in the same order, laid out validly.</returns>
    public IReadOnlyList<GridNode> Normalize(IEnumerable<GridNode> nodes)
    {
        IReadOnlyList<GridNode> list = nodes as IReadOnlyList<GridNode> ?? [.. nodes];
        var rects = new GridRect[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            rects[i] = Clamp(list[i], list[i].Rect);
        }

        var locked = Locks(list);
        Settle(rects, locked, ReadingOrder(rects));
        if (Compaction == GridCompaction.Vertical)
        {
            CompactUp(rects, locked);
        }

        return Apply(list, rects);
    }

    /// <summary>
    /// Moves one node and rearranges the rest around it. A same-sized neighbour sharing its row or
    /// column swaps places with it; moving down onto something lifts that above when there's room;
    /// anything else in the way is pushed down.
    /// </summary>
    /// <param name="nodes">The layout before the move, normally the one the drag started from.</param>
    /// <param name="id">The node to move.</param>
    /// <param name="x">The column to move its left edge to.</param>
    /// <param name="y">The row to move its top edge to.</param>
    /// <returns>
    /// The new layout; the same list when nothing changes; or <see langword="null"/> when the node
    /// can't move, or only grazes its neighbours so the previous preview should stand.
    /// </returns>
    public IReadOnlyList<GridNode>? Move(IReadOnlyList<GridNode> nodes, string id, int x, int y)
    {
        int index = IndexOf(nodes, id);
        if (index < 0 || nodes[index].Locked)
        {
            return null;
        }

        var start = nodes[index].Rect;
        var target = Clamp(nodes[index], start with { X = x, Y = y });
        if (target == start)
        {
            return nodes;
        }

        var rects = Rects(nodes);
        var locked = Locks(nodes);
        rects[index] = target = BelowLocked(rects, locked, index, target);

        var colliders = Colliders(rects, locked, index);
        if (colliders.Count > 0)
        {
            double coverage = colliders.Max(j => (double)rects[j].Overlap(target) / Math.Min(rects[j].Area, target.Area));
            if (coverage <= CoverageThreshold)
            {
                return null;
            }

            bool swapped = colliders.Count == 1 && TrySwap(rects, index, start, colliders[0]);
            if (!swapped && target.Y > start.Y)
            {
                LiftAbove(rects, index, colliders);
            }
        }

        var isFixed = (bool[])locked.Clone();
        isFixed[index] = true;
        Settle(rects, isFixed, ReadingOrder(rects));
        if (Compaction == GridCompaction.Vertical)
        {
            CompactUp(rects, locked);
        }

        return Apply(nodes, rects);
    }

    /// <summary>Moves a node one step at a time in a direction until the layout actually changes, for keyboard moves.</summary>
    /// <param name="nodes">The current layout.</param>
    /// <param name="id">The node to move.</param>
    /// <param name="dx">The column direction: -1, 0 or 1.</param>
    /// <param name="dy">The row direction: -1, 0 or 1.</param>
    /// <returns>The new layout, or <see langword="null"/> when it can't go any further that way.</returns>
    public IReadOnlyList<GridNode>? Nudge(IReadOnlyList<GridNode> nodes, string id, int dx, int dy)
    {
        int index = IndexOf(nodes, id);
        if (index < 0 || nodes[index].Locked || (dx == 0 && dy == 0))
        {
            return null;
        }

        var start = nodes[index].Rect;
        int steps = dy != 0 ? Bottom(nodes) + 1 : Columns;
        for (int step = 1; step <= steps; step++)
        {
            int x = start.X + (Math.Sign(dx) * step);
            int y = start.Y + (Math.Sign(dy) * step);
            if (x < 0 || y < 0 || x + start.W > Columns)
            {
                break;
            }

            var moved = Move(nodes, id, x, y);
            if (moved is not null && moved[index].Rect != start)
            {
                return moved;
            }
        }

        return null;
    }

    /// <summary>Resizes one node and pushes whatever it now covers out of the way.</summary>
    /// <param name="nodes">The layout before the resize, normally the one the drag started from.</param>
    /// <param name="id">The node to resize.</param>
    /// <param name="rect">The rectangle it should cover.</param>
    /// <returns>
    /// The new layout; the same list when nothing changes; or <see langword="null"/> when it can't
    /// be resized there (it's locked, or the new size would cover a locked node).
    /// </returns>
    public IReadOnlyList<GridNode>? Resize(IReadOnlyList<GridNode> nodes, string id, GridRect rect)
    {
        int index = IndexOf(nodes, id);
        if (index < 0 || nodes[index].Locked)
        {
            return null;
        }

        var target = Clamp(nodes[index], rect);
        if (target == nodes[index].Rect)
        {
            return nodes;
        }

        var rects = Rects(nodes);
        var locked = Locks(nodes);
        for (int i = 0; i < rects.Length; i++)
        {
            if (locked[i] && i != index && rects[i].Intersects(target))
            {
                return null;
            }
        }

        rects[index] = target;
        var isFixed = (bool[])locked.Clone();
        isFixed[index] = true;
        Settle(rects, isFixed, ReadingOrder(rects));
        if (Compaction == GridCompaction.Vertical)
        {
            CompactUp(rects, locked);
        }

        return Apply(nodes, rects);
    }

    /// <summary>Adds a node exactly where it says it is, pushing anything there down. Used when a node is dragged in from another grid.</summary>
    /// <param name="nodes">The layout to add to.</param>
    /// <param name="node">The node to add; its id mustn't already be in the layout.</param>
    /// <returns>The layout with the node appended.</returns>
    public IReadOnlyList<GridNode> Insert(IReadOnlyList<GridNode> nodes, GridNode node)
    {
        List<GridNode> list = [.. nodes, node];
        var rects = Rects(list);
        var locked = Locks(list);
        int index = list.Count - 1;
        rects[index] = BelowLocked(rects, locked, index, Clamp(node, node.Rect));

        var isFixed = (bool[])locked.Clone();
        isFixed[index] = true;
        Settle(rects, isFixed, ReadingOrder(rects));
        if (Compaction == GridCompaction.Vertical)
        {
            CompactUp(rects, locked);
        }

        return Apply(list, rects);
    }

    /// <summary>Adds a node at the first place its size fits, reading left to right and top to bottom.</summary>
    /// <param name="nodes">The layout to add to.</param>
    /// <param name="node">The node to add; only its size is used.</param>
    /// <returns>The layout with the node appended.</returns>
    public IReadOnlyList<GridNode> Add(IReadOnlyList<GridNode> nodes, GridNode node)
    {
        var size = Clamp(node, node.Rect with { X = 0, Y = 0 });
        return [.. nodes, node with { Rect = FindSpace(nodes, size.W, size.H) }];
    }

    /// <summary>The first free spot of a given size, reading left to right and top to bottom.</summary>
    /// <param name="nodes">The layout to search.</param>
    /// <param name="w">The number of columns needed.</param>
    /// <param name="h">The number of rows needed.</param>
    /// <returns>A rectangle that overlaps nothing; at worst, below everything.</returns>
    public GridRect FindSpace(IReadOnlyList<GridNode> nodes, int w, int h)
    {
        w = Math.Clamp(w, 1, Columns);
        h = Math.Max(1, h);
        int bottom = Bottom(nodes);
        for (int y = 0; y < bottom; y++)
        {
            for (int x = 0; x + w <= Columns; x++)
            {
                var candidate = new GridRect(x, y, w, h);
                if (!nodes.Any(n => n.Rect.Intersects(candidate)))
                {
                    return candidate;
                }
            }
        }

        return new GridRect(0, bottom, w, h);
    }

    /// <summary>
    /// Re-flows a layout made for a different number of columns. Widths scale, and the nodes are
    /// laid out again in their reading order, each at the first spot after the one before, so a
    /// row of four tiles becomes two rows of two rather than leaving holes or changing their order.
    /// Down to one column it's a simple stack.
    /// </summary>
    /// <param name="nodes">The layout to convert.</param>
    /// <param name="fromColumns">The number of columns it was made for.</param>
    /// <returns>The same nodes, laid out for <see cref="Columns"/>.</returns>
    public IReadOnlyList<GridNode> Project(IReadOnlyList<GridNode> nodes, int fromColumns)
    {
        fromColumns = Math.Max(1, fromColumns);
        if (fromColumns == Columns)
        {
            return Normalize(nodes);
        }

        double ratio = (double)Columns / fromColumns;
        var rects = new GridRect[nodes.Count];
        var placed = new List<GridRect>(nodes.Count);
        int row = 0;
        int column = 0;
        foreach (int i in ReadingOrder(Rects(nodes)))
        {
            var source = nodes[i].Rect;
            int w = Math.Max(1, (int)Math.Round(source.W * ratio, MidpointRounding.AwayFromZero));
            var size = Clamp(nodes[i], new GridRect(0, 0, w, source.H));
            var rect = FlowFrom(placed, row, column, size.W, size.H);
            rects[i] = rect;
            placed.Add(rect);
            (row, column) = (rect.Y, rect.Right);
        }

        CompactUp(rects, new bool[nodes.Count]);
        return Apply(nodes, rects);
    }

    /// <summary>The first free spot at or after a cell, reading left to right and top to bottom.</summary>
    private GridRect FlowFrom(List<GridRect> placed, int row, int column, int w, int h)
    {
        for (int y = row; ; y++)
        {
            for (int x = y == row ? column : 0; x + w <= Columns; x++)
            {
                var candidate = new GridRect(x, y, w, h);
                if (!placed.Exists(p => p.Intersects(candidate)))
                {
                    return candidate;
                }
            }
        }
    }

    private static int IndexOf(IReadOnlyList<GridNode> nodes, string id)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (string.Equals(nodes[i].Id, id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static GridRect[] Rects(IReadOnlyList<GridNode> nodes)
    {
        var rects = new GridRect[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            rects[i] = nodes[i].Rect;
        }

        return rects;
    }

    private static bool[] Locks(IReadOnlyList<GridNode> nodes)
    {
        var locked = new bool[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            locked[i] = nodes[i].Locked;
        }

        return locked;
    }

    private static int[] ReadingOrder(GridRect[] rects) =>
        [.. Enumerable.Range(0, rects.Length).OrderBy(i => rects[i].Y).ThenBy(i => rects[i].X).ThenBy(i => i)];

    private static IReadOnlyList<GridNode> Apply(IReadOnlyList<GridNode> nodes, GridRect[] rects)
    {
        var result = new GridNode[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            result[i] = nodes[i].Rect == rects[i] ? nodes[i] : nodes[i] with { Rect = rects[i] };
        }

        return result;
    }

    private static List<int> Colliders(GridRect[] rects, bool[] locked, int index)
    {
        List<int> colliders = [];
        for (int i = 0; i < rects.Length; i++)
        {
            if (i != index && !locked[i] && rects[i].Intersects(rects[index]))
            {
                colliders.Add(i);
            }
        }

        return colliders;
    }

    /// <summary>A locked node can't make room, so whatever lands on one drops below it.</summary>
    private static GridRect BelowLocked(GridRect[] rects, bool[] locked, int index, GridRect target)
    {
        bool moved;
        do
        {
            moved = false;
            for (int i = 0; i < rects.Length; i++)
            {
                if (i != index && locked[i] && rects[i].Intersects(target))
                {
                    target = target with { Y = rects[i].Bottom };
                    moved = true;
                }
            }
        }
        while (moved);

        return target;
    }

    /// <summary>
    /// Moves every node that isn't fixed down until it overlaps nothing placed before it. Fixed
    /// nodes are placed first; the rest follow in the given order, so earlier ones keep their spot.
    /// </summary>
    private static void Settle(GridRect[] rects, bool[] isFixed, int[] order)
    {
        var placed = new List<GridRect>(rects.Length);
        for (int i = 0; i < rects.Length; i++)
        {
            if (isFixed[i])
            {
                placed.Add(rects[i]);
            }
        }

        foreach (int i in order)
        {
            if (isFixed[i])
            {
                continue;
            }

            var rect = rects[i];
            bool moved;
            do
            {
                moved = false;
                foreach (var other in placed)
                {
                    if (other.Intersects(rect))
                    {
                        rect = rect with { Y = other.Bottom };
                        moved = true;
                    }
                }
            }
            while (moved);

            rects[i] = rect;
            placed.Add(rect);
        }
    }

    /// <summary>
    /// Raises every node that isn't fixed as far as it'll go. Expects a layout without overlaps,
    /// which means each node only has to clear the nodes above it in its own columns.
    /// </summary>
    private static void CompactUp(GridRect[] rects, bool[] isFixed)
    {
        var placed = new List<GridRect>(rects.Length);
        for (int i = 0; i < rects.Length; i++)
        {
            if (isFixed[i])
            {
                placed.Add(rects[i]);
            }
        }

        foreach (int i in ReadingOrder(rects))
        {
            if (isFixed[i])
            {
                continue;
            }

            var rect = rects[i];
            int top = 0;
            foreach (var other in placed)
            {
                if (other.X < rect.Right && rect.X < other.Right && other.Bottom <= rect.Y)
                {
                    top = Math.Max(top, other.Bottom);
                }
            }

            rects[i] = rect = rect with { Y = top };
            placed.Add(rect);
        }
    }

    /// <summary>
    /// Trades places with a neighbour that shares the moving node's row (same top and height) or
    /// column (same left and width). The two keep their order along that line without a gap, so
    /// differently sized neighbours swap too.
    /// </summary>
    private bool TrySwap(GridRect[] rects, int index, GridRect start, int other)
    {
        var them = rects[other];
        GridRect mine;
        GridRect theirs;
        if (them.Y == start.Y && them.H == start.H)
        {
            if (them.X > start.X)
            {
                theirs = them with { X = start.X };
                mine = start with { X = start.X + them.W };
            }
            else
            {
                mine = start with { X = them.X };
                theirs = them with { X = them.X + start.W };
            }
        }
        else if (them.X == start.X && them.W == start.W)
        {
            if (them.Y > start.Y)
            {
                theirs = them with { Y = start.Y };
                mine = start with { Y = start.Y + them.H };
            }
            else
            {
                mine = start with { Y = them.Y };
                theirs = them with { Y = them.Y + start.H };
            }
        }
        else
        {
            return false;
        }

        if (mine.Right > Columns || theirs.Right > Columns || mine.Intersects(theirs))
        {
            return false;
        }

        for (int i = 0; i < rects.Length; i++)
        {
            if (i != index && i != other && (rects[i].Intersects(mine) || rects[i].Intersects(theirs)))
            {
                return false;
            }
        }

        rects[index] = mine;
        rects[other] = theirs;
        return true;
    }

    /// <summary>
    /// When a node moves down onto others, each of them goes just above it if that space is free,
    /// so they trade places instead of everything being shoved further down.
    /// </summary>
    private static void LiftAbove(GridRect[] rects, int index, List<int> colliders)
    {
        var target = rects[index];
        foreach (int j in colliders.OrderBy(j => rects[j].Y).ThenBy(j => rects[j].X))
        {
            var lifted = rects[j] with { Y = target.Y - rects[j].H };
            if (lifted.Y < 0)
            {
                continue;
            }

            bool free = true;
            for (int i = 0; i < rects.Length && free; i++)
            {
                free = i == j || !rects[i].Intersects(lifted);
            }

            if (free)
            {
                rects[j] = lifted;
            }
        }
    }
}
