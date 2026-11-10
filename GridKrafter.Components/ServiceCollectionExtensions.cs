using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GridKrafter;

/// <summary>Settings for <c>AddGridKrafter</c>.</summary>
public sealed class GridKrafterOptions
{
    /// <summary>
    /// Gets where extra language files live.
    /// </summary>
    public GridKrafterLocalizationOptions Localization { get; } = new();

    /// <summary>
    /// Gets the widget definitions every dashboard can use, alongside any passed to a dashboard directly.
    /// </summary>
    public Components.WidgetRegistry Widgets { get; } = new();
}

/// <summary>
/// Registers GridKrafter's services.
/// </summary>
public static class ComponentsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Optional: dashboards work without it, using the built-in languages and the widgets passed
        /// to them. Call it to register widgets once for the whole app, or to add languages or
        /// override strings from <c>{tag}.json</c> files in a folder of your own.
        /// </summary>
        /// <param name="configure">Registers widgets and sets where language files live.</param>
        /// <returns>The services, for chaining.</returns>
        public IServiceCollection AddGridKrafter(Action<GridKrafterOptions>? configure = null)
        {
            var options = new GridKrafterOptions();
            configure?.Invoke(options);
            services.TryAddSingleton<IGridKrafterLocalizer>(new GridKrafterLocaleCatalog(options.Localization));
            services.TryAddSingleton<Components.IWidgetRegistry>(options.Widgets);
            return services;
        }
    }
}
