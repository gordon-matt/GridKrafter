namespace GridKrafter.Components;

/// <summary>
/// A widget whose body the dashboard leaves empty, for another framework to render into. The
/// body is the element matching <c>[data-gk-mount="<see cref="Id"/>"]</c>.
/// </summary>
/// <param name="Id">The widget's id.</param>
/// <param name="Kind">The widget's kind, if it has one.</param>
/// <param name="Section">The section it's in; a widget moved to another section gets a new, empty body.</param>
public sealed record WidgetMount(string Id, string? Kind, string Section);
