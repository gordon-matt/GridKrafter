namespace GridKrafter.Localization;

/// <summary>A language the dashboard can show, named for a language menu.</summary>
/// <param name="Tag">The IETF language tag, such as <c>pt-BR</c>.</param>
/// <param name="NativeName">The language's name in that language.</param>
/// <param name="IsRightToLeft">Whether it's written right to left.</param>
public sealed record GridKrafterLanguage(string Tag, string NativeName, bool IsRightToLeft);

/// <summary>Loads JSON catalogs and resolves a tag, including fallback to English.</summary>
public interface IGridKrafterLocalizer
{
    /// <summary>
    /// Gets every language with a catalog, English first.
    /// </summary>
    IReadOnlyList<GridKrafterLanguage> Languages { get; }

    /// <summary>The strings for a language tag, falling back through parent tags to English.</summary>
    /// <param name="languageTag">An IETF tag, or <see langword="null"/> for English.</param>
    /// <returns>The strings.</returns>
    GridKrafterText Resolve(string? languageTag);
}

/// <summary>
/// Where extra language files live. Each file is named with an IETF tag, such as <c>fr-CA.json</c>,
/// and contains a flat object of string keys. Keys you leave out fall back to English.
/// </summary>
public sealed class GridKrafterLocalizationOptions
{
    /// <summary>
    /// Gets or sets a folder of <c>{tag}.json</c> files. Files overlay the built-in catalog for that tag, and a
    /// tag with no built-in file becomes a new language. A path that doesn't exist is ignored.
    /// </summary>
    public string? Directory { get; set; }
}
