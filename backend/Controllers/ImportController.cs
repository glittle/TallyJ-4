using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.Application.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly ImportService _importService;

    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("ballots/{electionGuid}")]
    public async Task<IActionResult> ImportBallots(Guid electionGuid, [FromBody] ImportBallotsRequest request)
    {
        if (string.IsNullOrEmpty(request.CsvContent))
        {
            return BadRequest(new { error = "CSV content is required" });
        }

        var result = await _importService.ImportBallotDataAsync(electionGuid, request.CsvContent);

        if (!result.Success)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new
        {
            message = $"Import completed. Created {result.BallotsCreated} ballots with {result.VotesCreated} votes",
            ballotsCreated = result.BallotsCreated,
            votesCreated = result.VotesCreated
        });
    }
}

public class ImportBallotsRequest
{
    public string CsvContent { get; set; } = null!;
}