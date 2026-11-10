namespace GridKrafter.Layout;

/// <summary>One thing on the grid, where it is, and how far it may grow or shrink.</summary>
/// <param name="Id">Identifies the node; unique within a grid.</param>
/// <param name="Rect">Where the node sits.</param>
public sealed record GridNode(string Id, GridRect Rect)
{
    /// <summary>
    /// Gets the minimum number of columns.
    /// </summary>
    public int MinW { get; init; } = 1;

    /// <summary>
    /// Gets the minimum number of rows.
    /// </summary>
    public int MinH { get; init; } = 1;

    /// <summary>
    /// Gets the maximum number of columns, or <see langword="null"/> for the full width.
    /// </summary>
    public int? MaxW { get; init; }

    /// <summary>
    /// Gets the maximum number of rows, or <see langword="null"/> for no limit.
    /// </summary>
    public int? MaxH { get; init; }

    /// <summary>Never moved by the engine, and can't be dragged or resized; everything else flows around it.</summary>
    public bool Locked { get; init; }
}
