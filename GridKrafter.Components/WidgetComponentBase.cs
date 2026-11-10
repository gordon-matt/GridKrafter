namespace GridKrafter.Components;

/// <summary>
/// An optional base for widget content. It picks up the cascading <see cref="WidgetContext"/>,
/// re-renders when the widget's size or settings change, and calls <see cref="OnRefreshAsync"/>
/// when the widget refreshes.
/// </summary>
public abstract class WidgetComponentBase : ComponentBase, IDisposable
{
    private WidgetContext? subscribed;
    private IDisposable? refreshSubscription;

    /// <summary>
    /// Gets or sets the widget this content is in; <see langword="null"/> outside a widget.
    /// </summary>
    [CascadingParameter] protected WidgetContext? Context { get; set; }

    /// <summary>Unsubscribes from the widget.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>A setting of this widget, or a fallback outside a widget.</summary>
    /// <typeparam name="T">A string, a number type or <see cref="bool"/>.</typeparam>
    /// <param name="key">The setting's key.</param>
    /// <param name="fallback">The value when there's none.</param>
    /// <returns>The value.</returns>
    protected T? Option<T>(string key, T? fallback = default) =>
        Context is null ? fallback : Context.Option(key, fallback);

    /// <summary>Reloads the content. Called by the refresh button, the refresh timer and <see cref="WidgetContext.RefreshAsync"/>.</summary>
    /// <returns>A task that completes when the content has reloaded.</returns>
    protected virtual Task OnRefreshAsync() => Task.CompletedTask;

    /// <summary>Called when the widget's size, state or settings change, before it re-renders.</summary>
    protected virtual void OnWidgetChanged()
    {
    }

    /// <summary>Releases what the content holds. Override it to stop timers and the like, and call the base.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Unsubscribe();
        }
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(subscribed, Context))
        {
            return;
        }

        Unsubscribe();
        subscribed = Context;
        if (subscribed is not null)
        {
            subscribed.Changed += HandleChanged;
            refreshSubscription = subscribed.OnRefresh(RefreshAndRenderAsync);
        }
    }

    private async Task RefreshAndRenderAsync()
    {
        await OnRefreshAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void HandleChanged() => _ = InvokeAsync(() =>
    {
        OnWidgetChanged();
        StateHasChanged();
    });

    private void Unsubscribe()
    {
        if (subscribed is not null)
        {
            subscribed.Changed -= HandleChanged;
        }

        refreshSubscription?.Dispose();
        refreshSubscription = null;
        subscribed = null;
    }
}
