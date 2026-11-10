using System.Net;

namespace GridKrafter.Demo.Mvc.Models;

/// <summary>The board this page starts with, and the widgets its catalog offers.</summary>
public static class DemoBoard
{
    /// <summary>
    /// Gets the board as the page defines it, before anyone changes it. People's changes are saved
    /// separately and applied over it, so new widgets added here still show up for them.
    /// </summary>
    public static string Json { get; } = DashboardJson.Serialize(Create());

    /// <summary>
    /// Registers the catalog. Each kind's content is a partial view fetched by URL or a
    /// <c>&lt;template&gt;</c> in the view, so nothing here needs Blazor components.
    /// </summary>
    /// <param name="widgets">The app's registry.</param>
    public static void Register(WidgetRegistry widgets) => widgets
        .Add(new WidgetDefinition
        {
            Kind = "orders",
            Name = "Recent orders",
            Description = "A partial view of the latest orders, fetched again every minute.",
            Icon = "cart",
            Color = "blue",
            W = 8,
            H = 6,
            MinW = 4,
            MinH = 4,
            NoPadding = true,
            Url = "widgets/orders",
            Refresh = 60,
        })
        .Add(new WidgetDefinition
        {
            Kind = "server-load",
            Name = "Server load",
            Description = "CPU, memory, disk and network from the server, every five seconds.",
            Icon = "server",
            Color = "green",
            W = 4,
            H = 4,
            MinW = 2,
            AutoHeight = true,
            Url = "widgets/server-load",
            Refresh = 5,
        })
        .Add(new WidgetDefinition
        {
            Kind = "activity",
            Name = "Activity",
            Description = "What happened lately. Its partial view carries a script that runs on each load.",
            Icon = "activity",
            Color = "violet",
            W = 6,
            H = 4,
            MinW = 3,
            AutoHeight = true,
            Url = "widgets/activity",
            Refresh = 30,
        })
        .Add(new WidgetDefinition
        {
            Kind = "note",
            Name = "Note",
            Description = "Markup from a template in the view.",
            Icon = "note",
            Color = "amber",
            W = 4,
            H = 3,
            AutoHeight = true,
            Template = "#tpl-note",
        })
        .Add(new WidgetDefinition
        {
            Kind = "links",
            Name = "Links",
            Description = "Useful links, from a template too.",
            Icon = "link",
            Color = "sky",
            W = 4,
            H = 3,
            AutoHeight = true,
            Template = "#tpl-links",
        });

    private static DashboardBoard Create() => new()
    {
        Sections =
        [
            new() { Id = "overview", Title = "Overview", Icon = "home" },
            new() { Id = "sales", Title = "Sales", Icon = "cart" },
        ],
        Items =
        [
            Stat("revenue", "Revenue", "$48,200", "+3.2%", "green", 0),
            Stat("visitors", "Visitors", "12,840", "+8.1%", "blue", 3),
            Stat("orders-count", "Orders", "1,204", "-2.4%", "orange", 6),
            Stat("uptime", "Uptime", "99.98%", "+0.02%", "violet", 9),
            new() { Id = "activity", Kind = "activity", Section = "overview", Title = "Activity", X = 0, Y = 2, W = 8, H = 4 },
            new()
            {
                Id = "load",
                Kind = "server-load",
                Section = "overview",
                Title = "Server load",
                Buttons = WidgetButtons.Refresh | WidgetButtons.Fullscreen | WidgetButtons.Collapse | WidgetButtons.Delete,
                X = 8,
                Y = 2,
                W = 4,
                H = 4,
            },
            new() { Id = "orders", Kind = "orders", Section = "sales", Title = "Recent orders", X = 0, Y = 0, W = 8, H = 6 },
            new()
            {
                Id = "about",
                Section = "sales",
                Title = "How this page works",
                Icon = "info",
                X = 8,
                Y = 0,
                W = 4,
                H = 4,
                AutoHeight = true,
                Template = "#tpl-about",
            },
        ],
    };

    /// <summary>A headline figure whose body is an HTML string: the simplest kind of widget.</summary>
    private static DashboardItem Stat(string id, string label, string value, string change, string color, int x) => new()
    {
        Id = id,
        Section = "overview",
        Title = label,
        Color = color,
        Chrome = WidgetChrome.Plain,
        Buttons = WidgetButtons.Color | WidgetButtons.Delete,
        X = x,
        Y = 0,
        W = 3,
        H = 2,
        MinW = 2,
        MaxH = 3,
        Html = $"""
            <div class="demo-stat">
                <span class="demo-stat-label">{WebUtility.HtmlEncode(label)}</span>
                <strong class="demo-stat-value">{WebUtility.HtmlEncode(value)}</strong>
                <span class="demo-stat-change {(change.StartsWith('-') ? "demo-down" : "demo-up")}">{WebUtility.HtmlEncode(change)} <span class="gk-muted">vs last week</span></span>
            </div>
            """,
    };
}
