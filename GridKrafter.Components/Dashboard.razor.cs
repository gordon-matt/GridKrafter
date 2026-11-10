using Microsoft.Extensions.DependencyInjection;

namespace GridKrafter.Components;

/// <summary>
/// A board of widgets in collapsible sections that people drag, resize, collapse, recolour,
/// rename, add and remove, with a layout for each screen size. Declare widgets inside it with
/// <see cref="DashboardWidget"/>, offer more through a catalog, or pass a whole <see cref="Board"/>.
/// </summary>
public sealed partial class Dashboard : IAsyncDisposable
{
    private const int HeaderPixels = 44;

    private readonly Dictionary<string, WidgetContext> contexts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContextState> notified = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> revisions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> measuredRows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> storedHeights = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DashboardWidget> declaredWidgets = new(StringComparer.Ordinal);
    private readonly List<WidgetSection> declaredSections = [];
    private readonly DashboardSection implicitSection = new() { Id = Constants.DefaultSection };

    private ElementReference root;
    private GridKrafterInterop? interop;
    private DotNetObjectReference<DashboardScriptHandler>? handler;
    private IWidgetRegistry? registry;
    private GridKrafterText text = GridKrafterText.English;
    private ConfirmDialog? confirm;
    private WidgetCatalog? catalog;

    private DashboardBoard board = new();
    private DashboardBoard? appliedBoard;
    private DashboardBoard? appliedSaved;
    private DashboardBoard initialBoard = new();
    private HashSet<string> initialItems = new(StringComparer.Ordinal);
    private IReadOnlyList<DashboardLayout> layouts = DashboardLayout.Defaults;
    private IReadOnlyList<DashboardLayout>? appliedLayouts;
    private DashboardLayout layout = DashboardLayout.Defaults[0];
    private GridEngine? engine;
    private Dictionary<string, IReadOnlyList<GridNode>>? arrangement;
    private Interaction? interaction;
    private string? fullscreenId;
    private bool editMode;
    private bool? appliedEditMode;
    private bool isAttached;
    private bool isReady;
    private bool disposed;
    private long revision;
    private string? sentOptions;
    private string? mountSignature;
    private string? pendingScroll;
    private bool measuredWhileBusy;

    /// <summary>
    /// Gets or sets declarations: <see cref="WidgetSection"/> and <see cref="DashboardWidget"/>.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the board to show. It's changed in place as people use the dashboard, and
    /// <see cref="BoardChanged"/> reports it, so <c>@bind-Board</c> keeps a copy to save.
    /// Declared widgets and sections are added to it.
    /// </summary>
    [Parameter] public DashboardBoard? Board { get; set; }

    /// <summary>
    /// Gets or sets the callback for every change to the board.
    /// </summary>
    [Parameter] public EventCallback<DashboardBoard> BoardChanged { get; set; }

    /// <summary>
    /// Gets or sets a board saved earlier, for hosts that keep it on the server. It's applied over
    /// the board the page defines the way <see cref="BoardMerge"/> describes, and reset forgets it.
    /// </summary>
    [Parameter] public DashboardBoard? SavedBoard { get; set; }

    /// <summary>
    /// Gets or sets the callback for every change, saying what happened.
    /// </summary>
    [Parameter] public EventCallback<DashboardChange> OnChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback for switching layout as the dashboard's width crosses a breakpoint.
    /// </summary>
    [Parameter] public EventCallback<DashboardLayout> OnLayoutChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback for when the dashboard has measured itself and loaded any saved state.
    /// </summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>
    /// Gets or sets what any widget's refresh button does, before content registered with <see cref="WidgetContext.OnRefresh"/> reloads.
    /// </summary>
    [Parameter] public EventCallback<WidgetContext> OnRefresh { get; set; }

    /// <summary>
    /// Gets or sets the callback for when the set of empty widget bodies changes, for other frameworks to render into.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<WidgetMount>> OnMountsChanged { get; set; }

    /// <summary>
    /// Gets or sets the widgets people can add, on top of those registered with <c>AddGridKrafter</c>.
    /// </summary>
    [Parameter] public IReadOnlyList<WidgetDefinition>? Widgets { get; set; }

    /// <summary>
    /// Gets or sets the layouts, one per range of widths. By default: 12 columns from 1100px, 6 from 640px, then 1.
    /// </summary>
    [Parameter] public IReadOnlyList<DashboardLayout>? Layouts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether people can resize widgets, manage sections and add widgets.
    /// </summary>
    [Parameter] public bool EditMode { get; set; }

    /// <summary>
    /// Gets or sets the callback for entering or leaving edit mode.
    /// </summary>
    [Parameter] public EventCallback<bool> EditModeChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether nothing can be moved, resized, renamed, recoloured or deleted.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether widgets can be dragged by their header outside edit mode, as SmartAdmin widgets can.
    /// </summary>
    [Parameter] public bool MoveWithoutEditMode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether widgets stay where they're dropped instead of floating up to fill gaps.
    /// </summary>
    [Parameter] public bool Float { get; set; }

    /// <summary>
    /// Gets or sets the height of a row in pixels.
    /// </summary>
    [Parameter] public int RowHeight { get; set; } = Constants.Defaults.RowHeight;

    /// <summary>
    /// Gets or sets the space between widgets in pixels.
    /// </summary>
    [Parameter] public int Gap { get; set; } = Constants.Defaults.Gap;

    /// <summary>
    /// Gets or sets the theme: <c>light</c>, <c>dark</c>, <c>auto</c> to follow the system, or <c>bootstrap</c> to use the page's Bootstrap variables.
    /// </summary>
    [Parameter] public string Theme { get; set; } = Constants.Themes.Light;

    /// <summary>
    /// Gets or sets the language as an IETF tag such as <c>de</c> or <c>pt-BR</c>.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// Gets or sets a key to save the board in the browser's local storage under, and load it from next time.
    /// </summary>
    [Parameter] public string? StorageKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the bar with edit mode, the catalog, hidden widgets, the layout and reset.
    /// </summary>
    [Parameter] public bool ShowToolbar { get; set; }

    /// <summary>
    /// Gets or sets the header buttons of widgets that don't choose their own.
    /// </summary>
    [Parameter] public WidgetButtons DefaultButtons { get; set; } = WidgetButtons.Default;

    /// <summary>
    /// Gets or sets a value indicating whether widget buttons only show while the pointer or focus is on the widget.
    /// </summary>
    [Parameter] public bool ButtonsOnHover { get; set; }

    /// <summary>
    /// Gets or sets the frame of widgets that don't choose their own.
    /// </summary>
    [Parameter] public WidgetChrome DefaultChrome { get; set; } = WidgetChrome.Panel;

    /// <summary>
    /// Gets or sets a value indicating whether deleting a widget asks first.
    /// </summary>
    [Parameter] public bool ConfirmDelete { get; set; } = true;

    /// <summary>
    /// Gets or sets the body of widgets that have no declared content, component, HTML, URL or template.
    /// </summary>
    [Parameter] public RenderFragment<WidgetContext>? ItemContent { get; set; }

    /// <summary>
    /// Gets or sets extra CSS classes.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Gets or sets attributes for the dashboard's element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets the board as it is now.
    /// </summary>
    public DashboardBoard Current => board;

    /// <summary>
    /// Gets the layout showing now.
    /// </summary>
    public DashboardLayout ActiveLayout => layout;

    /// <summary>
    /// Gets a value indicating whether the dashboard is in edit mode.
    /// </summary>
    public bool IsEditing => editMode;

    internal GridKrafterText Text => text;

    internal GridKrafterInterop Interop => interop ??= new GridKrafterInterop(JS);

    internal int? OffsetMinutes { get; private set; }

    internal IReadOnlyList<DashboardLayout> AllLayouts => layouts;

    internal IReadOnlyList<DashboardItem> HiddenItems => [.. board.Items.Where(i => i.Hidden)];

    internal IReadOnlyList<DashboardSection> Sections => board.Sections.Count > 0 ? board.Sections : [implicitSection];

    internal IReadOnlyList<WidgetDefinition> Definitions
    {
        get
        {
            List<WidgetDefinition> all = [.. Widgets ?? []];
            foreach (var definition in registry?.Definitions ?? [])
            {
                if (!all.Exists(d => string.Equals(d.Kind, definition.Kind, StringComparison.Ordinal)))
                {
                    all.Add(definition);
                }
            }

            return all;
        }
    }

    internal bool CanEdit => !ReadOnly;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private GridEngine Engine => engine ??= new GridEngine(layout.Columns, Float ? GridCompaction.None : GridCompaction.Vertical);

    private GridCompaction Compaction => Float ? GridCompaction.None : GridCompaction.Vertical;

    private IReadOnlyDictionary<string, IReadOnlyList<GridNode>> Arrangement => arrangement ??= Arrange();

    private int CollapsedRows => Math.Max(1, (int)Math.Ceiling((double)(HeaderPixels + Gap) / (RowHeight + Gap)));

    private string RootClass => string.Join(' ', new[]
    {
        "gk-root gk-dashboard",
        editMode ? "gk-editing" : null,
        ReadOnly ? "gk-readonly" : null,
        interaction is not null ? "gk-interacting" : null,
        Class,
    }.Where(c => !string.IsNullOrEmpty(c)));

    private string RootStyle => string.Create(CultureInfo.InvariantCulture, $"--gk-row: {RowHeight}px; --gk-gap: {Gap}px;");

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        disposed = true;
        try
        {
            if (isAttached)
            {
                await Interop.DetachAsync(root);
            }

            if (interop is not null)
            {
                await interop.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException or ObjectDisposedException or TaskCanceledException)
        {
            // The circuit or page is already gone.
        }

        handler?.Dispose();
    }

    /// <inheritdoc/>
    protected override void OnInitialized() => registry = Services.GetService<IWidgetRegistry>();

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        revision++;
        text = (Services.GetService<IGridKrafterLocalizer>() ?? GridKrafterLocaleCatalog.Shared).Resolve(Language);

        if (!ReferenceEquals(Layouts, appliedLayouts))
        {
            appliedLayouts = Layouts;
            layouts = Layouts is { Count: > 0 } ? Layouts : DashboardLayout.Defaults;
            if (!layouts.Any(l => string.Equals(l.Id, layout.Id, StringComparison.Ordinal)))
            {
                layout = DashboardLayout.Order(layouts)[0];
            }
        }

        if (!ReferenceEquals(Board, appliedBoard))
        {
            appliedBoard = Board;
            if (Board is not null && !ReferenceEquals(Board, board))
            {
                board = Board;
                interaction = null;
                ApplyDeclarations();
                if (isReady)
                {
                    board.Tidy();
                    initialBoard = DashboardJson.Clone(board);
                    initialItems = new(board.Items.Select(i => i.Id), StringComparer.Ordinal);
                }
            }
        }

        if (isReady && !ReferenceEquals(SavedBoard, appliedSaved))
        {
            appliedSaved = SavedBoard;
            var fresh = DashboardJson.Clone(initialBoard);
            board.Sections = fresh.Sections;
            board.Items = fresh.Items;
            ApplySaved(SavedBoard);
            interaction = null;
        }

        if (appliedEditMode != EditMode)
        {
            appliedEditMode = EditMode;
            editMode = EditMode && !ReadOnly;
        }

        engine = null;
        Invalidate();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (disposed)
        {
            return;
        }

        try
        {
            if (firstRender)
            {
                NotifyContexts();
                await AttachAsync();
                return;
            }

            if (!isAttached)
            {
                return;
            }

            if (interaction is null)
            {
                string options = ScriptOptions();
                await Interop.AfterRenderAsync(root, string.Equals(options, sentOptions, StringComparison.Ordinal) ? null : options);
                sentOptions = options;
            }

            if (pendingScroll is { } scrollTo)
            {
                pendingScroll = null;
                await Interop.ScrollIntoViewAsync(root, scrollTo);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
        {
            return;
        }

        if (interaction is null)
        {
            NotifyContexts();
        }

        await PublishMountsAsync();
    }

    private async Task AttachAsync()
    {
        handler = DotNetObjectReference.Create(new DashboardScriptHandler(this));
        string options = ScriptOptions();
        string result = await Interop.AttachAsync(root, handler, options);
        sentOptions = options;
        isAttached = true;

        var attached = JsonNode.Parse(result);
        double width = attached?["width"]?.GetValue<double>() ?? 0;
        OffsetMinutes = attached?["offset"]?.GetValue<int>() ?? 0;

        if (board.Sections.Count == 0)
        {
            board.Sections.Add(implicitSection);
        }

        board.Tidy();
        initialBoard = DashboardJson.Clone(board);
        initialItems = new(board.Items.Select(i => i.Id), StringComparer.Ordinal);
        appliedSaved = SavedBoard;
        ApplySaved(SavedBoard);
        if (!string.IsNullOrWhiteSpace(StorageKey))
        {
            var stored = DashboardJson.TryParse(await Interop.LoadAsync(StorageKey));
            if (stored.IsSuccess)
            {
                ApplySaved(stored.Value);
            }
        }

        layout = DashboardLayout.ForWidth(layouts, width);
        engine = null;
        Invalidate();
        isReady = true;
        revision++;
        StateHasChanged();
        await OnReady.InvokeAsync();
        if (!string.Equals(layout.Id, DashboardLayout.Order(layouts)[0].Id, StringComparison.Ordinal))
        {
            await OnLayoutChanged.InvokeAsync(layout);
        }
    }

    /// <summary>Applies saved state; widgets only the saved board has must come from a definition, whose content wins over anything saved.</summary>
    private void ApplySaved(DashboardBoard? saved)
    {
        if (saved is null)
        {
            return;
        }

        BoardMerge.Apply(board, DashboardJson.Clone(saved), item => DefinitionFor(item.Kind) is not null);
        foreach (var item in board.Items.Where(i => !initialItems.Contains(i.Id)))
        {
            item.Html = null;
            item.Url = null;
            item.Template = null;
        }
    }

    private string ScriptOptions()
    {
        var options = new JsonObject
        {
            ["row"] = RowHeight,
            ["gap"] = Gap,
            ["layouts"] = new JsonArray([.. DashboardLayout.Order(layouts).Select(l => (JsonNode)new JsonObject { ["id"] = l.Id, ["minWidth"] = l.MinWidth })]),
            ["loadFailed"] = text["widget.loadFailed"],
        };
        return options.ToJsonString();
    }

    private void NotifyContexts()
    {
        foreach (var (id, context) in contexts)
        {
            var state = new ContextState(context.Rect, context.Columns, context.IsEditing, context.IsFullscreen, context.IsCollapsed, context.Text.Language, context.ClientOffset);
            if (!notified.TryGetValue(id, out var previous))
            {
                notified[id] = state;
            }
            else if (previous != state)
            {
                notified[id] = state;
                context.NotifyChanged();
            }
        }
    }

    private async Task PublishMountsAsync()
    {
        if (!isReady || !OnMountsChanged.HasDelegate)
        {
            return;
        }

        var mounts = Mounts();
        string signature = string.Join('|', mounts.Select(m => $"{m.Id}\u001f{m.Kind}\u001f{m.Section}"));
        if (string.Equals(signature, mountSignature, StringComparison.Ordinal))
        {
            return;
        }

        mountSignature = signature;
        await OnMountsChanged.InvokeAsync(mounts);
    }

    /// <summary>The widgets whose body is left empty for someone else to fill, in the order they render.</summary>
    internal IReadOnlyList<WidgetMount> Mounts()
    {
        List<WidgetMount> mounts = [];
        foreach (var section in Sections)
        {
            foreach (var item in VisibleItems(section.Id))
            {
                var definition = DefinitionFor(item.Kind);
                bool filled = declaredWidgets.ContainsKey(item.Id)
                    || definition?.Component is not null
                    || !string.IsNullOrEmpty(item.Html ?? definition?.Html)
                    || UrlOf(item, definition) is not null
                    || TemplateOf(item, definition) is not null
                    || ItemContent is not null;
                if (!filled)
                {
                    mounts.Add(new WidgetMount(item.Id, item.Kind, section.Id));
                }
            }
        }

        return mounts;
    }

    internal WidgetDefinition? DefinitionFor(string? kind)
    {
        if (kind is null)
        {
            return null;
        }

        return Widgets?.FirstOrDefault(d => string.Equals(d.Kind, kind, StringComparison.Ordinal)) ?? registry?.Find(kind);
    }

    internal void Declare(WidgetSection declaration)
    {
        if (string.IsNullOrWhiteSpace(declaration.Id))
        {
            return;
        }

        if (!declaredSections.Contains(declaration))
        {
            declaredSections.Add(declaration);
        }

        var section = board.FindSection(declaration.Id);
        if (section is null)
        {
            board.Sections.Add(CreateSection(declaration));
            if (isReady)
            {
                initialBoard.Sections.Add(CreateSection(declaration));
            }

            Invalidate();
            Render();
        }
        else if (section.Icon != declaration.Icon || section.Collapsible != declaration.Collapsible)
        {
            section.Icon = declaration.Icon;
            section.Collapsible = declaration.Collapsible;
            section.Collapsed &= section.Collapsible;
            Invalidate();
            Render();
        }
    }

    internal void Declare(DashboardWidget declaration)
    {
        if (string.IsNullOrWhiteSpace(declaration.Id))
        {
            return;
        }

        declaredWidgets[declaration.Id] = declaration;
        var item = board.Find(declaration.Id);
        if (item is null)
        {
            board.Items.Add(declaration.CreateItem());
            if (isReady)
            {
                initialBoard.Items.Add(declaration.CreateItem());
                initialItems.Add(declaration.Id);
            }
        }
        else if (!declaration.Apply(item))
        {
            return;
        }

        Touch(declaration.Id);
        Invalidate();
        Render();
    }

    internal void Forget(WidgetSection declaration) => declaredSections.Remove(declaration);

    internal void Forget(DashboardWidget declaration)
    {
        if (disposed || !declaredWidgets.TryGetValue(declaration.Id, out var current) || !ReferenceEquals(current, declaration))
        {
            return;
        }

        declaredWidgets.Remove(declaration.Id);
        var item = board.Find(declaration.Id);
        if (item is not null)
        {
            board.Items.Remove(item);
            initialBoard.Items.RemoveAll(i => string.Equals(i.Id, declaration.Id, StringComparison.Ordinal));
            Invalidate();
            Render();
        }
    }

    internal long RevisionOf(string id) => revision + revisions.GetValueOrDefault(id);

    internal void Touch(string id) => revisions[id] = revisions.GetValueOrDefault(id) + 1;

    internal void Invalidate() => arrangement = null;

    internal void Render()
    {
        if (!disposed)
        {
            StateHasChanged();
        }
    }

    internal List<DashboardItem> VisibleItems(string sectionId) =>
        [.. board.Items.Where(i => !i.Hidden && string.Equals(SectionOfItem(i), sectionId, StringComparison.Ordinal))];

    private static DashboardSection CreateSection(WidgetSection declaration) => new()
    {
        Id = declaration.Id,
        Title = declaration.Title,
        Icon = declaration.Icon,
        Collapsible = declaration.Collapsible,
        Collapsed = declaration.Collapsed && declaration.Collapsible,
    };

    private static RenderFragment ComponentBody(WidgetDefinition definition) => builder =>
    {
        if (definition.Component is not null)
        {
            builder.OpenComponent(0, definition.Component);
            builder.CloseComponent();
        }
    };

    private static string? UrlOf(DashboardItem item, WidgetDefinition? definition) =>
        NullIfEmpty(item.Url ?? definition?.Url);

    private static string? TemplateOf(DashboardItem item, WidgetDefinition? definition) =>
        NullIfEmpty(item.Template ?? definition?.Template);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string CellStyle(GridRect rect) =>
        string.Create(CultureInfo.InvariantCulture, $"--gk-x: {rect.X}; --gk-y: {rect.Y}; --gk-w: {rect.W}; --gk-h: {rect.H};");

    private string SectionOfItem(DashboardItem item) => board.Sections.Count > 0 ? board.SectionOf(item) : implicitSection.Id;

    private void ApplyDeclarations()
    {
        foreach (var declaration in declaredSections.Where(d => board.FindSection(d.Id) is null))
        {
            board.Sections.Add(CreateSection(declaration));
        }

        foreach (var declaration in declaredWidgets.Values)
        {
            if (board.Find(declaration.Id) is { } item)
            {
                declaration.Apply(item);
            }
            else
            {
                board.Items.Add(declaration.CreateItem());
            }
        }
    }

    private Dictionary<string, IReadOnlyList<GridNode>> Arrange()
    {
        storedHeights.Clear();
        Dictionary<string, IReadOnlyList<GridNode>> result = new(StringComparer.Ordinal);
        foreach (var section in Sections)
        {
            var nodes = BoardArranger.Arrange(VisibleItems(section.Id), layouts, layout, Compaction, Describe);
            foreach (var node in nodes)
            {
                storedHeights[node.Id] = node.Rect.H;
            }

            result[section.Id] = ToDisplay(nodes);
        }

        return result;
    }

    private GridNode Describe(DashboardItem item)
    {
        var definition = DefinitionFor(item.Kind);
        int w = item.W ?? definition?.W ?? Constants.Defaults.Width;
        int h = item.H ?? definition?.H ?? Constants.Defaults.Height;
        return new GridNode(item.Id, new GridRect(item.X ?? 0, item.Y ?? 0, w, h))
        {
            MinW = Math.Max(1, item.MinW ?? definition?.MinW ?? 1),
            MinH = Math.Max(1, item.MinH ?? definition?.MinH ?? 1),
            MaxW = item.MaxW ?? definition?.MaxW,
            MaxH = item.MaxH ?? definition?.MaxH,
            Locked = item.Locked,
        };
    }

    /// <summary>Collapsed widgets shrink to their header and auto-height ones to their content; neither changes the stored height.</summary>
    private IReadOnlyList<GridNode> ToDisplay(IReadOnlyList<GridNode> nodes)
    {
        bool changed = false;
        List<GridNode> display = new(nodes.Count);
        foreach (var node in nodes)
        {
            int? rows = DisplayRows(board.Find(node.Id));
            if (rows is int height)
            {
                changed |= height != node.Rect.H;
                display.Add(node with { Rect = node.Rect with { H = height }, MinH = 1, MaxH = null });
            }
            else
            {
                display.Add(node);
            }
        }

        return changed ? Engine.Normalize(display) : display;
    }

    private int? DisplayRows(DashboardItem? item)
    {
        if (item is null)
        {
            return null;
        }

        if (item.Collapsed)
        {
            return CollapsedRows;
        }

        return MeasuresHeight(item, DefinitionFor(item.Kind)) && measuredRows.TryGetValue(item.Id, out int rows) && rows > 0 ? rows : null;
    }

    private bool HasDisplayHeight(DashboardItem item) => item.Collapsed || MeasuresHeight(item, DefinitionFor(item.Kind));

    private bool MeasuresHeight(DashboardItem item, WidgetDefinition? definition) =>
        !item.Collapsed && (item.AutoHeight || definition?.AutoHeight == true);

    /// <summary>Writes the active layout's positions for a section, keeping stored heights where the shown one is only for display.</summary>
    private void Freeze(IReadOnlyList<GridNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (board.Find(node.Id) is not { } item)
            {
                continue;
            }

            var rect = node.Rect;
            if (HasDisplayHeight(item))
            {
                int stored = item.Placements.TryGetValue(layout.Id, out var placed) ? placed.H : storedHeights.GetValueOrDefault(item.Id, rect.H);
                rect = rect with { H = stored };
            }

            item.Placements[layout.Id] = rect;
        }
    }

    private SectionView ViewOf(DashboardSection section)
    {
        var nodes = Arrangement.GetValueOrDefault(section.Id) ?? [];
        GridRect? placeholder = null;
        if (interaction is { } active)
        {
            if (string.Equals(section.Id, active.TargetSection, StringComparison.Ordinal))
            {
                nodes = active.Preview;
                placeholder = active.Preview.FirstOrDefault(n => string.Equals(n.Id, active.ItemId, StringComparison.Ordinal))?.Rect;
            }
            else if (string.Equals(section.Id, active.SourceSection, StringComparison.Ordinal))
            {
                nodes = active.SourcePreview ?? active.Snapshot;
            }
        }

        Dictionary<string, GridRect> rects = new(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            rects[node.Id] = node.Rect;
        }

        if (interaction is { } current && string.Equals(section.Id, current.SourceSection, StringComparison.Ordinal))
        {
            rects[current.ItemId] = current.Start.Rect;
        }

        var items = VisibleItems(section.Id).Where(i => rects.ContainsKey(i.Id)).ToList();
        int rows = 0;
        foreach (var item in items)
        {
            rows = Math.Max(rows, rects[item.Id].Bottom);
        }

        if (placeholder is { } shown)
        {
            rows = Math.Max(rows, shown.Bottom);
        }

        if (rows == 0 && (editMode || interaction is not null))
        {
            rows = 2;
        }

        return new SectionView(items, rects, placeholder, rows);
    }

    private WidgetContext ContextFor(DashboardItem item, WidgetDefinition? definition, GridRect rect)
    {
        if (!contexts.TryGetValue(item.Id, out var context))
        {
            string id = item.Id;
            context = new WidgetContext(item, definition, (key, value) => SetOptionAsync(id, key, value));
            contexts[item.Id] = context;
        }

        context.Item = item;
        context.Definition = definition;
        context.Rect = rect;
        context.Columns = layout.Columns;
        context.IsEditing = editMode;
        context.IsFullscreen = string.Equals(fullscreenId, item.Id, StringComparison.Ordinal);
        context.Text = text;
        context.ClientOffset = TimeSpan.FromMinutes(OffsetMinutes ?? 0);
        return context;
    }

    private string SectionClass(DashboardSection section) => string.Join(' ', new[]
    {
        "gk-section",
        section.Collapsed ? "gk-section-collapsed" : null,
        interaction is { } active && string.Equals(active.TargetSection, section.Id, StringComparison.Ordinal) ? "gk-section-target" : null,
    }.Where(c => c is not null));

    private string SectionLabel(DashboardSection section) =>
        string.IsNullOrWhiteSpace(section.Title) ? text["section.untitled"] : section.Title;

    private bool ShowsHeader(DashboardSection section) =>
        editMode || board.Sections.Count > 1 || !string.IsNullOrWhiteSpace(section.Title);

    private string GridStyle(int rows) =>
        string.Create(CultureInfo.InvariantCulture, $"--gk-columns: {layout.Columns}; --gk-rows: {rows};");

    private string ItemClass(DashboardItem item) => string.Join(' ', new[]
    {
        "gk-item",
        interaction is { } active && string.Equals(active.ItemId, item.Id, StringComparison.Ordinal) ? (active.IsResize ? "gk-item-active gk-item-resizing" : "gk-item-active") : null,
        item.Locked ? "gk-item-locked" : null,
        item.Collapsed ? "gk-item-collapsed" : null,
        string.Equals(fullscreenId, item.Id, StringComparison.Ordinal) ? "gk-item-fullscreen" : null,
    }.Where(c => c is not null));

    private bool TitleMatchesDeclaration(DashboardItem item, DashboardWidget? declaration) =>
        declaration?.TitleContent is not null && string.Equals(item.Title, declaration.Title, StringComparison.Ordinal);

    private WidgetChrome ChromeOf(DashboardItem item, WidgetDefinition? definition) =>
        item.Chrome ?? definition?.Chrome ?? DefaultChrome;

    private WidgetButtons ButtonsOf(DashboardItem item, WidgetDefinition? definition)
    {
        var buttons = item.Buttons ?? definition?.Buttons ?? DefaultButtons;
        if (item.Buttons is null && definition?.Buttons is null
            && (UrlOf(item, definition) is not null || (item.Refresh ?? definition?.Refresh ?? 0) > 0))
        {
            buttons |= WidgetButtons.Refresh;
        }

        return ReadOnly ? buttons & (WidgetButtons.Refresh | WidgetButtons.Fullscreen | WidgetButtons.Collapse) : buttons;
    }

    private string? DeleteMessageFor(DashboardItem item) => WillHide(item) ? text["widget.hiddenMessage"] : null;

    private bool WillHide(DashboardItem item) => initialItems.Contains(item.Id) || declaredWidgets.ContainsKey(item.Id) || DefinitionFor(item.Kind) is null;

    private bool CanMove(DashboardItem item) =>
        !ReadOnly && !item.Locked && fullscreenId is null && (editMode || MoveWithoutEditMode);

    private bool CanResize(DashboardItem item) => !ReadOnly && !item.Locked && editMode && fullscreenId is null;

    private IEnumerable<string> ResizeEdges(DashboardItem item, WidgetDefinition? definition)
    {
        if (!CanResize(item))
        {
            return [];
        }

        return item.Collapsed || MeasuresHeight(item, definition) ? ["e", "w"] : ["e", "w", "s", "se", "sw"];
    }

    private sealed record SectionView(List<DashboardItem> Items, Dictionary<string, GridRect> Rects, GridRect? Placeholder, int Rows);

    private readonly record struct ContextState(GridRect Rect, int Columns, bool IsEditing, bool IsFullscreen, bool IsCollapsed, string Language, TimeSpan Offset);
}
