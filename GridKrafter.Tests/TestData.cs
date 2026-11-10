namespace GridKrafter.Tests;

internal static class TestData
{
    public static GridNode Node(string id, int x, int y, int w, int h, bool locked = false) =>
        new(id, new GridRect(x, y, w, h)) { Locked = locked };

    public static GridRect RectOf(IReadOnlyList<GridNode> nodes, string id) =>
        nodes.Single(n => n.Id == id).Rect;

    public static DashboardItem Item(string id, int? x = null, int? y = null, int w = 4, int h = 2, string? section = null) =>
        new() { Id = id, X = x, Y = y, W = w, H = h, Section = section };

    public static GridNode Describe(DashboardItem item) =>
        new(item.Id, new GridRect(item.X ?? 0, item.Y ?? 0, item.W ?? 4, item.H ?? 2))
        {
            MinW = item.MinW ?? 1,
            MinH = item.MinH ?? 1,
            MaxW = item.MaxW,
            MaxH = item.MaxH,
            Locked = item.Locked,
        };
}
