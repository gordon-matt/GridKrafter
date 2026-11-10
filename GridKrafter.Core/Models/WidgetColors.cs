using System.Text.RegularExpressions;

namespace GridKrafter.Models;

/// <summary>
/// The header colours people can pick from. The stylesheet defines each one for light and dark
/// themes; hosts can also use any CSS colour for <see cref="DashboardItem.Color"/>.
/// </summary>
public static partial class WidgetColors
{
    /// <summary>The palette, in the order the picker shows it.</summary>
    public static IReadOnlyList<string> Names { get; } =
    [
        "slate", "red", "orange", "amber", "yellow", "lime", "green", "teal",
        "cyan", "sky", "blue", "indigo", "violet", "purple", "pink", "rose",
    ];

    /// <summary>Whether a colour is one of <see cref="Names"/>.</summary>
    /// <param name="color">The colour.</param>
    /// <returns><see langword="true"/> for a palette name.</returns>
    public static bool IsNamed(string? color) =>
        color is not null && Names.Contains(color, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a colour that isn't a palette name is safe to put in a style attribute: a hex colour
    /// or a CSS colour function such as <c>rgb()</c>, <c>hsl()</c> or <c>oklch()</c>.
    /// </summary>
    /// <param name="color">The colour.</param>
    /// <returns><see langword="true"/> when it can be used as a custom colour.</returns>
    public static bool IsCustom(string? color) =>
        !string.IsNullOrWhiteSpace(color) && !IsNamed(color) && CustomColor().IsMatch(color);

    [GeneratedRegex(@"^(#[0-9a-fA-F]{3,8}|(rgb|rgba|hsl|hsla|hwb|lab|lch|oklab|oklch)\([0-9a-zA-Z.,%/\s+-]{1,80}\))$")]
    private static partial Regex CustomColor();
}
