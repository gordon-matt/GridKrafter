namespace GridKrafter.Components;

/// <summary>The widget definitions every dashboard in the app can use, registered with <c>AddGridKrafter</c>.</summary>
public interface IWidgetRegistry
{
    /// <summary>
    /// Gets the definitions, in registration order.
    /// </summary>
    IReadOnlyList<WidgetDefinition> Definitions { get; }

    /// <summary>The definition for a kind.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The definition, or <see langword="null"/>.</returns>
    WidgetDefinition? Find(string? kind);
}

/// <summary>A list of widget definitions where a later registration replaces an earlier one of the same kind.</summary>
public sealed class WidgetRegistry : IWidgetRegistry
{
    private readonly List<WidgetDefinition> definitions = [];

    /// <inheritdoc/>
    public IReadOnlyList<WidgetDefinition> Definitions => definitions;

    /// <inheritdoc/>
    public WidgetDefinition? Find(string? kind) =>
        kind is null ? null : definitions.Find(d => string.Equals(d.Kind, kind, StringComparison.Ordinal));

    /// <summary>Registers a definition.</summary>
    /// <param name="definition">The definition.</param>
    /// <returns>This registry, for chaining.</returns>
    public WidgetRegistry Add(WidgetDefinition definition)
    {
        definitions.RemoveAll(d => string.Equals(d.Kind, definition.Kind, StringComparison.Ordinal));
        definitions.Add(definition);
        return this;
    }

    /// <summary>Registers a component as a kind of widget.</summary>
    /// <typeparam name="TComponent">The component for the body.</typeparam>
    /// <param name="kind">The kind identifier.</param>
    /// <param name="configure">Sets the name, size, options and so on.</param>
    /// <returns>This registry, for chaining.</returns>
    public WidgetRegistry Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(string kind, Action<WidgetDefinition>? configure = null)
        where TComponent : IComponent
    {
        var definition = WidgetDefinition.For<TComponent>(kind);
        configure?.Invoke(definition);
        return Add(definition);
    }
}
