namespace GridKrafter.Layout;

/// <summary>What happens to empty space above a node.</summary>
public enum GridCompaction
{
    /// <summary>Nodes rise until something stops them, so the grid never has holes at the top.</summary>
    Vertical,

    /// <summary>Nodes stay where they're put; only collisions move them.</summary>
    None,
}
