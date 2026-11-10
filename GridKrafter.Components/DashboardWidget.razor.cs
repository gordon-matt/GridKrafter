namespace GridKrafter.Components;

/// <summary>
/// Declares a widget on a <see cref="Dashboard"/>, with its content written in place. The
/// position, size, title, colour and collapsed state are where it starts; once someone changes
/// them, the board remembers theirs. Content, limits and buttons always follow the declaration.
/// </summary>
public sealed partial class DashboardWidget : IDisposable
{
    /// <summary>
    /// Gets or sets the identifier; saved state is keyed by it, so keep it stable.
    /// </summary>
    [Parameter, EditorRequired] public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a catalog definition to render the body and offer its settings, when there's no <see cref="ChildContent"/>.
    /// </summary>
    [Parameter] public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the starting settings, over the definition's defaults.
    /// </summary>
    [Parameter] public JsonObject? Options { get; set; }

    /// <summary>
    /// Gets or sets the section to start in; inside a <see cref="WidgetSection"/>, that section.
    /// </summary>
    [Parameter] public string? Section { get; set; }

    /// <summary>
    /// Gets or sets the header text.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets rich header content, shown until someone renames the widget.
    /// </summary>
    [Parameter] public RenderFragment? TitleContent { get; set; }

    /// <summary>
    /// Gets or sets a built-in icon name or icon-font classes.
    /// </summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the starting header colour: a palette name or a CSS colour.
    /// </summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether it starts collapsed.
    /// </summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>
    /// Gets or sets the starting column in the largest layout; leave it unset to take the first free spot.
    /// </summary>
    [Parameter] public int? X { get; set; }

    /// <summary>
    /// Gets or sets the starting row in the largest layout.
    /// </summary>
    [Parameter] public int? Y { get; set; }

    /// <summary>
    /// Gets or sets the starting width, in columns of the largest layout.
    /// </summary>
    [Parameter] public int? W { get; set; }

    /// <summary>
    /// Gets or sets the starting height, in rows.
    /// </summary>
    [Parameter] public int? H { get; set; }

    /// <summary>
    /// Gets or sets the minimum width in columns.
    /// </summary>
    [Parameter] public int? MinW { get; set; }

    /// <summary>
    /// Gets or sets the minimum height in rows.
    /// </summary>
    [Parameter] public int? MinH { get; set; }

    /// <summary>
    /// Gets or sets the maximum width in columns.
    /// </summary>
    [Parameter] public int? MaxW { get; set; }

    /// <summary>
    /// Gets or sets the maximum height in rows.
    /// </summary>
    [Parameter] public int? MaxH { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether it stays put while other widgets flow around it.
    /// </summary>
    [Parameter] public bool Locked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the height follows the content.
    /// </summary>
    [Parameter] public bool AutoHeight { get; set; }

    /// <summary>
    /// Gets or sets the header buttons, replacing the dashboard's defaults.
    /// </summary>
    [Parameter] public WidgetButtons? Buttons { get; set; }

    /// <summary>
    /// Gets or sets the frame style, replacing the dashboard's default.
    /// </summary>
    [Parameter] public WidgetChrome? Chrome { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the body has no padding.
    /// </summary>
    [Parameter] public bool NoPadding { get; set; }

    /// <summary>
    /// Gets or sets how often, in seconds, it refreshes itself.
    /// </summary>
    [Parameter] public int? Refresh { get; set; }

    /// <summary>
    /// Gets or sets the body. Its context is the widget's <see cref="WidgetContext"/>.
    /// </summary>
    [Parameter] public RenderFragment<WidgetContext>? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets content for the header beside the title.
    /// </summary>
    [Parameter] public RenderFragment? Toolbar { get; set; }

    /// <summary>
    /// Gets or sets a bar across the top of the body.
    /// </summary>
    [Parameter] public RenderFragment? BodyToolbar { get; set; }

    /// <summary>
    /// Gets or sets a bar across the bottom.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Gets or sets extra fields for the edit panel.
    /// </summary>
    [Parameter] public RenderFragment<WidgetContext>? EditContent { get; set; }

    /// <summary>
    /// Gets or sets what the refresh button does, before content registered with <see cref="WidgetContext.OnRefresh"/> reloads.
    /// </summary>
    [Parameter] public EventCallback<WidgetContext> OnRefresh { get; set; }

    [CascadingParameter] private Dashboard? Dashboard { get; set; }

    [CascadingParameter] private WidgetSection? Parent { get; set; }

    internal string? SectionId => Section ?? Parent?.Id;

    /// <inheritdoc/>
    public void Dispose() => Dashboard?.Forget(this);

    /// <summary>A new board item as the declaration starts it.</summary>
    internal DashboardItem CreateItem()
    {
        var item = new DashboardItem
        {
            Id = Id,
            Section = SectionId,
            Title = Title,
            Color = Color,
            Collapsed = Collapsed,
            Options = Options?.DeepClone().AsObject(),
        };
        Apply(item);
        return item;
    }

    /// <summary>Copies what the declaration is always in charge of onto an item.</summary>
    /// <returns><see langword="true"/> when anything changed.</returns>
    internal bool Apply(DashboardItem item)
    {
        bool changed = item.Kind != Kind || item.Icon != Icon || item.X != X || item.Y != Y || item.W != W || item.H != H
            || item.MinW != MinW || item.MinH != MinH || item.MaxW != MaxW || item.MaxH != MaxH
            || item.Locked != Locked || item.AutoHeight != AutoHeight || item.Buttons != Buttons
            || item.Chrome != Chrome || item.NoPadding != NoPadding || item.Refresh != Refresh;
        item.Kind = Kind;
        item.Icon = Icon;
        item.X = X;
        item.Y = Y;
        item.W = W;
        item.H = H;
        item.MinW = MinW;
        item.MinH = MinH;
        item.MaxW = MaxW;
        item.MaxH = MaxH;
        item.Locked = Locked;
        item.AutoHeight = AutoHeight;
        item.Buttons = Buttons;
        item.Chrome = Chrome;
        item.NoPadding = NoPadding;
        item.Refresh = Refresh;
        return changed;
    }

    /// <inheritdoc/>
    protected override void OnParametersSet() => Dashboard?.Declare(this);

    /// <inheritdoc/>
    protected override bool ShouldRender() => false;
}
