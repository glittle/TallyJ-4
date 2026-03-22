using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.Reports;
using Backend.Services;

namespace Backend.Controllers;

[Authorize]
[Authorize(Policy = "ElectionAccess")]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("{electionGuid:guid}/available")]
    public async Task<ActionResult<List<ReportListItemDto>>> GetAvailableReports(Guid electionGuid)
    {
        var reports = await _reportService.GetAvailableReportsAsync(electionGuid);
        return Ok(reports);
    }

    [HttpGet("{electionGuid:guid}/Main")]
    public async Task<ActionResult<MainReportDto>> GetMainReport(Guid electionGuid)
    {
        var report = await _reportService.GetMainReportAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VotesByNum")]
    public async Task<ActionResult<VotesByNumDto>> GetVotesByNum(Guid electionGuid)
    {
        var report = await _reportService.GetVotesByNumAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VotesByName")]
    public async Task<ActionResult<VotesByNameDto>> GetVotesByName(Guid electionGuid)
    {
        var report = await _reportService.GetVotesByNameAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/Ballots")]
    public async Task<ActionResult<BallotsReportDto>> GetBallots(Guid electionGuid)
    {
        var report = await _reportService.GetBallotsReportAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/BallotsOnline")]
    public async Task<ActionResult<BallotsReportDto>> GetBallotsOnline(Guid electionGuid)
    {
        var report = await _reportService.GetBallotsReportAsync(electionGuid, "Online");
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/BallotsImported")]
    public async Task<ActionResult<BallotsReportDto>> GetBallotsImported(Guid electionGuid)
    {
        var report = await _reportService.GetBallotsReportAsync(electionGuid, "Imported");
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/BallotsTied")]
    public async Task<ActionResult<BallotsReportDto>> GetBallotsTied(Guid electionGuid)
    {
        var report = await _reportService.GetBallotsReportAsync(electionGuid, "Tied");
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/SpoiledVotes")]
    public async Task<ActionResult<SpoiledVotesReportDto>> GetSpoiledVotes(Guid electionGuid)
    {
        var report = await _reportService.GetSpoiledVotesAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/BallotAlignment")]
    public async Task<ActionResult<BallotAlignmentReportDto>> GetBallotAlignment(Guid electionGuid)
    {
        var report = await _reportService.GetBallotAlignmentAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/BallotsSame")]
    public async Task<ActionResult<BallotsSameReportDto>> GetBallotsSame(Guid electionGuid)
    {
        var report = await _reportService.GetBallotsSameAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/BallotsSummary")]
    public async Task<ActionResult<BallotsSummaryReportDto>> GetBallotsSummary(Guid electionGuid)
    {
        var report = await _reportService.GetBallotsSummaryAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/AllCanReceive")]
    public async Task<ActionResult<AllCanReceiveReportDto>> GetAllCanReceive(Guid electionGuid)
    {
        var report = await _reportService.GetAllCanReceiveAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/Voters")]
    public async Task<ActionResult<VotersReportDto>> GetVoters(Guid electionGuid)
    {
        var report = await _reportService.GetVotersAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/Flags")]
    public async Task<ActionResult<FlagsReportDto>> GetFlags(Guid electionGuid)
    {
        var report = await _reportService.GetFlagsReportAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VotersOnline")]
    public async Task<ActionResult<VotersOnlineReportDto>> GetVotersOnline(Guid electionGuid)
    {
        var report = await _reportService.GetVotersOnlineAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VotersByArea")]
    public async Task<ActionResult<VotersByAreaReportDto>> GetVotersByArea(Guid electionGuid)
    {
        var report = await _reportService.GetVotersByAreaAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VotersByLocation")]
    public async Task<ActionResult<VotersByLocationReportDto>> GetVotersByLocation(Guid electionGuid)
    {
        var report = await _reportService.GetVotersByLocationAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VotersByLocationArea")]
    public async Task<ActionResult<VotersByLocationAreaReportDto>> GetVotersByLocationArea(Guid electionGuid)
    {
        var report = await _reportService.GetVotersByLocationAreaAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/ChangedPeople")]
    public async Task<ActionResult<ChangedPeopleReportDto>> GetChangedPeople(Guid electionGuid)
    {
        var report = await _reportService.GetChangedPeopleAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/AllNonEligible")]
    public async Task<ActionResult<AllNonEligibleReportDto>> GetAllNonEligible(Guid electionGuid)
    {
        var report = await _reportService.GetAllNonEligibleAsync(electionGuid);
        return Ok(report);
    }

    [HttpGet("{electionGuid:guid}/VoterEmails")]
    public async Task<ActionResult<VoterEmailsReportDto>> GetVoterEmails(Guid electionGuid)
    {
        var report = await _reportService.GetVoterEmailsAsync(electionGuid);
        return Ok(report);
    }
}
