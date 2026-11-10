namespace GridKrafter.Demo.Mvc.Services;

/// <summary>
/// Keeps the board people arrange in a JSON file in App_Data, so it follows them from browser to
/// browser. A real app would key it by user and keep it in its database.
/// </summary>
public sealed class BoardStore(IWebHostEnvironment environment, ILogger<BoardStore> logger)
{
    /// <summary>The request header the page sends its antiforgery token in.</summary>
    public const string TOKEN_HEADER = "RequestVerificationToken";

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string path = Path.Combine(environment.ContentRootPath, "App_Data", "board.json");

    /// <summary>The saved board, or <see langword="null"/> when nothing has been saved.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The board JSON.</returns>
    public async Task<string?> LoadAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : null;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Saves a board. The browser sends the whole board back, and whatever it says a widget's
    /// content is would be trusted as markup on the next visit, so content is dropped here:
    /// the page's own board and the catalog supply it again by id and kind.
    /// </summary>
    /// <param name="board">The board.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the file is written.</returns>
    public async Task SaveAsync(DashboardBoard board, CancellationToken cancellationToken)
    {
        foreach (var item in board.Items)
        {
            item.Html = null;
            item.Url = null;
            item.Template = null;
        }

        string json = DashboardJson.Serialize(board, indented: true);
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".tmp";
            await File.WriteAllTextAsync(temporary, json, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            gate.Release();
        }

        logger.LogInformation("Saved a board with {Count} widgets.", board.Items.Count);
    }

    /// <summary>Forgets the saved board, so the page's own board shows again.</summary>
    /// <param name="cancellationToken">Cancels the wait for other writes.</param>
    /// <returns>A task that completes once the file is gone.</returns>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            File.Delete(path);
        }
        finally
        {
            gate.Release();
        }
    }
}
