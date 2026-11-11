namespace GridKrafter.Components.Infrastructure;

/// <summary>
/// What <c>gridkrafter.js</c> calls on a dashboard. It's a separate object so the invokable
/// methods aren't part of the component's public API.
/// </summary>
internal sealed class DashboardScriptHandler(Dashboard owner)
{
    [JSInvokable]
    public Task OnWidth(double width) => owner.SetWidthAsync(width);

    [JSInvokable]
    public bool OnDragStart(string id) => owner.StartInteraction(id, isResize: false);

    [JSInvokable]
    public void OnDragMove(string section, int x, int y) => owner.MoveTo(section, x, y);

    [JSInvokable]
    public Task OnDragEnd(bool commit) => owner.EndInteractionAsync(commit);

    [JSInvokable]
    public bool OnResizeStart(string id) => owner.StartInteraction(id, isResize: true);

    [JSInvokable]
    public void OnResizeMove(int x, int y, int w, int h) => owner.ResizeTo(new GridRect(x, y, w, h));

    [JSInvokable]
    public Task OnResizeEnd(bool commit) => owner.EndInteractionAsync(commit);

    [JSInvokable]
    public Task OnNudge(string id, int dx, int dy, bool resize) => owner.NudgeAsync(id, dx, dy, resize);

    [JSInvokable]
    public void OnMeasured(string json) => owner.SetMeasured(json);

    [JSInvokable]
    public void OnEditGrown(string json) => owner.SetEditGrown(json);

    [JSInvokable]
    public Task OnOption(string id, string key, string valueJson) => owner.SetScriptOptionAsync(id, key, valueJson);
}
