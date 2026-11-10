using Microsoft.AspNetCore.Mvc;

namespace GridKrafter.Demo.Mvc.Controllers;

/// <summary>Where the page saves the board after each change.</summary>
[ApiController]
[Route("api/board")]
public sealed class BoardController(BoardStore store) : ControllerBase
{
    private const long MAX_BYTES = 256 * 1024;

    /// <summary>Saves the board from a <c>gk-changed</c> event.</summary>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>No content once saved, or a problem when the body isn't a board.</returns>
    [HttpPut]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MAX_BYTES)]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var parsed = DashboardJson.TryParse(await reader.ReadToEndAsync(cancellationToken));
        if (!parsed.IsSuccess)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "That isn't a board.",
                detail: string.Join(' ', parsed.ValidationErrors.Select(e => e.ErrorMessage)));
        }

        await store.SaveAsync(parsed.Value, cancellationToken);
        return NoContent();
    }
}
