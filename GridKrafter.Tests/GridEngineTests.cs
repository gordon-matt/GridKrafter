using static GridKrafter.Tests.TestData;

namespace GridKrafter.Tests;

public class GridEngineTests
{
    private static readonly GridEngine Compact = new(12);
    private static readonly GridEngine Float = new(12, GridCompaction.None);

    [Fact]
    public void Normalize_Overlap_PushesLaterNodeDown()
    {
        var nodes = Float.Normalize([Node("a", 0, 0, 4, 2), Node("b", 2, 1, 4, 2)]);

        Assert.Equal(new GridRect(0, 0, 4, 2), RectOf(nodes, "a"));
        Assert.Equal(new GridRect(2, 2, 4, 2), RectOf(nodes, "b"));
    }

    [Fact]
    public void Normalize_Vertical_RemovesGapsAbove()
    {
        var nodes = Compact.Normalize([Node("a", 0, 5, 4, 2), Node("b", 0, 9, 4, 2)]);

        Assert.Equal(0, RectOf(nodes, "a").Y);
        Assert.Equal(2, RectOf(nodes, "b").Y);
    }

    [Fact]
    public void Normalize_Float_KeepsGaps()
    {
        var nodes = Float.Normalize([Node("a", 0, 5, 4, 2)]);

        Assert.Equal(5, RectOf(nodes, "a").Y);
    }

    [Fact]
    public void Normalize_OutOfBounds_ClampsToGridAndLimits()
    {
        var nodes = Compact.Normalize([Node("a", 10, 0, 6, 2), Node("b", 0, 2, 1, 1) with { MinW = 3, MinH = 2 }, Node("c", 0, 4, 20, 9) with { MaxH = 4 }]);

        Assert.Equal(new GridRect(6, 0, 6, 2), RectOf(nodes, "a"));
        Assert.Equal(new GridRect(0, 0, 3, 2), RectOf(nodes, "b"));
        Assert.Equal(new GridRect(0, 2, 12, 4), RectOf(nodes, "c"));
    }

    [Fact]
    public void Normalize_KeepsInputOrderAndUnchangedInstances()
    {
        GridNode[] input = [Node("b", 4, 0, 4, 2), Node("a", 0, 0, 4, 2)];

        var nodes = Compact.Normalize(input);

        Assert.Equal(["b", "a"], nodes.Select(n => n.Id));
        Assert.Same(input[0], nodes[0]);
    }

    [Fact]
    public void Normalize_Locked_OthersFlowAround()
    {
        var nodes = Compact.Normalize([Node("lock", 0, 1, 4, 2, locked: true), Node("a", 0, 0, 4, 2)]);

        Assert.Equal(new GridRect(0, 1, 4, 2), RectOf(nodes, "lock"));
        Assert.Equal(3, RectOf(nodes, "a").Y);
    }

    [Fact]
    public void Move_PastHalfOfSameRowNeighbour_Swaps()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 4, 0, 4, 2), Node("c", 8, 0, 4, 2)];

        var moved = Compact.Move(nodes, "a", 3, 0)!;

        Assert.Equal(new GridRect(4, 0, 4, 2), RectOf(moved, "a"));
        Assert.Equal(new GridRect(0, 0, 4, 2), RectOf(moved, "b"));
        Assert.Equal(new GridRect(8, 0, 4, 2), RectOf(moved, "c"));
    }

    [Fact]
    public void Move_DifferentWidthsInRow_SwapsKeepingThemAdjacent()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 4, 0, 8, 2)];

        var moved = Compact.Move(nodes, "a", 6, 0)!;

        Assert.Equal(new GridRect(0, 0, 8, 2), RectOf(moved, "b"));
        Assert.Equal(new GridRect(8, 0, 4, 2), RectOf(moved, "a"));
    }

    [Fact]
    public void Move_SameColumnNeighbour_Swaps()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 0, 2, 4, 3)];

        var moved = Compact.Move(nodes, "a", 0, 3)!;

        Assert.Equal(new GridRect(0, 0, 4, 3), RectOf(moved, "b"));
        Assert.Equal(new GridRect(0, 3, 4, 2), RectOf(moved, "a"));
    }

    [Fact]
    public void Move_OnlyGrazingNeighbour_ReturnsNull()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 4, 0, 4, 2)];

        Assert.Null(Compact.Move(nodes, "a", 1, 0));
        Assert.Null(Compact.Move(nodes, "a", 2, 0));
    }

    [Fact]
    public void Move_DownOntoSeveral_LiftsThemAbove()
    {
        GridNode[] nodes = [Node("a", 0, 0, 6, 2), Node("b", 0, 2, 3, 2), Node("c", 3, 2, 3, 2)];

        var moved = Compact.Move(nodes, "a", 0, 2)!;

        Assert.Equal(0, RectOf(moved, "b").Y);
        Assert.Equal(0, RectOf(moved, "c").Y);
        Assert.Equal(new GridRect(0, 2, 6, 2), RectOf(moved, "a"));
    }

    [Fact]
    public void Move_UpOntoOffsetNeighbour_PushesItDown()
    {
        GridNode[] nodes = [Node("b", 0, 0, 4, 2), Node("a", 2, 4, 4, 2)];

        var moved = Float.Move(nodes, "a", 1, 0)!;

        Assert.Equal(new GridRect(1, 0, 4, 2), RectOf(moved, "a"));
        Assert.Equal(new GridRect(0, 2, 4, 2), RectOf(moved, "b"));
    }

    [Fact]
    public void Move_PushedNodes_CascadeDown()
    {
        GridNode[] nodes = [Node("b", 0, 0, 4, 2), Node("c", 0, 2, 4, 2), Node("a", 4, 0, 3, 3)];

        var moved = Float.Move(nodes, "a", 1, 0)!;

        Assert.Equal(new GridRect(1, 0, 3, 3), RectOf(moved, "a"));
        Assert.Equal(3, RectOf(moved, "b").Y);
        Assert.Equal(5, RectOf(moved, "c").Y);
    }

    [Fact]
    public void Move_Vertical_CompactsAfterwards()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 0, 2, 4, 2)];

        var moved = Compact.Move(nodes, "a", 0, 10)!;

        Assert.Equal(0, RectOf(moved, "b").Y);
        Assert.Equal(2, RectOf(moved, "a").Y);
    }

    [Fact]
    public void Move_Float_StaysWherePut()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2)];

        var moved = Float.Move(nodes, "a", 5, 7)!;

        Assert.Equal(new GridRect(5, 7, 4, 2), RectOf(moved, "a"));
    }

    [Fact]
    public void Move_OntoLocked_DropsBelowIt()
    {
        GridNode[] nodes = [Node("lock", 4, 0, 4, 2, locked: true), Node("a", 0, 0, 4, 2)];

        var moved = Compact.Move(nodes, "a", 4, 0)!;

        Assert.Equal(new GridRect(4, 0, 4, 2), RectOf(moved, "lock"));
        Assert.Equal(new GridRect(4, 2, 4, 2), RectOf(moved, "a"));
    }

    [Fact]
    public void Move_LockedNode_ReturnsNull()
    {
        GridNode[] nodes = [Node("lock", 0, 0, 4, 2, locked: true)];

        Assert.Null(Compact.Move(nodes, "lock", 4, 0));
    }

    [Fact]
    public void Move_SamePlace_ReturnsSameList()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2)];

        Assert.Same(nodes, Compact.Move(nodes, "a", 0, 0));
    }

    [Fact]
    public void Move_PastRightEdge_ClampsInside()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2)];

        var moved = Compact.Move(nodes, "a", 11, 0)!;

        Assert.Equal(8, RectOf(moved, "a").X);
    }

    [Fact]
    public void Move_LeavesInputUntouched()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 4, 0, 4, 2)];

        _ = Compact.Move(nodes, "a", 4, 0);

        Assert.Equal(new GridRect(0, 0, 4, 2), nodes[0].Rect);
        Assert.Equal(new GridRect(4, 0, 4, 2), nodes[1].Rect);
    }

    [Fact]
    public void Nudge_Right_StepsUntilItSwaps()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 4, 0, 4, 2)];

        var moved = Compact.Nudge(nodes, "a", 1, 0)!;

        Assert.Equal(4, RectOf(moved, "a").X);
        Assert.Equal(0, RectOf(moved, "b").X);
    }

    [Fact]
    public void Nudge_AtEdge_ReturnsNull()
    {
        GridNode[] nodes = [Node("a", 8, 0, 4, 2)];

        Assert.Null(Compact.Nudge(nodes, "a", 1, 0));
        Assert.Null(Compact.Nudge(nodes, "a", 0, 1));
    }

    [Fact]
    public void Resize_Taller_PushesNodeBelowDown()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 0, 2, 4, 2)];

        var resized = Compact.Resize(nodes, "a", new GridRect(0, 0, 4, 4))!;

        Assert.Equal(new GridRect(0, 0, 4, 4), RectOf(resized, "a"));
        Assert.Equal(4, RectOf(resized, "b").Y);
    }

    [Fact]
    public void Resize_Shorter_LetsNodeBelowRise()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 4), Node("b", 0, 4, 4, 2)];

        var resized = Compact.Resize(nodes, "a", new GridRect(0, 0, 4, 2))!;

        Assert.Equal(2, RectOf(resized, "b").Y);
    }

    [Fact]
    public void Resize_BeyondLimits_Clamps()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2) with { MaxW = 6, MinH = 2 }];

        var resized = Compact.Resize(nodes, "a", new GridRect(0, 0, 10, 1))!;

        Assert.Equal(new GridRect(0, 0, 6, 2), RectOf(resized, "a"));
    }

    [Fact]
    public void Resize_OverLocked_ReturnsNull()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("lock", 4, 0, 4, 2, locked: true)];

        Assert.Null(Compact.Resize(nodes, "a", new GridRect(0, 0, 6, 2)));
    }

    [Fact]
    public void FindSpace_ReturnsFirstGapInReadingOrder()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 8, 0, 4, 2)];

        Assert.Equal(new GridRect(4, 0, 4, 2), Compact.FindSpace(nodes, 4, 2));
    }

    [Fact]
    public void FindSpace_FullRow_GoesBelow()
    {
        GridNode[] nodes = [Node("a", 0, 0, 6, 2), Node("b", 6, 0, 6, 2)];

        Assert.Equal(new GridRect(0, 2, 4, 3), Compact.FindSpace(nodes, 4, 3));
    }

    [Fact]
    public void Add_AppendsAtFirstFreeSpot()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2)];

        var added = Compact.Add(nodes, Node("new", 0, 0, 8, 2));

        Assert.Equal(["a", "new"], added.Select(n => n.Id));
        Assert.Equal(new GridRect(4, 0, 8, 2), RectOf(added, "new"));
    }

    [Fact]
    public void Insert_AtOccupiedSpot_PushesExistingDown()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2)];

        var inserted = Compact.Insert(nodes, Node("new", 0, 0, 4, 3));

        Assert.Equal(new GridRect(0, 0, 4, 3), RectOf(inserted, "new"));
        Assert.Equal(3, RectOf(inserted, "a").Y);
    }

    [Fact]
    public void Project_ToOneColumn_StacksInReadingOrder()
    {
        GridNode[] nodes = [Node("c", 0, 2, 4, 2), Node("b", 4, 0, 4, 3), Node("a", 0, 0, 4, 2)];

        var projected = new GridEngine(1).Project(nodes, 12);

        Assert.Equal(new GridRect(0, 0, 1, 2), RectOf(projected, "a"));
        Assert.Equal(new GridRect(0, 2, 1, 3), RectOf(projected, "b"));
        Assert.Equal(new GridRect(0, 5, 1, 2), RectOf(projected, "c"));
    }

    [Fact]
    public void Project_HalfTheColumns_ScalesPositionsAndWidths()
    {
        GridNode[] nodes = [Node("a", 0, 0, 4, 2), Node("b", 4, 0, 4, 2), Node("c", 8, 0, 4, 2)];

        var projected = new GridEngine(6).Project(nodes, 12);

        Assert.Equal([0, 2, 4], projected.Select(n => n.Rect.X));
        Assert.All(projected, n => Assert.Equal(2, n.Rect.W));
        Assert.All(projected, n => Assert.Equal(0, n.Rect.Y));
    }

    [Fact]
    public void Project_RowThatNoLongerFits_WrapsInOrderWithoutHoles()
    {
        GridNode[] nodes = [Node("a", 0, 0, 3, 2), Node("b", 3, 0, 3, 2), Node("c", 6, 0, 3, 2), Node("d", 9, 0, 3, 2), Node("wide", 0, 2, 8, 5)];

        var projected = new GridEngine(4).Project(nodes, 12);

        Assert.Equal(new GridRect(0, 0, 1, 2), RectOf(projected, "a"));
        Assert.Equal(new GridRect(1, 0, 1, 2), RectOf(projected, "b"));
        Assert.Equal(new GridRect(2, 0, 1, 2), RectOf(projected, "c"));
        Assert.Equal(new GridRect(3, 0, 1, 2), RectOf(projected, "d"));
        Assert.Equal(new GridRect(0, 2, 3, 5), RectOf(projected, "wide"));

        var halved = new GridEngine(6).Project(nodes, 12);

        Assert.Equal(["a", "b", "c", "d"], halved.Take(4).OrderBy(n => n.Rect.Y).ThenBy(n => n.Rect.X).Select(n => n.Id));
        Assert.Equal(new GridRect(0, 2, 2, 2), RectOf(halved, "d"));
    }

    [Fact]
    public void Project_RespectsMinWidth()
    {
        GridNode[] nodes = [Node("a", 0, 0, 2, 2) with { MinW = 3 }];

        var projected = new GridEngine(6).Project(nodes, 12);

        Assert.Equal(3, RectOf(projected, "a").W);
    }
}
