namespace GridKrafter.Serialization;

/// <summary>Reads and writes boards and layouts as JSON, the format the custom element, storage and hosts share.</summary>
public static class DashboardJson
{
    /// <summary>Writes a board.</summary>
    /// <param name="board">The board.</param>
    /// <param name="indented">Whether to indent it for people to read.</param>
    /// <returns>The JSON.</returns>
    public static string Serialize(DashboardBoard board, bool indented = false) =>
        JsonSerializer.Serialize(board, (indented ? GridKrafterJsonContext.Indented : GridKrafterJsonContext.Default).DashboardBoard);

    /// <summary>Reads a board, tidying ids and sections.</summary>
    /// <param name="json">The JSON.</param>
    /// <returns>The board, or an invalid result describing what's wrong.</returns>
    public static Result<DashboardBoard> TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Result.Invalid(new ValidationError("A board needs some JSON."));
        }

        try
        {
            var board = JsonSerializer.Deserialize(json, GridKrafterJsonContext.Default.DashboardBoard);
            return board is null
                ? Result.Invalid(new ValidationError("The board JSON was null."))
                : Result.Success(board.Tidy());
        }
        catch (JsonException exception)
        {
            return Result.Invalid(new ValidationError(exception.Message));
        }
    }

    /// <summary>Reads a list of layouts, ignoring any without an id or columns.</summary>
    /// <param name="json">The JSON array.</param>
    /// <returns>The layouts, or an invalid result describing what's wrong.</returns>
    public static Result<IReadOnlyList<DashboardLayout>> TryParseLayouts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Result.Invalid(new ValidationError("Layouts need some JSON."));
        }

        try
        {
            var layouts = JsonSerializer.Deserialize(json, GridKrafterJsonContext.Default.ListDashboardLayout) ?? [];
            IReadOnlyList<DashboardLayout> valid = [.. layouts.Where(l => !string.IsNullOrWhiteSpace(l.Id) && l.Columns > 0)];
            return valid.Count > 0
                ? Result.Success(valid)
                : Result.Invalid(new ValidationError("There were no layouts with an id and columns."));
        }
        catch (JsonException exception)
        {
            return Result.Invalid(new ValidationError(exception.Message));
        }
    }

    /// <summary>Writes a list of layouts.</summary>
    /// <param name="layouts">The layouts.</param>
    /// <returns>The JSON array.</returns>
    public static string SerializeLayouts(IEnumerable<DashboardLayout> layouts) =>
        JsonSerializer.Serialize(layouts.ToList(), GridKrafterJsonContext.Default.ListDashboardLayout);

    /// <summary>A deep copy, so a host's board and a dashboard's working copy never share state.</summary>
    /// <param name="board">The board.</param>
    /// <returns>An independent copy.</returns>
    public static DashboardBoard Clone(DashboardBoard board) =>
        JsonSerializer.Deserialize(Serialize(board), GridKrafterJsonContext.Default.DashboardBoard)!;

    /// <summary>The board as a JSON node, for embedding in a larger payload such as a DOM event.</summary>
    /// <param name="board">The board.</param>
    /// <returns>The JSON object.</returns>
    public static JsonNode? ToNode(DashboardBoard board) =>
        JsonSerializer.SerializeToNode(board, GridKrafterJsonContext.Default.DashboardBoard);
}
