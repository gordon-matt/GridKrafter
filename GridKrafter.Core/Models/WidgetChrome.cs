namespace GridKrafter.Models;

/// <summary>How a widget is framed.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WidgetChrome>))]
public enum WidgetChrome
{
    /// <summary>A header bar with the icon, title, toolbar and buttons above the body.</summary>
    [JsonStringEnumMemberName("panel")]
    Panel,

    /// <summary>The content fills the card; the title is a small badge and the buttons appear on hover.</summary>
    [JsonStringEnumMemberName("card")]
    Card,

    /// <summary>No frame at all: the content is the widget.</summary>
    [JsonStringEnumMemberName("plain")]
    Plain,
}
