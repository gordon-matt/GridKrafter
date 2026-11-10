namespace GridKrafter.Serialization;

/// <summary>
/// Source-generated JSON for boards, so they serialize without reflection in trimmed WebAssembly
/// builds. camelCase, skipping nulls, and lenient about trailing commas and comments in host JSON.
/// </summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(DashboardBoard))]
[JsonSerializable(typeof(List<DashboardLayout>))]
[JsonSerializable(typeof(GridRect))]
internal sealed partial class GridKrafterJsonContext : JsonSerializerContext
{
    private static GridKrafterJsonContext? indented;

    /// <summary>The same settings, indented for people to read.</summary>
    public static GridKrafterJsonContext Indented => indented ??= new GridKrafterJsonContext(new JsonSerializerOptions(Default.Options) { WriteIndented = true });
}
