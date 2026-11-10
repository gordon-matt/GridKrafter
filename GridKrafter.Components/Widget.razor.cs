using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace GridKrafter.Components;

/// <summary>
/// A widget; a header with an icon, a title, a toolbar and buttons to
/// refresh, recolour, rename, fill the window, collapse and delete, over a body with an optional
/// toolbar, timestamp and footer. Use it on its own anywhere, or let a <see cref="Dashboard"/>
/// lay widgets out and remember what people did to them.
/// </summary>
public sealed partial class Widget : IAsyncDisposable
{
    private const int MinimumSpinnerMilliseconds = 400;

    private readonly string titleId = $"gk-widget-{Guid.NewGuid():N}";
    private readonly string titleInputId = $"gk-title-{Guid.NewGuid():N}";
    private WidgetContext context = default!;
    private GridKrafterInterop? ownInterop;
    private GridKrafterText text = GridKrafterText.English;
    private ConfirmDialog? confirm;

    private bool collapsed;
    private bool? appliedCollapsed;
    private bool fullscreen;
    private bool? appliedFullscreen;
    private string? editedTitle;
    private string? appliedTitle;
    private string? pickedColor;
    private bool hasPickedColor;
    private string? appliedColor;

    private bool editing;
    private bool paletteOpen;
    private bool busy;
    private bool removed;
    private bool scrollLocked;
    private DateTimeOffset? updatedAt;
    private int? offsetMinutes;
    private CancellationTokenSource? timerCancellation;
    private int timerSeconds;
    private long? renderedRevision;
    private bool forceRender = true;

    /// <summary>
    /// Gets or sets the header text.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets rich header content, shown instead of <see cref="Title"/> until someone renames the widget.
    /// </summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>
    /// Gets or sets a built-in icon name, or icon-font classes such as <c>fa fa-comments</c>.
    /// </summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the header colour: a name from <see cref="WidgetColors.Names"/> or a CSS colour.
    /// </summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the callback for a colour picked from the palette.
    /// </summary>
    [Parameter] public EventCallback<string?> ColorChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback for a new title typed in the edit panel.
    /// </summary>
    [Parameter] public EventCallback<string?> TitleChanged { get; set; }

    /// <summary>
    /// Gets or sets how the widget is framed.
    /// </summary>
    [Parameter] public WidgetChrome Chrome { get; set; } = WidgetChrome.Panel;

    /// <summary>
    /// Gets or sets the header buttons.
    /// </summary>
    [Parameter] public WidgetButtons Buttons { get; set; } = WidgetButtons.Default;

    /// <summary>
    /// Gets or sets a value indicating whether the buttons only show while the pointer or focus is on the widget.
    /// </summary>
    [Parameter] public bool ButtonsOnHover { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the header shows.
    /// </summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>
    /// Gets or sets the callback for collapsing or expanding.
    /// </summary>
    [Parameter] public EventCallback<bool> CollapsedChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the widget fills the window.
    /// </summary>
    [Parameter] public bool Fullscreen { get; set; }

    /// <summary>
    /// Gets or sets the callback for entering or leaving full screen.
    /// </summary>
    [Parameter] public EventCallback<bool> FullscreenChanged { get; set; }

    /// <summary>
    /// Gets or sets what the refresh button does, before content registered with <see cref="WidgetContext.OnRefresh"/> reloads.
    /// </summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>
    /// Gets or sets how often, in seconds, to refresh automatically; zero never does. Collapsed widgets wait until they're expanded.
    /// </summary>
    [Parameter] public int RefreshInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether refreshable widgets show when they were last updated.
    /// </summary>
    [Parameter] public bool ShowTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets what the delete button does once confirmed. Without it, the widget simply disappears.
    /// </summary>
    [Parameter] public EventCallback OnDelete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether deleting asks first.
    /// </summary>
    [Parameter] public bool ConfirmDelete { get; set; } = true;

    /// <summary>
    /// Gets or sets the message in the delete confirmation, replacing the "can't be undone" warning.
    /// </summary>
    [Parameter] public string? DeleteMessage { get; set; }

    /// <summary>
    /// Gets or sets the body.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets content for the header beside the title. Content inside the body can also use <see cref="WidgetToolbar"/>.
    /// </summary>
    [Parameter] public RenderFragment? Toolbar { get; set; }

    /// <summary>
    /// Gets or sets a bar across the top of the body. Content inside the body can also use <see cref="WidgetBodyToolbar"/>.
    /// </summary>
    [Parameter] public RenderFragment? BodyToolbar { get; set; }

    /// <summary>
    /// Gets or sets a bar across the bottom. Content inside the body can also use <see cref="WidgetFooter"/>.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Gets or sets extra fields for the edit panel, under the title.
    /// </summary>
    [Parameter] public RenderFragment? EditContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the body has no padding, for tables, maps and charts that should reach the edges.
    /// </summary>
    [Parameter] public bool NoPadding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the loading spinner covers the body.
    /// </summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>
    /// Gets or sets the theme of a widget used on its own: <c>light</c>, <c>dark</c>, <c>auto</c> or <c>bootstrap</c>. Inside a dashboard, the dashboard's applies.
    /// </summary>
    [Parameter] public string Theme { get; set; } = Constants.Themes.Light;

    /// <summary>
    /// Gets or sets the language of a widget used on its own, as an IETF tag.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the id content sees in <see cref="WidgetContext.Id"/> when the widget is used on its own.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Gets or sets extra CSS classes.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Gets or sets attributes for the widget's element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the context the dashboard keeps for this widget. Set by <see cref="Dashboard"/>.
    /// </summary>
    [Parameter] public WidgetContext? ItemContext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header is a drag handle. Set by <see cref="Dashboard"/>.
    /// </summary>
    [Parameter] public bool Movable { get; set; }

    /// <summary>
    /// Gets or sets a number the dashboard changes whenever this widget needs to re-render, so a drag
    /// elsewhere on the board doesn't re-render every widget. Leave it unset outside a dashboard.
    /// </summary>
    [Parameter] public long? Revision { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [CascadingParameter] private Dashboard? Dashboard { get; set; }

    private bool IsStandalone => Dashboard is null;

    private GridKrafterInterop Interop => Dashboard?.Interop ?? (ownInterop ??= new GridKrafterInterop(JS));

    private string Direction => text.IsRightToLeft ? "rtl" : "ltr";

    private string CurrentTitle => editedTitle ?? Title ?? string.Empty;

    private string? CurrentColor => hasPickedColor ? pickedColor : Color;

    private bool CanRefresh => Has(WidgetButtons.Refresh) || RefreshInterval > 0;

    private string FrameClass
    {
        get
        {
            string? tone = CurrentColor;
            return string.Join(' ', new[]
            {
                IsStandalone ? "gk-root gk-standalone" : null,
                "gk-widget",
                Chrome switch { WidgetChrome.Card => "gk-widget-card", WidgetChrome.Plain => "gk-widget-plain", _ => "gk-widget-panel" },
                WidgetColors.IsNamed(tone) ? $"gk-colored gk-color-{tone!.ToLowerInvariant()}" : null,
                WidgetColors.IsCustom(tone) ? "gk-colored" : null,
                collapsed && !fullscreen ? "gk-widget-collapsed" : null,
                fullscreen ? "gk-widget-fullscreen" : null,
                NoPadding ? "gk-widget-flush" : null,
                ButtonsOnHover ? "gk-widget-hover-buttons" : null,
                editing ? "gk-widget-editing" : null,
                Class,
            }.Where(c => !string.IsNullOrEmpty(c)));
        }
    }

    private string? ToneStyle => WidgetColors.IsCustom(CurrentColor) ? $"--gk-tone: {CurrentColor};" : null;

    /// <summary>Escape only needs listening for while something can be closed, so typing in the body doesn't send every key to the server.</summary>
    private IReadOnlyDictionary<string, object>? FrameAttributes
    {
        get
        {
            if (!fullscreen && !editing && !paletteOpen)
            {
                return AdditionalAttributes;
            }

            var attributes = AdditionalAttributes is null ? [] : new Dictionary<string, object>(AdditionalAttributes);
            attributes["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, OnKeyDown);
            return attributes;
        }
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        forceRender = true;
        var task = callback.InvokeAsync(arg);
        bool pending = task.Status is not TaskStatus.RanToCompletion and not TaskStatus.Canceled;
        StateHasChanged();
        return pending ? RenderWhenDoneAsync(task) : Task.CompletedTask;
    }

    /// <summary>Stops the refresh timer and releases the page's scroll if the widget was full screen.</summary>
    /// <returns>A task that completes when everything is released.</returns>
    public async ValueTask DisposeAsync()
    {
        StopTimer();
        if (ReferenceEquals(context?.RefreshRequested?.Target, this))
        {
            context.RefreshRequested = null;
        }

        try
        {
            if (scrollLocked)
            {
                await Interop.LockScrollAsync(false);
            }

            if (ownInterop is not null)
            {
                await ownInterop.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or ObjectDisposedException or TaskCanceledException)
        {
            // The circuit or page is already gone.
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        context = ItemContext ?? new WidgetContext(new DashboardItem { Id = Id ?? titleId, Title = Title, Color = Color }, null, null);
        context.RefreshRequested = RefreshAsync;
        if (CanRefresh)
        {
            updatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        text = Dashboard?.Text
            ?? (Services.GetService<IGridKrafterLocalizer>() ?? GridKrafterLocaleCatalog.Shared).Resolve(Language);

        if (appliedCollapsed != Collapsed)
        {
            appliedCollapsed = Collapsed;
            collapsed = Collapsed;
        }

        if (appliedFullscreen != Fullscreen)
        {
            appliedFullscreen = Fullscreen;
            fullscreen = Fullscreen;
        }

        if (!string.Equals(appliedTitle, Title, StringComparison.Ordinal))
        {
            appliedTitle = Title;
            editedTitle = null;
        }

        if (!string.Equals(appliedColor, Color, StringComparison.Ordinal))
        {
            appliedColor = Color;
            hasPickedColor = false;
        }

        if (IsStandalone)
        {
            context.Item.Collapsed = collapsed;
            context.IsFullscreen = fullscreen;
            context.Text = text;
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldRender()
    {
        if (Revision is null || forceRender || Revision != renderedRevision)
        {
            forceRender = false;
            renderedRevision = Revision;
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        RestartTimerIfChanged();

        try
        {
            if (CanRefresh && offsetMinutes is null)
            {
                offsetMinutes = Dashboard?.OffsetMinutes ?? await Interop.TimeOffsetAsync();
                Render();
            }

            if (scrollLocked != fullscreen)
            {
                scrollLocked = fullscreen;
                await Interop.LockScrollAsync(fullscreen);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // Prerendering, or the page is going away.
        }
    }

    private bool Has(WidgetButtons button) => (Buttons & button) == button;

    /// <summary>Plain widgets have no header, so their buttons float over the content while the board is being arranged.</summary>
    private bool ShowFloatingControls =>
        Chrome == WidgetChrome.Plain && (context.IsEditing || fullscreen) && (Buttons != WidgetButtons.None || Movable);

    private string LocalTime(DateTimeOffset at) =>
        at.ToOffset(TimeSpan.FromMinutes(offsetMinutes ?? 0)).ToString("T", text.Culture);

    private void Render()
    {
        forceRender = true;
        StateHasChanged();
    }

    private async Task RenderWhenDoneAsync(Task task)
    {
        try
        {
            await task;
        }
        catch when (task.IsCanceled)
        {
            return;
        }

        forceRender = true;
        StateHasChanged();
    }

    private async Task RefreshAsync()
    {
        if (busy)
        {
            return;
        }

        busy = true;
        Render();
        try
        {
            var minimum = Task.Delay(MinimumSpinnerMilliseconds);
            await OnRefresh.InvokeAsync();
            await context.RunRefreshHandlersAsync();
            await minimum;
            updatedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            busy = false;
            Render();
        }
    }

    private void RestartTimerIfChanged()
    {
        int seconds = Math.Max(0, RefreshInterval);
        if (seconds == timerSeconds)
        {
            return;
        }

        StopTimer();
        timerSeconds = seconds;
        if (seconds > 0)
        {
            timerCancellation = new CancellationTokenSource();
            _ = RunTimerAsync(TimeSpan.FromSeconds(seconds), timerCancellation.Token);
        }
    }

    private void StopTimer()
    {
        timerCancellation?.Cancel();
        timerCancellation?.Dispose();
        timerCancellation = null;
        timerSeconds = 0;
    }

    private async Task RunTimerAsync(TimeSpan period, CancellationToken cancellation)
    {
        using var timer = new PeriodicTimer(period);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellation))
            {
                if (collapsed)
                {
                    continue;
                }

                try
                {
                    await InvokeAsync(RefreshAsync);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One failed refresh shouldn't stop the next; the widget keeps its last content.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped or disposed.
        }
    }

    private async Task ToggleCollapseAsync()
    {
        collapsed = !collapsed;
        paletteOpen = false;
        if (IsStandalone)
        {
            context.Item.Collapsed = collapsed;
            context.NotifyChanged();
        }

        await CollapsedChanged.InvokeAsync(collapsed);
    }

    private async Task ToggleFullscreenAsync()
    {
        fullscreen = !fullscreen;
        paletteOpen = false;
        if (IsStandalone)
        {
            context.IsFullscreen = fullscreen;
            context.NotifyChanged();
        }

        await FullscreenChanged.InvokeAsync(fullscreen);
    }

    private void TogglePalette() => paletteOpen = !paletteOpen;

    private void ClosePalette() => paletteOpen = false;

    private async Task ToggleEdit()
    {
        editing = !editing;
        paletteOpen = false;
        // Collapsed widgets hide the edit panel, so editing one has to open it first.
        if (editing && collapsed && !fullscreen)
        {
            collapsed = false;
            if (IsStandalone)
            {
                context.Item.Collapsed = false;
                context.NotifyChanged();
            }

            await CollapsedChanged.InvokeAsync(false);
        }
    }

    private async Task PickColorAsync(string? color)
    {
        pickedColor = color;
        hasPickedColor = true;
        paletteOpen = false;
        if (IsStandalone)
        {
            context.Item.Color = color;
        }

        await ColorChanged.InvokeAsync(color);
    }

    private async Task SetTitleAsync(string? title)
    {
        editedTitle = title ?? string.Empty;
        if (IsStandalone)
        {
            context.Item.Title = editedTitle;
        }

        await TitleChanged.InvokeAsync(editedTitle);
    }

    private async Task DeleteAsync()
    {
        paletteOpen = false;
        if (ConfirmDelete && confirm is not null)
        {
            string name = string.IsNullOrWhiteSpace(CurrentTitle) ? text["widget.untitled"] : CurrentTitle;
            bool confirmed = await confirm.ShowAsync(
                text.Format("widget.deleteTitle", name),
                DeleteMessage ?? text["widget.deleteMessage"],
                text["dialog.delete"],
                isDestructive: true);
            if (!confirmed)
            {
                return;
            }
        }

        if (OnDelete.HasDelegate)
        {
            await OnDelete.InvokeAsync();
        }
        else
        {
            removed = true;
        }
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape")
        {
            return;
        }

        if (paletteOpen)
        {
            paletteOpen = false;
        }
        else if (editing)
        {
            editing = false;
        }
        else if (fullscreen)
        {
            await ToggleFullscreenAsync();
        }
    }
}
