namespace GridKrafter.Localization;

/// <summary>
/// The strings for one language. The indexer returns English when this catalog has no value for
/// a key, and the key itself only when English has no value either.
/// </summary>
public sealed class GridKrafterText
{
    private readonly IReadOnlyList<Dictionary<string, string>> chain;

    internal GridKrafterText(string language, CultureInfo culture, bool isRightToLeft, IReadOnlyList<Dictionary<string, string>> chain)
    {
        Language = language;
        Culture = culture;
        IsRightToLeft = isRightToLeft;
        this.chain = chain;
    }

    /// <summary>
    /// Gets the built-in English strings.
    /// </summary>
    public static GridKrafterText English { get; } = GridKrafterLocaleCatalog.Shared.Resolve("en");

    /// <summary>
    /// Gets the tag of the catalog that matched, such as <c>fr</c> for a request for <c>fr-CA</c>.
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// Gets the culture for numbers and dates; invariant when globalization is.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Gets a value indicating whether the language is written right to left.
    /// </summary>
    public bool IsRightToLeft { get; }

    /// <summary>The string for a key.</summary>
    /// <param name="key">The key, such as <c>widget.collapse</c>.</param>
    /// <returns>The string, or the key when no catalog has it.</returns>
    public string this[string key] => Get(key);

    /// <summary>The string for a key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The string, or the key when no catalog has it.</returns>
    public string Get(string key) => Find(key) ?? key;

    /// <summary>The string for a key with <c>{0}</c>-style placeholders filled in using <see cref="Culture"/>.</summary>
    /// <param name="key">The key.</param>
    /// <param name="args">The values for the placeholders.</param>
    /// <returns>The formatted string; the unformatted one if a translation's placeholders are broken.</returns>
    public string Format(string key, params object?[] args)
    {
        string template = Get(key);
        try
        {
            return string.Format(Culture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>The string for a key if any catalog has one.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The string, or <see langword="null"/>.</returns>
    public string? Find(string key)
    {
        foreach (var catalog in chain)
        {
            if (catalog.TryGetValue(key, out var value) && value.Length > 0)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>The name of a colour from <see cref="WidgetColors.Names"/>.</summary>
    /// <param name="color">The palette name.</param>
    /// <returns>The translated name, or the palette name.</returns>
    public string ColorName(string color) => Find("colors." + color) ?? color;

    /// <summary>The name of a layout: translated, then its own <see cref="DashboardLayout.Name"/>, then its id.</summary>
    /// <param name="layout">The layout.</param>
    /// <returns>The name to show.</returns>
    public string LayoutName(DashboardLayout layout) => Find("layouts." + layout.Id) ?? layout.Name ?? layout.Id;
}
