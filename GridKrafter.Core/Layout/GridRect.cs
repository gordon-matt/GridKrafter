namespace GridKrafter.Layout;

/// <summary>
/// A block of grid cells. <see cref="X"/> is the first column and <see cref="Y"/> the first row,
/// both zero-based; <see cref="W"/> and <see cref="H"/> are how many columns and rows it spans.
/// </summary>
/// <param name="X">The first column.</param>
/// <param name="Y">The first row.</param>
/// <param name="W">The number of columns.</param>
/// <param name="H">The number of rows.</param>
public readonly record struct GridRect(int X, int Y, int W, int H)
{
    /// <summary>The column just past the right edge.</summary>
    [JsonIgnore] public int Right => X + W;

    /// <summary>The row just below the bottom edge.</summary>
    [JsonIgnore] public int Bottom => Y + H;

    /// <summary>The number of cells covered.</summary>
    [JsonIgnore] public int Area => W * H;

    /// <summary>Whether the two share at least one cell. Touching edges don't count.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><see langword="true"/> when they overlap.</returns>
    public bool Intersects(GridRect other) =>
        X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;

    /// <summary>The number of cells both cover.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The shared area, or zero.</returns>
    public int Overlap(GridRect other)
    {
        int width = Math.Min(Right, other.Right) - Math.Max(X, other.X);
        int height = Math.Min(Bottom, other.Bottom) - Math.Max(Y, other.Y);
        return width > 0 && height > 0 ? width * height : 0;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{X},{Y} {W}x{H}";
}
