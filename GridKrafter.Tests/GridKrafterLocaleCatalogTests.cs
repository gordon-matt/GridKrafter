using System.Reflection;
using System.Text.RegularExpressions;

namespace GridKrafter.Tests;

public partial class GridKrafterLocaleCatalogTests
{
    public static TheoryData<string> Tags => [.. Catalogs().Keys.Where(tag => tag != "en")];

    [Theory]
    [MemberData(nameof(Tags))]
    public void Catalog_HasExactlyTheEnglishKeys(string tag)
    {
        static IEnumerable<string> Keys(Dictionary<string, string> catalog) =>
            catalog.Keys.Where(key => !key.StartsWith('@')).Order(StringComparer.Ordinal);

        Assert.Equal(Keys(Catalogs()["en"]), Keys(Catalogs()[tag]));
    }

    [Theory]
    [MemberData(nameof(Tags))]
    public void Catalog_KeepsEveryPlaceholder(string tag)
    {
        var english = Catalogs()["en"];
        var catalog = Catalogs()[tag];

        foreach (var (key, text) in english)
        {
            var expected = Placeholder().Matches(text).Select(m => m.Value).Order();
            var actual = Placeholder().Matches(catalog[key]).Select(m => m.Value).Order();
            Assert.True(expected.SequenceEqual(actual), $"{tag}: {key}");
        }
    }

    [Fact]
    public void Catalog_CoversEveryPaletteColour()
    {
        var english = Catalogs()["en"];

        Assert.All(WidgetColors.Names, color => Assert.True(english.ContainsKey("colors." + color), color));
    }

    [Fact]
    public void Resolve_RegionalTag_FallsBackToLanguage()
    {
        var text = GridKrafterLocaleCatalog.Shared.Resolve("fr-CA");

        Assert.Equal("fr", text.Language);
        Assert.Equal("Plein écran", text["widget.fullscreen"]);
    }

    [Fact]
    public void Resolve_UnknownTag_FallsBackToEnglish()
    {
        var text = GridKrafterLocaleCatalog.Shared.Resolve("xx");

        Assert.Equal("en", text.Language);
        Assert.Equal("Full screen", text["widget.fullscreen"]);
    }

    [Fact]
    public void Resolve_Arabic_IsRightToLeft()
    {
        Assert.True(GridKrafterLocaleCatalog.Shared.Resolve("ar").IsRightToLeft);
        Assert.False(GridKrafterLocaleCatalog.Shared.Resolve("de").IsRightToLeft);
    }

    [Fact]
    public void Format_FillsPlaceholders()
    {
        Assert.Equal("Delete “Sales”?", GridKrafterText.English.Format("widget.deleteTitle", "Sales"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKey()
    {
        Assert.Equal("no.such.key", GridKrafterText.English["no.such.key"]);
    }

    [Fact]
    public void Languages_EnglishFirst()
    {
        Assert.Equal("en", GridKrafterLocaleCatalog.Shared.Languages[0].Tag);
        Assert.Contains(GridKrafterLocaleCatalog.Shared.Languages, l => l.Tag == "zh-Hans" && l.NativeName == "简体中文");
    }

    private static Dictionary<string, Dictionary<string, string>> Catalogs()
    {
        const string prefix = "GridKrafter.Locales.";
        var assembly = typeof(GridKrafterLocaleCatalog).Assembly;
        Dictionary<string, Dictionary<string, string>> catalogs = [];
        foreach (string name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            var json = (JsonObject)JsonNode.Parse(stream)!;
            catalogs[name[prefix.Length..^".json".Length]] = json.ToDictionary(p => p.Key, p => p.Value!.GetValue<string>());
        }

        return catalogs;
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();
}
