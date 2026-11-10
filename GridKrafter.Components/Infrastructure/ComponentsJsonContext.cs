namespace GridKrafter.Components.Infrastructure;

/// <summary>Source-generated JSON for catalogs passed as attributes, so trimmed WebAssembly builds need no reflection.</summary>
[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(List<WidgetDefinition>))]
internal sealed partial class ComponentsJsonContext : JsonSerializerContext;
