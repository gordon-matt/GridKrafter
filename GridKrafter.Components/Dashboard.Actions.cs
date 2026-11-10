namespace GridKrafter.Components;

/// <content>
/// Everything people and hosts can do to the board, and saving the result.
/// </content>
public sealed partial class Dashboard
{
    /// <summary>The board as JSON, for saving somewhere other than local storage.</summary>
    /// <param name="indented">Whether to format it for reading.</param>
    /// <returns>The JSON.</returns>
    public string ToJson(bool indented = false) => DashboardJson.Serialize(board, indented);

    /// <summary>Turns edit mode on or off.</summary>
    /// <param name="editing">Whether to edit.</param>
    /// <returns>A task that completes once the change has been reported.</returns>
    public async Task SetEditModeAsync(bool editing)
    {
        editing &= !ReadOnly;
        if (editing == editMode)
        {
            return;
        }

        editMode = editing;
        revision++;
        Invalidate();
        Render();
        await EditModeChanged.InvokeAsync(editMode);
    }

    /// <summary>Opens the catalog of widgets people can add.</summary>
    /// <param name="sectionId">The section to add to by default.</param>
    public void OpenCatalog(string? sectionId = null) => catalog?.Open(sectionId);

    /// <summary>Adds a widget from a definition at the first free spot in a section.</summary>
    /// <param name="kind">The definition's kind.</param>
    /// <param name="sectionId">The section; by default the first.</param>
    /// <returns>The new item, or <see langword="null"/> when there's no such definition.</returns>
    public async Task<DashboardItem?> AddWidgetAsync(string kind, string? sectionId = null)
    {
        if (DefinitionFor(kind) is not { } definition)
        {
            return null;
        }

        var item = new DashboardItem
        {
            Id = NewId(kind),
            Kind = kind,
            Section = sectionId,
            Title = definition.Name ?? kind,
            Icon = definition.Icon,
            Color = definition.Color,
            W = definition.W,
            H = definition.H,
            Options = definition.DefaultOptions(),
        };
        await AddItemAsync(item);
        return item;
    }

    /// <summary>Adds an item. Without a position it takes the first free spot.</summary>
    /// <param name="item">The item; its id must be new to the board.</param>
    /// <returns>A task that completes once the change has been reported.</returns>
    public async Task AddItemAsync(DashboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.IsNullOrWhiteSpace(item.Id) || board.Find(item.Id) is not null)
        {
            throw new ArgumentException($"The board already has an item '{item.Id}'.", nameof(item));
        }

        EnsureSections();
        item.Section = board.FindSection(item.Section)?.Id ?? Sections[0].Id;
        if (board.FindSection(item.Section) is { Collapsed: true } section)
        {
            section.Collapsed = false;
        }

        board.Items.Add(item);
        pendingScroll = item.Id;
        await CommitAsync(DashboardChangeKind.Added, item.Id, item.Section);
    }

    /// <summary>Deletes a widget: one the page defines is hidden so it can be restored, one added from the catalog is removed.</summary>
    /// <param name="id">The widget's id.</param>
    /// <returns>A task that completes once the change has been reported.</returns>
    public async Task RemoveAsync(string id)
    {
        if (board.Find(id) is not { } item)
        {
            return;
        }

        if (string.Equals(fullscreenId, id, StringComparison.Ordinal))
        {
            fullscreenId = null;
        }

        if (WillHide(item))
        {
            item.Hidden = true;
            await CommitAsync(DashboardChangeKind.Hidden, id, SectionOfItem(item));
        }
        else
        {
            board.Items.Remove(item);
            contexts.Remove(id);
            notified.Remove(id);
            await CommitAsync(DashboardChangeKind.Removed, id, SectionOfItem(item));
        }
    }

    /// <summary>Brings back a hidden widget at the first free spot.</summary>
    /// <param name="id">The widget's id.</param>
    /// <returns>A task that completes once the change has been reported.</returns>
    public async Task RestoreAsync(string id)
    {
        if (board.Find(id) is not { Hidden: true } item)
        {
            return;
        }

        item.Hidden = false;
        item.Placements = [];
        item.X = null;
        item.Y = null;
        if (board.FindSection(SectionOfItem(item)) is { Collapsed: true } section)
        {
            section.Collapsed = false;
        }

        pendingScroll = id;
        await CommitAsync(DashboardChangeKind.Restored, id, SectionOfItem(item));
    }

    /// <summary>Puts the board back the way the page defines it, forgetting what was saved.</summary>
    /// <param name="confirmFirst">Whether to ask first.</param>
    /// <returns><see langword="true"/> when the board was reset.</returns>
    public async Task<bool> ResetAsync(bool confirmFirst = true)
    {
        if (confirmFirst && confirm is not null
            && !await confirm.ShowAsync(text["dashboard.resetTitle"], text["dashboard.resetMessage"], text["dialog.reset"], isDestructive: true))
        {
            return false;
        }

        var fresh = DashboardJson.Clone(initialBoard);
        board.Sections = fresh.Sections;
        board.Items = fresh.Items;
        ApplyDeclarations();
        board.Tidy();
        fullscreenId = null;
        interaction = null;
        contexts.Clear();
        notified.Clear();
        revision++;
        if (isAttached && !string.IsNullOrWhiteSpace(StorageKey))
        {
            await Interop.RemoveAsync(StorageKey);
        }

        await CommitAsync(DashboardChangeKind.Reset, persist: false);
        return true;
    }

    /// <summary>Refreshes a widget as its refresh button would.</summary>
    /// <param name="id">The widget's id.</param>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshAsync(string id) =>
        contexts.TryGetValue(id, out var context) ? context.RefreshAsync() : Task.CompletedTask;

    /// <summary>Adds a section at the end.</summary>
    /// <param name="title">The heading.</param>
    /// <returns>The new section.</returns>
    public async Task<DashboardSection> AddSectionAsync(string? title = null)
    {
        EnsureSections();
        var section = new DashboardSection { Id = NewId("section"), Title = title ?? text["section.untitled"] };
        board.Sections.Add(section);
        await CommitAsync(DashboardChangeKind.SectionAdded, sectionId: section.Id);
        return section;
    }

    internal async Task RenameSectionAsync(string id, string? title)
    {
        if (board.FindSection(id) is { } section && !string.Equals(section.Title, title, StringComparison.Ordinal))
        {
            section.Title = title;
            await CommitAsync(DashboardChangeKind.SectionChanged, sectionId: id);
        }
    }

    internal async Task ToggleSectionAsync(string id)
    {
        if (board.FindSection(id) is { Collapsible: true } section)
        {
            section.Collapsed = !section.Collapsed;
            await CommitAsync(DashboardChangeKind.SectionChanged, sectionId: id);
        }
    }

    internal async Task MoveSectionAsync(string id, int offset)
    {
        int index = board.Sections.FindIndex(s => string.Equals(s.Id, id, StringComparison.Ordinal));
        int target = index + offset;
        if (index < 0 || target < 0 || target >= board.Sections.Count)
        {
            return;
        }

        (board.Sections[index], board.Sections[target]) = (board.Sections[target], board.Sections[index]);
        await CommitAsync(DashboardChangeKind.SectionChanged, sectionId: id);
    }

    internal async Task RemoveSectionAsync(string id)
    {
        int index = board.Sections.FindIndex(s => string.Equals(s.Id, id, StringComparison.Ordinal));
        if (index < 0 || board.Sections.Count < 2)
        {
            return;
        }

        var section = board.Sections[index];
        var items = board.Items.Where(i => string.Equals(SectionOfItem(i), id, StringComparison.Ordinal)).ToList();
        if (confirm is not null && !await confirm.ShowAsync(
            text.Format("section.deleteTitle", SectionLabel(section)),
            text["section.deleteMessage"],
            text["dialog.delete"],
            isDestructive: true))
        {
            return;
        }

        string neighbour = board.Sections[index > 0 ? index - 1 : 1].Id;
        foreach (var item in items)
        {
            item.Section = neighbour;
            item.Placements = [];
            item.X = null;
            item.Y = null;
        }

        board.Sections.RemoveAt(index);
        await CommitAsync(DashboardChangeKind.SectionRemoved, sectionId: id);
    }

    internal bool IsAdded(string kind) =>
        board.Items.Exists(i => string.Equals(i.Kind, kind, StringComparison.Ordinal) && !i.Hidden);

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)];

    private void EnsureSections()
    {
        if (board.Sections.Count == 0)
        {
            board.Sections.Add(implicitSection);
        }
    }

    private async Task SetCollapsedAsync(string id, bool collapsed)
    {
        if (board.Find(id) is { } item && item.Collapsed != collapsed)
        {
            item.Collapsed = collapsed;
            Touch(id);
            await CommitAsync(collapsed ? DashboardChangeKind.Collapsed : DashboardChangeKind.Expanded, id, SectionOfItem(item));
        }
    }

    private void SetFullscreen(string id, bool fullscreen)
    {
        fullscreenId = fullscreen ? id : null;
        revision++;
        Render();
    }

    private async Task SetTitleAsync(string id, string? title)
    {
        if (board.Find(id) is { } item && !string.Equals(item.Title, title, StringComparison.Ordinal))
        {
            item.Title = title;
            Touch(id);
            await CommitAsync(DashboardChangeKind.Retitled, id, SectionOfItem(item));
        }
    }

    private async Task SetColorAsync(string id, string? color)
    {
        if (board.Find(id) is { } item && !string.Equals(item.Color, color, StringComparison.Ordinal))
        {
            item.Color = color;
            Touch(id);
            await CommitAsync(DashboardChangeKind.Recolored, id, SectionOfItem(item));
        }
    }

    /// <summary>Saves a setting sent by the widget's content with a <c>gk-option</c> event.</summary>
    internal Task SetScriptOptionAsync(string id, string key, string valueJson)
    {
        JsonNode? value;
        try
        {
            value = JsonNode.Parse(valueJson);
        }
        catch (JsonException)
        {
            return Task.CompletedTask;
        }

        return SetOptionAsync(id, key, value);
    }

    private async Task SetOptionAsync(string id, string key, JsonNode? value)
    {
        // Content that writes its settings back whenever it renders would otherwise never settle.
        if (board.Find(id) is not { } item || string.IsNullOrEmpty(key) || JsonNode.DeepEquals(item.Options?[key], value))
        {
            return;
        }

        item.Options ??= [];
        if (value is null)
        {
            item.Options.Remove(key);
        }
        else
        {
            item.Options[key] = value.Parent is null ? value : value.DeepClone();
        }

        Touch(id);
        if (contexts.TryGetValue(id, out var context))
        {
            context.NotifyChanged();
        }

        await CommitAsync(DashboardChangeKind.Options, id, SectionOfItem(item));
    }

    private async Task RefreshItemAsync(string id)
    {
        if (board.Find(id) is not { } item || !contexts.TryGetValue(id, out var context))
        {
            return;
        }

        var definition = DefinitionFor(item.Kind);
        if (isAttached && (UrlOf(item, definition) is not null || TemplateOf(item, definition) is not null))
        {
            await Interop.ReloadAsync(root, id);
        }

        if (declaredWidgets.TryGetValue(id, out var declaration))
        {
            await declaration.OnRefresh.InvokeAsync(context);
        }

        await OnRefresh.InvokeAsync(context);
    }

    private async Task CommitAsync(DashboardChangeKind kind, string? itemId = null, string? sectionId = null, bool persist = true)
    {
        Invalidate();
        Render();
        if (persist && isAttached && !string.IsNullOrWhiteSpace(StorageKey))
        {
            try
            {
                await Interop.SaveAsync(StorageKey, DashboardJson.Serialize(board));
            }
            catch (JSException)
            {
                // Storage is full or blocked; the board still works for this visit.
            }
        }

        await BoardChanged.InvokeAsync(board);
        await OnChanged.InvokeAsync(new DashboardChange(kind, itemId, sectionId, layout.Id, board));
    }
}
