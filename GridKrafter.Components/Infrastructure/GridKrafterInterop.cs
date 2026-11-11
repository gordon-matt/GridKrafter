namespace GridKrafter.Components.Infrastructure;

/// <summary>
/// Calls into <c>gridkrafter.js</c>. The browser only reports what the pointer and the page are
/// doing; every layout decision is made in .NET and rendered by Blazor.
/// </summary>
internal sealed class GridKrafterInterop(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/GridKrafter.Components/gridkrafter.js";

    private Task<IJSObjectReference>? module;

    private Task<IJSObjectReference> Module => module ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();

    /// <summary>Starts watching a dashboard's size, pointer and keyboard. Returns <c>{ width, offset }</c> as JSON.</summary>
    public async ValueTask<string> AttachAsync<T>(ElementReference root, DotNetObjectReference<T> handler, string optionsJson)
        where T : class =>
        await (await Module).InvokeAsync<string>("attach", root, handler, optionsJson);

    /// <summary>Called after each render: passes changed options, loads new URL and template bodies, and measures auto-height widgets.</summary>
    public async ValueTask AfterRenderAsync(ElementReference root, string? optionsJson) =>
        await (await Module).InvokeVoidAsync("afterRender", root, optionsJson);

    public async ValueTask DetachAsync(ElementReference root) =>
        await (await Module).InvokeVoidAsync("detach", root);

    /// <summary>Fetches a URL body again, or re-clones a template. Returns an error message, or null.</summary>
    public async ValueTask<string?> ReloadAsync(ElementReference root, string itemId) =>
        await (await Module).InvokeAsync<string?>("reload", root, itemId);

    public async ValueTask ScrollIntoViewAsync(ElementReference root, string itemId) =>
        await (await Module).InvokeVoidAsync("scrollIntoView", root, itemId);

    public async ValueTask LockScrollAsync(bool locked) =>
        await (await Module).InvokeVoidAsync("lockScroll", locked);

    /// <summary>The browser's offset from UTC in minutes, so timestamps show local time even when rendered on the server.</summary>
    public async ValueTask<int> TimeOffsetAsync() =>
        await (await Module).InvokeAsync<int>("timeOffset");

    public async ValueTask<string?> LoadAsync(string key) =>
        await (await Module).InvokeAsync<string?>("load", key);

    public async ValueTask SaveAsync(string key, string json) =>
        await (await Module).InvokeVoidAsync("save", key, json);

    public async ValueTask RemoveAsync(string key) =>
        await (await Module).InvokeVoidAsync("remove", key);

    public async ValueTask FocusAsync(ElementReference element) =>
        await (await Module).InvokeVoidAsync("focus", element);

    /// <summary>Collapses header buttons that no longer fit on one line into the widget's overflow menu.</summary>
    public async ValueTask FitHeaderAsync(ElementReference root) =>
        await (await Module).InvokeVoidAsync("fitHeader", root);

    public async ValueTask DispatchAsync(ElementReference element, string eventName, string detailJson) =>
        await (await Module).InvokeVoidAsync("dispatch", element, eventName, detailJson);

    public async ValueTask DisposeAsync()
    {
        if (module is { IsCompletedSuccessfully: true })
        {
            try
            {
                await module.Result.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, and the module with it.
            }
        }
    }
}
