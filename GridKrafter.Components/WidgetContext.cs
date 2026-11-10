namespace GridKrafter.Components;

/// <summary>
/// What a widget's content knows about the widget it's in. It cascades to everything in the
/// body, so content components can read and save their settings, add to the header and footer,
/// and take part in refreshes.
/// </summary>
public sealed class WidgetContext
{
    private readonly List<Func<Task>> refreshHandlers = [];
    private readonly Func<string, JsonNode?, Task>? saveOption;

    internal WidgetContext(DashboardItem item, WidgetDefinition? definition, Func<string, JsonNode?, Task>? saveOption)
    {
        Item = item;
        Definition = definition;
        this.saveOption = saveOption;
    }

    /// <summary>Raised when the widget's size, state or settings change, so content can re-render.</summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the widget's id.
    /// </summary>
    public string Id => Item.Id;

    /// <summary>
    /// Gets the widget's kind, when it came from a catalog.
    /// </summary>
    public string? Kind => Item.Kind;

    /// <summary>
    /// Gets the item on the board. Change it through the dashboard, not directly.
    /// </summary>
    public DashboardItem Item { get; internal set; }

    /// <summary>
    /// Gets the catalog definition, when there is one.
    /// </summary>
    public WidgetDefinition? Definition { get; internal set; }

    /// <summary>
    /// Gets where the widget sits in the current layout, in cells.
    /// </summary>
    public GridRect Rect { get; internal set; }

    /// <summary>
    /// Gets the number of columns in the current layout.
    /// </summary>
    public int Columns { get; internal set; } = 12;

    /// <summary>
    /// Gets a value indicating whether only the header shows.
    /// </summary>
    public bool IsCollapsed => Item.Collapsed;

    /// <summary>
    /// Gets a value indicating whether the widget fills the window.
    /// </summary>
    public bool IsFullscreen { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the dashboard is in edit mode.
    /// </summary>
    public bool IsEditing { get; internal set; }

    /// <summary>
    /// Gets the dashboard's strings, for content that wants to match its language.
    /// </summary>
    public GridKrafterText Text { get; internal set; } = GridKrafterText.English;

    /// <summary>
    /// Gets the browser's offset from UTC, so content rendered on the server can show local times.
    /// </summary>
    public TimeSpan ClientOffset { get; internal set; }

    /// <summary>
    /// Gets the current time where the browser is.
    /// </summary>
    public DateTimeOffset LocalNow => DateTimeOffset.UtcNow.ToOffset(ClientOffset);

    internal object ToolbarSlot { get; } = new();

    internal object BodyToolbarSlot { get; } = new();

    internal object FooterSlot { get; } = new();

    internal Func<Task>? RefreshRequested { get; set; }

    /// <summary>A setting, from the widget's options or else the definition's default.</summary>
    /// <typeparam name="T">A string, a number type or <see cref="bool"/>.</typeparam>
    /// <param name="key">The setting's key.</param>
    /// <param name="fallback">The value when there's none, or it can't be read as <typeparamref name="T"/>.</param>
    /// <returns>The value.</returns>
    public T? Option<T>(string key, T? fallback = default) =>
        OptionNode(key) is JsonValue value ? Read(value, fallback) : fallback;

    /// <summary>A setting as JSON, for values that are lists or objects rather than a single string, number or flag.</summary>
    /// <param name="key">The setting's key.</param>
    /// <returns>The widget's value, else the definition's default, else <see langword="null"/>. Treat it as read-only and save changes with <see cref="SetOptionAsync"/>.</returns>
    public JsonNode? OptionNode(string key) =>
        Item.Options?[key] ?? Definition?.Options.Find(o => string.Equals(o.Key, key, StringComparison.Ordinal))?.Default;

    /// <summary>Saves a setting with the rest of the board, for content that keeps its own state, like a to-do list.</summary>
    /// <param name="key">The setting's key.</param>
    /// <param name="value">The value; <see langword="null"/> removes it.</param>
    /// <returns>A task that completes once the change has been applied.</returns>
    public Task SetOptionAsync(string key, JsonNode? value)
    {
        if (saveOption is not null)
        {
            return saveOption(key, value);
        }

        Item.Options ??= [];
        Item.Options[key] = value;
        return Task.CompletedTask;
    }

    /// <summary>Runs a handler whenever the widget refreshes, by its button, its timer, or <see cref="RefreshAsync"/>.</summary>
    /// <param name="handler">Reloads the content.</param>
    /// <returns>Dispose it to stop.</returns>
    public IDisposable OnRefresh(Func<Task> handler)
    {
        refreshHandlers.Add(handler);
        return new Subscription(() => refreshHandlers.Remove(handler));
    }

    /// <summary>Refreshes the widget as its refresh button would: spinner, handlers, then the timestamp.</summary>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshAsync() => RefreshRequested?.Invoke() ?? RunRefreshHandlersAsync();

    internal async Task RunRefreshHandlersAsync()
    {
        foreach (var handler in refreshHandlers.ToArray())
        {
            await handler();
        }
    }

    internal void NotifyChanged() => Changed?.Invoke();

    private static T? Read<T>(JsonValue value, T? fallback)
    {
        if (value.TryGetValue(out T? typed))
        {
            return typed;
        }

        var kind = value.GetValueKind();
        if (typeof(T) == typeof(string))
        {
            return (T)(object)(kind == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString());
        }

        if (kind == JsonValueKind.Number && value.TryGetValue(out double number))
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            try
            {
                return type == typeof(bool) ? fallback : (T)Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
            {
                return fallback;
            }
        }

        if (kind == JsonValueKind.String && value.TryGetValue(out string? text))
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (type == typeof(bool) && bool.TryParse(text, out bool flag))
            {
                return (T)(object)flag;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                try
                {
                    return (T)Convert.ChangeType(parsed, type, CultureInfo.InvariantCulture);
                }
                catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
                {
                    return fallback;
                }
            }
        }

        return fallback;
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            dispose?.Invoke();
            dispose = null;
        }
    }
}
