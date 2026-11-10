namespace GridKrafter.Components;

/// <summary>
/// A kind of widget people can add from the catalog: what it's called, how big it starts, the
/// settings it offers, and what renders it. Hosts that aren't Blazor describe the same thing in
/// JSON and leave <see cref="Component"/> out, rendering into the widget's empty body themselves.
/// </summary>
public sealed class WidgetDefinition
{
    /// <summary>
    /// Gets or sets the identifier items refer to in <see cref="DashboardItem.Kind"/>.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name in the catalog, and the title of new widgets.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a sentence for the catalog.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a built-in icon name or icon-font classes.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the header colour of new widgets.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the starting width, in columns of the largest layout.
    /// </summary>
    public int W { get; set; } = Constants.Defaults.Width;

    /// <summary>
    /// Gets or sets the starting height, in rows.
    /// </summary>
    public int H { get; set; } = Constants.Defaults.Height;

    /// <summary>
    /// Gets or sets the minimum width in columns.
    /// </summary>
    public int? MinW { get; set; }

    /// <summary>
    /// Gets or sets the minimum height in rows.
    /// </summary>
    public int? MinH { get; set; }

    /// <summary>
    /// Gets or sets the maximum width in columns.
    /// </summary>
    public int? MaxW { get; set; }

    /// <summary>
    /// Gets or sets the maximum height in rows.
    /// </summary>
    public int? MaxH { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the height follows the content.
    /// </summary>
    public bool AutoHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the body has no padding.
    /// </summary>
    public bool NoPadding { get; set; }

    /// <summary>
    /// Gets or sets the header buttons, replacing the dashboard's defaults.
    /// </summary>
    public WidgetButtons? Buttons { get; set; }

    /// <summary>
    /// Gets or sets the frame style, replacing the dashboard's default.
    /// </summary>
    public WidgetChrome? Chrome { get; set; }

    /// <summary>
    /// Gets or sets how often, in seconds, widgets of this kind refresh themselves.
    /// </summary>
    public int? Refresh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a board can only have one, so the catalog stops offering it once added.
    /// </summary>
    public bool Single { get; set; }

    /// <summary>
    /// Gets or sets markup for the body, trusted as-is.
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// Gets or sets an address whose HTML becomes the body.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a selector for a <c>&lt;template&gt;</c> whose content becomes the body.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the settings in the widget's edit panel. Their values are stored in <see cref="DashboardItem.Options"/>.
    /// </summary>
    public List<WidgetOption> Options { get; set; } = [];

    /// <summary>
    /// Gets or sets the Blazor component that renders the body. It receives a cascading <see cref="WidgetContext"/>.
    /// </summary>
    [JsonIgnore]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? Component { get; set; }

    /// <summary>A definition rendered by a component.</summary>
    /// <typeparam name="TComponent">The component for the body.</typeparam>
    /// <param name="kind">The kind identifier.</param>
    /// <param name="name">The name in the catalog.</param>
    /// <returns>The definition, ready for its other properties to be set.</returns>
    public static WidgetDefinition For<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(string kind, string? name = null)
        where TComponent : IComponent =>
        new() { Kind = kind, Name = name, Component = typeof(TComponent) };

    /// <summary>The starting settings of a new widget: every option's default.</summary>
    /// <returns>The options object, or <see langword="null"/> when there are no options.</returns>
    public JsonObject? DefaultOptions()
    {
        if (Options.Count == 0)
        {
            return null;
        }

        JsonObject options = [];
        foreach (var option in Options.Where(o => !string.IsNullOrEmpty(o.Key)))
        {
            options[option.Key] = option.Default?.DeepClone();
        }

        return options;
    }
}
