namespace GridKrafter.Localization;

/// <summary>
/// Built-in catalogs are embedded in this assembly. A host adds a language by dropping
/// <c>{tag}.json</c> in <see cref="GridKrafterLocalizationOptions.Directory"/>, or in
/// <c>GridKrafter/Locales</c> beside the app. Missing keys fall back through parent tags to English.
/// </summary>
public sealed class GridKrafterLocaleCatalog : IGridKrafterLocalizer
{
    private const string ResourcePrefix = "GridKrafter.Locales.";
    private const string RtlKey = "@rtl";

    private readonly Dictionary<string, Dictionary<string, string>> catalogs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="GridKrafterLocaleCatalog"/> class.
    /// </summary>
    /// <param name="options">Where to find extra language files.</param>
    public GridKrafterLocaleCatalog(GridKrafterLocalizationOptions options)
    {
        LoadEmbedded();
        LoadDirectory(options.Directory);
        LoadDirectory(Path.Combine(AppContext.BaseDirectory, "GridKrafter", "Locales"));
        RebuildLanguages();
    }

    /// <summary>
    /// Gets the catalog used when no localizer is registered: the built-in languages plus any files beside the app.
    /// </summary>
    public static GridKrafterLocaleCatalog Shared { get; } = new(new GridKrafterLocalizationOptions());

    /// <inheritdoc/>
    public IReadOnlyList<GridKrafterLanguage> Languages { get; private set; } = [];

    /// <inheritdoc/>
    public GridKrafterText Resolve(string? languageTag)
    {
        string requested = string.IsNullOrWhiteSpace(languageTag) ? "en" : languageTag.Trim().Replace('_', '-');
        List<Dictionary<string, string>> chain = [];
        string? matched = null;

        foreach (string tag in FallbackTags(requested))
        {
            if (!catalogs.TryGetValue(tag, out var catalog))
            {
                continue;
            }

            matched ??= tag;
            chain.Add(catalog);
        }

        string language = matched ?? "en";
        var culture = TryCulture(language) ?? CultureInfo.InvariantCulture;
        bool rtl = culture.TextInfo.IsRightToLeft || chain.Any(catalog =>
            catalog.TryGetValue(RtlKey, out var flag) && flag.Equals("true", StringComparison.OrdinalIgnoreCase));

        return new GridKrafterText(language, culture, rtl, chain);
    }

    private static IEnumerable<string> FallbackTags(string tag)
    {
        HashSet<string> yielded = new(StringComparer.OrdinalIgnoreCase);
        if (yielded.Add(tag))
        {
            yield return tag;
        }

        int dash = tag.LastIndexOf('-');
        while (dash > 0)
        {
            string parent = tag[..dash];
            if (yielded.Add(parent))
            {
                yield return parent;
            }

            dash = parent.LastIndexOf('-');
        }

        if (yielded.Add("en"))
        {
            yield return "en";
        }
    }

    /// <summary>Null under invariant globalization, as in the custom elements build.</summary>
    private static CultureInfo? TryCulture(string tag)
    {
        try
        {
            return CultureInfo.GetCultureInfo(tag);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static bool IsLanguageTag(string tag) =>
        tag.Length is >= 2 and <= 16 && tag.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    private void LoadEmbedded()
    {
        var assembly = typeof(GridKrafterLocaleCatalog).Assembly;
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string tag = name[ResourcePrefix.Length..^".json".Length];
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is not null)
            {
                Merge(tag, stream);
            }
        }
    }

    private void LoadDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*.json"))
        {
            string tag = Path.GetFileNameWithoutExtension(file);
            if (!IsLanguageTag(tag))
            {
                continue;
            }

            try
            {
                using var stream = File.OpenRead(file);
                Merge(tag, stream);
            }
            catch (IOException)
            {
                // A file the host is still writing shouldn't take the dashboard down.
            }
            catch (JsonException)
            {
                // One broken catalog is skipped; its keys keep their built-in or English text.
            }
        }
    }

    /// <summary>Later catalogs win, so a host's files overlay the built-in ones.</summary>
    private void Merge(string tag, Stream stream)
    {
        if (JsonNode.Parse(stream) is not JsonObject parsed)
        {
            return;
        }

        if (!catalogs.TryGetValue(tag, out var target))
        {
            target = new Dictionary<string, string>(StringComparer.Ordinal);
            catalogs[tag] = target;
        }

        foreach (var (key, node) in parsed)
        {
            if (string.IsNullOrEmpty(key) || node is not JsonValue value || !value.TryGetValue(out string? text))
            {
                continue;
            }

            if (text.Length > 0 || key.StartsWith('@'))
            {
                target[key] = text;
            }
        }
    }

    private void RebuildLanguages() =>
        Languages = [.. catalogs.Keys
            .OrderBy(tag => tag.Equals("en", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(NativeName, StringComparer.OrdinalIgnoreCase)
            .Select(tag =>
            {
                bool rtl = TryCulture(tag)?.TextInfo.IsRightToLeft == true
                    || (catalogs[tag].TryGetValue(RtlKey, out var flag) && flag.Equals("true", StringComparison.OrdinalIgnoreCase));
                return new GridKrafterLanguage(tag, NativeName(tag), rtl);
            })];

    private string NativeName(string tag) =>
        catalogs.TryGetValue(tag, out var catalog) && catalog.TryGetValue("@name", out var name) ? name : TryCulture(tag)?.NativeName ?? tag;
}
