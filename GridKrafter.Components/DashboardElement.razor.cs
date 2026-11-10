namespace GridKrafter.Components;

/// <summary>
/// <see cref="Dashboard"/> with string and boolean parameters, so it can be registered as the
/// <c>&lt;gk-dashboard&gt;</c> custom element or rendered from an MVC view. Boards, catalogs and
/// layouts are JSON. Changes raise DOM events (<c>gk-changed</c>, <c>gk-layout</c>,
/// <c>gk-mounts</c>, <c>gk-refresh</c>, <c>gk-edit-mode</c>, <c>gk-ready</c>), and with
/// <see cref="Name"/> set the board is also posted with the surrounding form.
/// </summary>
public sealed partial class DashboardElement : IAsyncDisposable
{
    private GridKrafterInterop interop = default!;
    private ElementReference host;
    private Dashboard? dashboard;
    private bool isAttached;
    private string? error;

    private DashboardBoard? board;
    private string? appliedBoard;
    private DashboardBoard? saved;
    private string? appliedSaved;
    private List<WidgetDefinition>? catalog;
    private string? appliedCatalog;
    private IReadOnlyList<DashboardLayout>? layouts;
    private string? appliedLayouts;
    private WidgetButtons defaultButtons = WidgetButtons.Default;
    private WidgetChrome defaultChrome = WidgetChrome.Panel;
    private string boardJson = string.Empty;

    /// <summary>
    /// Gets or sets the board as JSON: <c>{ "sections": [...], "items": [{ "id": "sales", "title": "Sales", "w": 6, "h": 4, "html": "..." }] }</c>.
    /// Setting it again with the board from a <c>gk-changed</c> event is ignored, so hosts can echo it back.
    /// </summary>
    [Parameter] public string? Board { get; set; }

    /// <summary>
    /// Gets or sets a board saved earlier, as JSON, applied over <see cref="Board"/>.
    /// </summary>
    [Parameter] public string? Saved { get; set; }

    /// <summary>
    /// Gets or sets the widgets people can add, as a JSON array of definitions: <c>[{ "kind": "clock", "name": "Clock", "w": 3, "h": 2 }]</c>.
    /// </summary>
    [Parameter] public string? Catalog { get; set; }

    /// <summary>
    /// Gets or sets the layouts as a JSON array: <c>[{ "id": "large", "minWidth": 1100, "columns": 12 }]</c>.
    /// </summary>
    [Parameter] public string? Layouts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dashboard starts in edit mode.
    /// </summary>
    [Parameter] public bool EditMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether nothing can be changed.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether widgets can be dragged outside edit mode.
    /// </summary>
    [Parameter] public bool MoveWithoutEditMode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether widgets stay where they're dropped.
    /// </summary>
    [Parameter] public bool Float { get; set; }

    /// <summary>
    /// Gets or sets the row height in pixels.
    /// </summary>
    [Parameter] public int RowHeight { get; set; } = Constants.Defaults.RowHeight;

    /// <summary>
    /// Gets or sets the space between widgets in pixels.
    /// </summary>
    [Parameter] public int Gap { get; set; } = Constants.Defaults.Gap;

    /// <summary>
    /// Gets or sets the theme: <c>light</c>, <c>dark</c>, <c>auto</c> or <c>bootstrap</c>.
    /// </summary>
    [Parameter] public string Theme { get; set; } = Constants.Themes.Light;

    /// <summary>
    /// Gets or sets the language as an IETF tag.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// Gets or sets a key to keep the board in the browser's local storage under.
    /// </summary>
    [Parameter] public string? StorageKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the toolbar.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; }

    /// <summary>
    /// Gets or sets the default header buttons as a comma-separated list: <c>refresh, color, edit, fullscreen, collapse, delete</c>, or <c>all</c> or <c>none</c>.
    /// </summary>
    [Parameter] public string? Buttons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether widget buttons only show on hover.
    /// </summary>
    [Parameter] public bool ButtonsOnHover { get; set; }

    /// <summary>
    /// Gets or sets the default frame: <c>panel</c>, <c>card</c> or <c>plain</c>.
    /// </summary>
    [Parameter] public string? Chrome { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether deleting asks first.
    /// </summary>
    [Parameter] public bool ConfirmDelete { get; set; } = true;

    /// <summary>
    /// Gets or sets a name for a hidden input holding the board JSON, for posting with a form.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// Gets or sets extra CSS classes for the dashboard.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await interop.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or ObjectDisposedException or TaskCanceledException)
        {
            // The circuit or page is already gone.
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized() => interop = new GridKrafterInterop(JS);

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        List<string> errors = [];

        if (!string.Equals(Board, appliedBoard, StringComparison.Ordinal))
        {
            appliedBoard = Board;
            var parsed = string.IsNullOrWhiteSpace(Board) ? Result<DashboardBoard>.Success(new DashboardBoard()) : DashboardJson.TryParse(Board);
            if (!parsed.IsSuccess)
            {
                errors.Add("The board attribute isn't a valid dashboard: " + Describe(parsed));
            }
            else if (dashboard is null || !JsonNode.DeepEquals(DashboardJson.ToNode(parsed.Value), DashboardJson.ToNode(dashboard.Current)))
            {
                // A host echoing back the board it was just sent must not reset the dashboard mid-use.
                board = parsed.Value;
            }
        }

        if (!string.Equals(Saved, appliedSaved, StringComparison.Ordinal))
        {
            appliedSaved = Saved;
            var parsed = DashboardJson.TryParse(Saved);
            saved = parsed.IsSuccess && !string.IsNullOrWhiteSpace(Saved) ? parsed.Value : null;
        }

        if (!string.Equals(Catalog, appliedCatalog, StringComparison.Ordinal))
        {
            appliedCatalog = Catalog;
            catalog = null;
            if (!string.IsNullOrWhiteSpace(Catalog))
            {
                try
                {
                    catalog = JsonSerializer.Deserialize(Catalog, ComponentsJsonContext.Default.ListWidgetDefinition)?
                        .Where(d => !string.IsNullOrWhiteSpace(d.Kind))
                        .ToList();
                }
                catch (JsonException ex)
                {
                    errors.Add("The catalog attribute isn't a valid list of widget definitions: " + ex.Message);
                }
            }
        }

        if (!string.Equals(Layouts, appliedLayouts, StringComparison.Ordinal))
        {
            appliedLayouts = Layouts;
            var parsed = DashboardJson.TryParseLayouts(Layouts);
            layouts = parsed.IsSuccess && parsed.Value.Count > 0 ? parsed.Value : null;
            if (!parsed.IsSuccess && !string.IsNullOrWhiteSpace(Layouts))
            {
                errors.Add("The layouts attribute isn't a valid list of layouts: " + Describe(parsed));
            }
        }

        defaultButtons = Enum.TryParse(Buttons, ignoreCase: true, out WidgetButtons buttons) ? buttons : WidgetButtons.Default;
        defaultChrome = Enum.TryParse(Chrome, ignoreCase: true, out WidgetChrome chrome) ? chrome : WidgetChrome.Panel;
        error = errors.Count > 0 ? string.Join(' ', errors) : null;
    }

    /// <inheritdoc/>
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            isAttached = true;
        }
    }

    private static string KindName(DashboardChangeKind kind) => JsonNamingPolicy.CamelCase.ConvertName(kind.ToString());

    private static string Describe(IResult result) =>
        string.Join(' ', result.Errors.Concat(result.ValidationErrors.Select(e => e.ErrorMessage)));

    private static JsonObject LayoutNode(DashboardLayout layout) => new()
    {
        ["id"] = layout.Id,
        ["columns"] = layout.Columns,
        ["minWidth"] = layout.MinWidth,
    };

    private async Task OnChangedAsync(DashboardChange change)
    {
        UpdateBoardJson(change.Board);
        await DispatchAsync(Constants.DomEvents.Changed, new JsonObject
        {
            ["kind"] = KindName(change.Kind),
            ["itemId"] = change.ItemId,
            ["sectionId"] = change.SectionId,
            ["layout"] = change.Layout,
            ["board"] = DashboardJson.ToNode(change.Board),
        });
    }

    private Task OnLayoutChangedAsync(DashboardLayout layout) =>
        DispatchAsync(Constants.DomEvents.Layout, LayoutNode(layout));

    private async Task OnReadyAsync()
    {
        if (dashboard is null)
        {
            return;
        }

        UpdateBoardJson(dashboard.Current);
        await DispatchAsync(Constants.DomEvents.Ready, new JsonObject
        {
            ["layout"] = LayoutNode(dashboard.ActiveLayout),
            ["board"] = DashboardJson.ToNode(dashboard.Current),
        });
    }

    private Task OnRefreshAsync(WidgetContext context) =>
        DispatchAsync(Constants.DomEvents.Refresh, new JsonObject { ["id"] = context.Id, ["kind"] = context.Kind });

    private Task OnEditModeChangedAsync(bool editing) =>
        DispatchAsync(Constants.DomEvents.EditMode, new JsonObject { ["editing"] = editing });

    private Task OnMountsChangedAsync(IReadOnlyList<WidgetMount> mounts)
    {
        var list = new JsonArray();
        foreach (var mount in mounts)
        {
            JsonNode item = new JsonObject { ["id"] = mount.Id, ["kind"] = mount.Kind, ["section"] = mount.Section };
            list.Add(item);
        }

        return DispatchAsync(Constants.DomEvents.Mounts, new JsonObject { ["mounts"] = list });
    }

    private void UpdateBoardJson(DashboardBoard current)
    {
        string json = DashboardJson.Serialize(current);
        if (!string.Equals(json, boardJson, StringComparison.Ordinal))
        {
            boardJson = json;
            if (!string.IsNullOrEmpty(Name))
            {
                StateHasChanged();
            }
        }
    }

    private async Task DispatchAsync(string eventName, JsonObject detail)
    {
        if (!isAttached)
        {
            return;
        }

        try
        {
            await interop.DispatchAsync(host, eventName, detail.ToJsonString());
        }
        catch (JSDisconnectedException)
        {
            // The page is gone; nobody is listening.
        }
    }
}
