namespace GridKrafter.Components;

/// <summary>The kind of field a widget setting is edited with.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WidgetOptionType>))]
public enum WidgetOptionType
{
    /// <summary>A single line of text.</summary>
    [JsonStringEnumMemberName("text")]
    Text,

    /// <summary>Several lines of text.</summary>
    [JsonStringEnumMemberName("textarea")]
    TextArea,

    /// <summary>A number, optionally limited by <see cref="WidgetOption.Min"/> and <see cref="WidgetOption.Max"/>.</summary>
    [JsonStringEnumMemberName("number")]
    Number,

    /// <summary>On or off.</summary>
    [JsonStringEnumMemberName("switch")]
    Switch,

    /// <summary>One of <see cref="WidgetOption.Choices"/>.</summary>
    [JsonStringEnumMemberName("select")]
    Select,

    /// <summary>A colour.</summary>
    [JsonStringEnumMemberName("color")]
    Color,

    /// <summary>A number picked with a slider.</summary>
    [JsonStringEnumMemberName("range")]
    Range,
}

/// <summary>A choice for a <see cref="WidgetOptionType.Select"/> setting.</summary>
/// <param name="Value">The value stored.</param>
/// <param name="Label">The text shown.</param>
public sealed record WidgetOptionChoice(string Value, string Label);

/// <summary>A setting in a widget's edit panel, stored under <see cref="Key"/> in <see cref="DashboardItem.Options"/>.</summary>
public sealed class WidgetOption
{
    /// <summary>
    /// Gets or sets the key the value is stored under.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field label; the key when not set.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets help text under the field.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the kind of field.
    /// </summary>
    public WidgetOptionType Type { get; set; } = WidgetOptionType.Text;

    /// <summary>
    /// Gets or sets the value of a new widget.
    /// </summary>
    public JsonNode? Default { get; set; }

    /// <summary>
    /// Gets or sets the choices of a select.
    /// </summary>
    public List<WidgetOptionChoice> Choices { get; set; } = [];

    /// <summary>
    /// Gets or sets the smallest number allowed.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Gets or sets the largest number allowed.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Gets or sets the step between numbers.
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// Gets or sets the hint shown in an empty text field.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>A single line of text.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default.</param>
    /// <returns>The option.</returns>
    public static WidgetOption Text(string key, string label, string? value = null) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.Text, Default = value };

    /// <summary>Several lines of text.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default.</param>
    /// <returns>The option.</returns>
    public static WidgetOption TextArea(string key, string label, string? value = null) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.TextArea, Default = value };

    /// <summary>A number.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default.</param>
    /// <param name="min">The smallest allowed.</param>
    /// <param name="max">The largest allowed.</param>
    /// <param name="step">The step.</param>
    /// <returns>The option.</returns>
    public static WidgetOption Number(string key, string label, double value, double? min = null, double? max = null, double? step = null) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.Number, Default = value, Min = min, Max = max, Step = step };

    /// <summary>A number picked with a slider.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default.</param>
    /// <param name="min">The smallest allowed.</param>
    /// <param name="max">The largest allowed.</param>
    /// <param name="step">The step.</param>
    /// <returns>The option.</returns>
    public static WidgetOption Range(string key, string label, double value, double min, double max, double step = 1) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.Range, Default = value, Min = min, Max = max, Step = step };

    /// <summary>On or off.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default.</param>
    /// <returns>The option.</returns>
    public static WidgetOption Switch(string key, string label, bool value = false) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.Switch, Default = value };

    /// <summary>One of several choices.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default value.</param>
    /// <param name="choices">The choices, as value and label pairs.</param>
    /// <returns>The option.</returns>
    public static WidgetOption Select(string key, string label, string value, params (string Value, string Label)[] choices) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.Select, Default = value, Choices = [.. choices.Select(c => new WidgetOptionChoice(c.Value, c.Label))] };

    /// <summary>A colour.</summary>
    /// <param name="key">The storage key.</param>
    /// <param name="label">The label.</param>
    /// <param name="value">The default, such as <c>#2563eb</c>.</param>
    /// <returns>The option.</returns>
    public static WidgetOption Color(string key, string label, string value) =>
        new() { Key = key, Label = label, Type = WidgetOptionType.Color, Default = value };
}
