namespace GridKrafter.Components;

/// <summary>
/// What someone did to a dashboard.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DashboardChangeKind>))]
public enum DashboardChangeKind
{
    /// <summary>A widget was dragged somewhere else, possibly into another section.</summary>
    [JsonStringEnumMemberName("moved")]
    Moved,

    /// <summary>A widget was resized.</summary>
    [JsonStringEnumMemberName("resized")]
    Resized,

    /// <summary>A widget was collapsed to its header.</summary>
    [JsonStringEnumMemberName("collapsed")]
    Collapsed,

    /// <summary>A collapsed widget was expanded.</summary>
    [JsonStringEnumMemberName("expanded")]
    Expanded,

    /// <summary>A widget was renamed.</summary>
    [JsonStringEnumMemberName("retitled")]
    Retitled,

    /// <summary>A widget's colour changed.</summary>
    [JsonStringEnumMemberName("recolored")]
    Recolored,

    /// <summary>A widget added from the catalog was deleted.</summary>
    [JsonStringEnumMemberName("removed")]
    Removed,

    /// <summary>A widget the page defines was deleted, so it's hidden until restored.</summary>
    [JsonStringEnumMemberName("hidden")]
    Hidden,

    /// <summary>A hidden widget was brought back.</summary>
    [JsonStringEnumMemberName("restored")]
    Restored,

    /// <summary>A widget was added from the catalog.</summary>
    [JsonStringEnumMemberName("added")]
    Added,

    /// <summary>A widget's settings changed.</summary>
    [JsonStringEnumMemberName("options")]
    Options,

    /// <summary>A section was added.</summary>
    [JsonStringEnumMemberName("sectionAdded")]
    SectionAdded,

    /// <summary>A section was deleted and its widgets moved to its neighbour.</summary>
    [JsonStringEnumMemberName("sectionRemoved")]
    SectionRemoved,

    /// <summary>A section was renamed, collapsed, expanded or reordered.</summary>
    [JsonStringEnumMemberName("sectionChanged")]
    SectionChanged,

    /// <summary>The board was put back the way the page defines it.</summary>
    [JsonStringEnumMemberName("reset")]
    Reset,
}

/// <summary>
/// A change to a dashboard, with the whole board as it is now, ready to save.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="ItemId">The widget it happened to, if any.</param>
/// <param name="SectionId">The section it happened in, if any.</param>
/// <param name="Layout">The layout that was showing, such as <c>large</c>.</param>
/// <param name="Board">The board after the change.</param>
public sealed record DashboardChange(DashboardChangeKind Kind, string? ItemId, string? SectionId, string Layout, DashboardBoard Board);
