namespace GridKrafter.Demo.Blazor.Widgets;

/// <summary>The kinds of widget the demo's catalog offers.</summary>
public static class DemoWidgets
{
    /// <summary>Registers the demo widgets.</summary>
    /// <param name="widgets">The app's registry.</param>
    public static void Register(WidgetRegistry widgets) => widgets
        .Add<StatWidget>("stat", d =>
        {
            d.Name = "Statistic";
            d.Description = "A headline number with its change since last week.";
            d.Icon = "trending-up";
            d.W = 3;
            d.H = 2;
            d.MinW = 2;
            d.MinH = 2;
            d.MaxH = 3;
            d.Options =
            [
                WidgetOption.Text("label", "Label", "Revenue"),
                WidgetOption.Text("value", "Value", "$48,200"),
                WidgetOption.Number("change", "Change (%)", 3.2, -100, 100, 0.1),
            ];
        })
        .Add<SalesChart>("sales", d =>
        {
            d.Name = "Sales chart";
            d.Description = "Revenue and orders over a week, month or year.";
            d.Icon = "chart";
            d.Color = "blue";
            d.W = 6;
            d.H = 5;
            d.MinW = 3;
            d.MinH = 4;
            d.Options =
            [
                WidgetOption.Select("range", "Range", "week", ("week", "This week"), ("month", "This month"), ("year", "This year")),
                WidgetOption.Switch("orders", "Show orders", true),
                WidgetOption.Color("accent", "Bar colour", "#3b82f6"),
            ];
        })
        .Add<ServerLoad>("server-load", d =>
        {
            d.Name = "Server load";
            d.Description = "CPU, memory, disk and network, refreshed every few seconds.";
            d.Icon = "server";
            d.Color = "green";
            d.W = 4;
            d.H = 4;
            d.MinW = 3;
            d.AutoHeight = true;
            d.Refresh = 5;
        })
        .Add<OrdersTable>("orders", d =>
        {
            d.Name = "Recent orders";
            d.Description = "A filterable, paged table.";
            d.Icon = "cart";
            d.W = 8;
            d.H = 6;
            d.MinW = 4;
            d.MinH = 4;
            d.NoPadding = true;
            d.Options = [WidgetOption.Range("pageSize", "Rows per page", 5, 3, 10)];
        })
        .Add<TodoList>("todo", d =>
        {
            d.Name = "To-do list";
            d.Description = "Tasks that are saved with the board.";
            d.Icon = "list";
            d.Color = "orange";
            d.W = 4;
            d.H = 5;
            d.MinW = 3;
            d.MinH = 3;
            d.Options = [WidgetOption.Switch("hideDone", "Hide finished tasks")];
        })
        .Add<ClockWidget>("clock", d =>
        {
            d.Name = "Clock";
            d.Description = "The time and date where you are.";
            d.Icon = "clock";
            d.W = 3;
            d.H = 2;
            d.MinW = 2;
            d.MinH = 2;
            d.Options =
            [
                WidgetOption.Switch("hour12", "12-hour clock"),
                WidgetOption.Switch("seconds", "Show seconds", true),
            ];
        })
        .Add<NotesWidget>("notes", d =>
        {
            d.Name = "Notes";
            d.Description = "A scratch pad that's saved with the board.";
            d.Icon = "note";
            d.Color = "yellow";
            d.W = 4;
            d.H = 4;
            d.MinW = 2;
            d.MinH = 2;
            d.NoPadding = true;
        })
        .Add<CalendarWidget>("calendar", d =>
        {
            d.Name = "Calendar";
            d.Description = "This month at a glance.";
            d.Icon = "calendar";
            d.W = 4;
            d.H = 5;
            d.MinW = 3;
            d.AutoHeight = true;
            d.Options = [WidgetOption.Select("firstDay", "Weeks start on", "monday", ("monday", "Monday"), ("sunday", "Sunday"))];
        })
        .Add<LinksWidget>("links", d =>
        {
            d.Name = "Bookmarks";
            d.Description = "Links you visit often.";
            d.Icon = "link";
            d.W = 3;
            d.H = 4;
            d.MinW = 2;
            d.AutoHeight = true;
            d.Options =
            [
                WidgetOption.TextArea("links", "Links, one per line as title | address",
                    "GridKrafter | https://github.com\nBlazor | https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor\nMDN | https://developer.mozilla.org"),
            ];
        });
}
