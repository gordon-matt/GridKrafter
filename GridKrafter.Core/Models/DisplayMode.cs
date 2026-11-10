namespace GridKrafter.Models;

/// <summary>How a dashboard shows its sections.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DisplayMode>))]
public enum DisplayMode
{
    /// <summary>One section at a time, chosen from a tab strip. Dragging a widget onto another tab moves it there.</summary>
    [JsonStringEnumMemberName("tabs")]
    Tabs,

    /// <summary>Every section stacked, each one collapsible.</summary>
    [JsonStringEnumMemberName("accordion")]
    Accordion,
}
