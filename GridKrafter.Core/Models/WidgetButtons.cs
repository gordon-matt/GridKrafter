namespace GridKrafter.Models;

/// <summary>The buttons in a widget's header. In JSON, a comma-separated list such as <c>"collapse, fullscreen"</c>.</summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<WidgetButtons>))]
public enum WidgetButtons
{
    /// <summary>No buttons.</summary>
    [JsonStringEnumMemberName("none")]
    None = 0,

    /// <summary>Reloads the content and updates the timestamp.</summary>
    [JsonStringEnumMemberName("refresh")]
    Refresh = 1,

    /// <summary>Picks the header colour.</summary>
    [JsonStringEnumMemberName("color")]
    Color = 2,

    /// <summary>Opens the edit panel to rename the widget and change its settings.</summary>
    [JsonStringEnumMemberName("edit")]
    Edit = 4,

    /// <summary>Fills the window with the widget.</summary>
    [JsonStringEnumMemberName("fullscreen")]
    Fullscreen = 8,

    /// <summary>Shows only the header, or everything again.</summary>
    [JsonStringEnumMemberName("collapse")]
    Collapse = 16,

    /// <summary>Removes the widget, after asking.</summary>
    [JsonStringEnumMemberName("delete")]
    Delete = 32,

    /// <summary>Everything except refresh, which only makes sense for content that can reload.</summary>
    [JsonStringEnumMemberName("default")]
    Default = Color | Edit | Fullscreen | Collapse | Delete,

    /// <summary>Every button.</summary>
    [JsonStringEnumMemberName("all")]
    All = Refresh | Default,
}
