using static GridKrafter.Tests.TestData;

namespace GridKrafter.Tests;

public class BoardArrangerTests
{
    private static readonly IReadOnlyList<DashboardLayout> Layouts = DashboardLayout.Defaults;
    private static readonly DashboardLayout Large = Layouts[0];
    private static readonly DashboardLayout Medium = Layouts[1];
    private static readonly DashboardLayout Small = Layouts[2];

    [Fact]
    public void Arrange_LargestLayoutWithoutPlacements_UsesStartingPositions()
    {
        DashboardItem[] items = [Item("a", 0, 0, 6, 2), Item("b", 6, 0, 6, 3)];

        var nodes = BoardArranger.Arrange(items, Layouts, Large, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(0, 0, 6, 2), RectOf(nodes, "a"));
        Assert.Equal(new GridRect(6, 0, 6, 3), RectOf(nodes, "b"));
    }

    [Fact]
    public void Arrange_ItemsWithoutPosition_FillFirstFreeSpots()
    {
        DashboardItem[] items = [Item("a", 0, 0, 4, 2), Item("b", w: 4), Item("c", w: 6)];

        var nodes = BoardArranger.Arrange(items, Layouts, Large, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(4, 0, 4, 2), RectOf(nodes, "b"));
        Assert.Equal(new GridRect(0, 2, 6, 2), RectOf(nodes, "c"));
    }

    [Fact]
    public void Arrange_SavedPlacement_WinsOverStartingPosition()
    {
        var item = Item("a", 0, 0, 4, 2);
        item.Placements["large"] = new GridRect(8, 0, 4, 3);

        var nodes = BoardArranger.Arrange([item], Layouts, Large, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(8, 0, 4, 3), RectOf(nodes, "a"));
    }

    [Fact]
    public void Arrange_SmallestLayoutNeverEdited_StacksTheLargeLayout()
    {
        DashboardItem[] items = [Item("b", 6, 0, 6, 3), Item("a", 0, 0, 6, 2), Item("c", 0, 2, 6, 2)];

        var nodes = BoardArranger.Arrange(items, Layouts, Small, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(0, 0, 1, 2), RectOf(nodes, "a"));
        Assert.Equal(new GridRect(0, 2, 1, 3), RectOf(nodes, "b"));
        Assert.Equal(new GridRect(0, 5, 1, 2), RectOf(nodes, "c"));
    }

    [Fact]
    public void Arrange_MediumNeverEdited_FollowsEditedLarge()
    {
        var a = Item("a", 0, 0, 6, 2);
        var b = Item("b", 6, 0, 6, 2);
        a.Placements["large"] = new GridRect(6, 0, 6, 2);
        b.Placements["large"] = new GridRect(0, 0, 6, 2);

        var nodes = BoardArranger.Arrange([a, b], Layouts, Medium, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(3, 0, 3, 2), RectOf(nodes, "a"));
        Assert.Equal(new GridRect(0, 0, 3, 2), RectOf(nodes, "b"));
    }

    [Fact]
    public void Arrange_EditedLayout_KeepsItsPlacementsAndAddsNewItems()
    {
        var a = Item("a", 0, 0, 6, 2);
        a.Placements["medium"] = new GridRect(0, 0, 6, 2);
        var added = Item("new", 0, 0, 6, 2);

        var nodes = BoardArranger.Arrange([a, added], Layouts, Medium, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(0, 0, 6, 2), RectOf(nodes, "a"));
        Assert.Equal(new GridRect(0, 2, 3, 2), RectOf(nodes, "new"));
    }

    [Fact]
    public void Arrange_UnknownLayout_TreatedAsLargest()
    {
        DashboardItem[] items = [Item("a", 2, 0, 4, 2)];
        var custom = new DashboardLayout("wall", 3000, 24);

        var nodes = BoardArranger.Arrange(items, Layouts, custom, GridCompaction.Vertical, Describe);

        Assert.Equal(new GridRect(2, 0, 4, 2), RectOf(nodes, "a"));
    }

    [Theory]
    [InlineData(1400, "large")]
    [InlineData(1100, "large")]
    [InlineData(1099, "medium")]
    [InlineData(640, "medium")]
    [InlineData(320, "small")]
    public void ForWidth_PicksWidestLayoutThatFits(double width, string expected)
    {
        Assert.Equal(expected, DashboardLayout.ForWidth(Layouts, width).Id);
    }

    [Fact]
    public void ForWidth_NarrowerThanEveryLayout_UsesNarrowest()
    {
        DashboardLayout[] layouts = [new("wide", 900, 8), new("narrow", 500, 4)];

        Assert.Equal("narrow", DashboardLayout.ForWidth(layouts, 200).Id);
    }
}
