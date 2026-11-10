namespace GridKrafter.Tests;

public class DashboardJsonTests
{
    [Fact]
    public void Serialize_ThenParse_RoundTrips()
    {
        var board = new DashboardBoard
        {
            Sections = [new DashboardSection { Id = "overview", Title = "Overview", Collapsed = true }],
            Items =
            [
                new DashboardItem
                {
                    Id = "sales",
                    Kind = "chart",
                    Section = "overview",
                    Title = "Sales",
                    Color = "blue",
                    Buttons = WidgetButtons.Collapse | WidgetButtons.Fullscreen,
                    Chrome = WidgetChrome.Card,
                    Placements = { ["large"] = new GridRect(0, 0, 8, 4) },
                    Options = new JsonObject { ["period"] = "month", ["stacked"] = true },
                },
            ],
        };

        var parsed = DashboardJson.TryParse(DashboardJson.Serialize(board)).Value;

        var item = Assert.Single(parsed.Items);
        Assert.Equal("chart", item.Kind);
        Assert.Equal(WidgetButtons.Collapse | WidgetButtons.Fullscreen, item.Buttons);
        Assert.Equal(WidgetChrome.Card, item.Chrome);
        Assert.Equal(new GridRect(0, 0, 8, 4), item.Placements["large"]);
        Assert.Equal("month", item.Options!["period"]!.GetValue<string>());
        Assert.True(parsed.Sections[0].Collapsed);
    }

    [Fact]
    public void Serialize_UsesCamelCaseAndReadableEnums()
    {
        var board = new DashboardBoard { Items = [new DashboardItem { Id = "a", Buttons = WidgetButtons.Collapse | WidgetButtons.Fullscreen, Chrome = WidgetChrome.Plain, Placements = { ["large"] = new GridRect(1, 2, 3, 4) } }] };

        string json = DashboardJson.Serialize(board);

        Assert.Contains("\"buttons\":\"fullscreen, collapse\"", json, StringComparison.Ordinal);
        Assert.Contains("\"chrome\":\"plain\"", json, StringComparison.Ordinal);
        Assert.Contains("\"large\":{\"x\":1,\"y\":2,\"w\":3,\"h\":4}", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"right\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"html\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_HostJson_AcceptsShorthandAndComments()
    {
        const string json = """
            {
              // a host's hand-written board
              "items": [
                { "id": "welcome", "template": "#welcome", "x": 0, "y": 0, "w": 6, "h": 3, "buttons": "collapse,fullscreen", },
              ],
            }
            """;

        var board = DashboardJson.TryParse(json).Value;

        var item = Assert.Single(board.Items);
        Assert.Equal("#welcome", item.Template);
        Assert.Equal(6, item.W);
        Assert.Equal(WidgetButtons.Collapse | WidgetButtons.Fullscreen, item.Buttons);
    }

    [Fact]
    public void TryParse_NoSections_AddsDefaultSection()
    {
        var board = DashboardJson.TryParse("""{ "items": [] }""").Value;

        Assert.Equal(Constants.DefaultSection, Assert.Single(board.Sections).Id);
    }

    [Fact]
    public void TryParse_DuplicateIds_KeepsFirst()
    {
        var board = DashboardJson.TryParse("""{ "items": [ { "id": "a", "title": "First" }, { "id": "a", "title": "Second" }, { "id": "" } ] }""").Value;

        Assert.Equal("First", Assert.Single(board.Items).Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{ "items": 5 }""")]
    public void TryParse_Invalid_ReturnsInvalid(string json)
    {
        Assert.False(DashboardJson.TryParse(json).IsSuccess);
    }

    [Fact]
    public void TryParseLayouts_SkipsLayoutsWithoutIdOrColumns()
    {
        var layouts = DashboardJson.TryParseLayouts("""[ { "id": "desk", "minWidth": 900, "columns": 10 }, { "id": "", "columns": 4 }, { "id": "x", "columns": 0 } ]""").Value;

        Assert.Equal(new DashboardLayout("desk", 900, 10), Assert.Single(layouts));
    }

    [Fact]
    public void Clone_IsIndependent()
    {
        var board = new DashboardBoard { Items = [new DashboardItem { Id = "a", Title = "One" }] };

        var copy = DashboardJson.Clone(board);
        copy.Items[0].Title = "Two";

        Assert.Equal("One", board.Items[0].Title);
    }
}
