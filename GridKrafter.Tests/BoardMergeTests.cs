namespace GridKrafter.Tests;

public class BoardMergeTests
{
    [Fact]
    public void Apply_KnownItem_TakesSavedStateButKeepsHostContent()
    {
        var current = Board(new DashboardItem { Id = "a", Title = "Sales", Html = "<p>new</p>", MinW = 3 });
        var saved = Board(new DashboardItem
        {
            Id = "a",
            Title = "My sales",
            Html = "<p>old</p>",
            MinW = 1,
            Color = "red",
            Collapsed = true,
            Placements = { ["large"] = new GridRect(4, 0, 4, 2) },
        });

        var item = BoardMerge.Apply(current, saved).Items.Single();

        Assert.Equal("My sales", item.Title);
        Assert.Equal("red", item.Color);
        Assert.True(item.Collapsed);
        Assert.Equal(new GridRect(4, 0, 4, 2), item.Placements["large"]);
        Assert.Equal("<p>new</p>", item.Html);
        Assert.Equal(3, item.MinW);
    }

    [Fact]
    public void Apply_SavedItemHostNoLongerHas_IsDropped()
    {
        var merged = BoardMerge.Apply(Board(), Board(new DashboardItem { Id = "gone" }));

        Assert.Empty(merged.Items);
    }

    [Fact]
    public void Apply_SavedItemTheCallerKeeps_IsAdded()
    {
        var merged = BoardMerge.Apply(Board(), Board(new DashboardItem { Id = "clock-1", Kind = "clock" }), item => item.Kind == "clock");

        Assert.Equal("clock-1", Assert.Single(merged.Items).Id);
    }

    [Fact]
    public void Apply_NewHostItem_IsKept()
    {
        var merged = BoardMerge.Apply(Board(new DashboardItem { Id = "fresh" }), Board());

        Assert.Equal("fresh", Assert.Single(merged.Items).Id);
    }

    [Fact]
    public void Apply_Sections_SavedOrderFirstThenNewOnes()
    {
        var current = new DashboardBoard { Sections = [Section("a"), Section("b"), Section("c")] };
        var saved = new DashboardBoard { Sections = [Section("b", "Renamed"), Section("mine"), Section("a")] };

        var merged = BoardMerge.Apply(current, saved);

        Assert.Equal(["b", "mine", "a", "c"], merged.Sections.Select(s => s.Id));
        Assert.Equal("Renamed", merged.Sections[0].Title);
    }

    [Fact]
    public void Apply_SavedCollapsedSectionNoLongerCollapsible_StaysOpen()
    {
        var current = new DashboardBoard { Sections = [new DashboardSection { Id = "a", Collapsible = false }] };
        var saved = new DashboardBoard { Sections = [new DashboardSection { Id = "a", Collapsed = true }] };

        Assert.False(BoardMerge.Apply(current, saved).Sections[0].Collapsed);
    }

    private static DashboardBoard Board(params DashboardItem[] items) => new() { Items = [.. items] };

    private static DashboardSection Section(string id, string? title = null) => new() { Id = id, Title = title ?? id };
}
