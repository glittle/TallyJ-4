using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using TallyJ4.DTOs.Results;
using TallyJ4.Services;

namespace TallyJ4.Services;

/// <summary>
/// Service for generating election reports in PDF and Excel formats.
/// </summary>
public class ReportExportService : IReportExportService
{
    private readonly ITallyService _tallyService;
    private readonly ILogger<ReportExportService> _logger;

    /// <summary>
    /// Initializes a new instance of the ReportExportService.
    /// </summary>
    /// <param name="tallyService">The tally service for retrieving election data.</param>
    /// <param name="logger">Logger for recording export operations.</param>
    public ReportExportService(ITallyService tallyService, ILogger<ReportExportService> logger)
    {
        _tallyService = tallyService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a PDF report for the specified election.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="filters">Optional filters to apply to the report.</param>
    /// <returns>A byte array containing the PDF report data.</returns>
    public async Task<byte[]> GeneratePdfReportAsync(Guid electionId, Dictionary<string, string>? filters = null)
    {
        _logger.LogInformation("Generating PDF report for election {ElectionId}", electionId);

        try
        {
            var electionReport = await _tallyService.GetElectionReportAsync(electionId);
            var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);

            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 50, 50, 50, 50);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var title = new Paragraph($"Election Report - {electionReport.ElectionName}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20;
            document.Add(title);

            // Election Overview
            var overviewFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var overviewTitle = new Paragraph("Election Overview", overviewFont);
            overviewTitle.SpacingAfter = 10;
            document.Add(overviewTitle);

            var overviewTable = new PdfPTable(2);
            overviewTable.WidthPercentage = 100;
            overviewTable.SetWidths(new float[] { 1, 2 });

            AddTableRow(overviewTable, "Election Date", detailedStats.Overview.ElectionDate?.ToString("yyyy-MM-dd") ?? "Not specified");
            AddTableRow(overviewTable, "Total Registered Voters", detailedStats.Overview.TotalRegisteredVoters.ToString());
            AddTableRow(overviewTable, "Total Ballots Cast", detailedStats.Overview.TotalBallotsCast.ToString());
            AddTableRow(overviewTable, "Valid Ballots", detailedStats.Overview.ValidBallots.ToString());
            AddTableRow(overviewTable, "Spoiled Ballots", detailedStats.Overview.SpoiledBallots.ToString());
            AddTableRow(overviewTable, "Total Votes", detailedStats.Overview.TotalVotes.ToString());
            AddTableRow(overviewTable, "Positions to Elect", detailedStats.Overview.PositionsToElect.ToString());
            AddTableRow(overviewTable, "Overall Turnout", $"{detailedStats.Overview.OverallTurnoutPercentage:F2}%");

            document.Add(overviewTable);
            document.Add(new Paragraph("\n"));

            // Elected Candidates
            if (electionReport.Elected.Any())
            {
                var electedFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var electedTitle = new Paragraph("Elected Candidates", electedFont);
                electedTitle.SpacingAfter = 10;
                document.Add(electedTitle);

                var electedTable = new PdfPTable(3);
                electedTable.WidthPercentage = 100;
                electedTable.SetWidths(new float[] { 1, 3, 1 });

                // Header
                electedTable.AddCell(new PdfPCell(new Phrase("Rank", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                electedTable.AddCell(new PdfPCell(new Phrase("Name", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                electedTable.AddCell(new PdfPCell(new Phrase("Votes", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                foreach (var candidate in electionReport.Elected.OrderBy(c => c.Rank))
                {
                    electedTable.AddCell(candidate.Rank.ToString());
                    electedTable.AddCell(candidate.FullName);
                    electedTable.AddCell(candidate.VoteCount.ToString());
                }

                document.Add(electedTable);
                document.Add(new Paragraph("\n"));
            }

            // Location Statistics
            if (detailedStats.LocationStatistics.Any())
            {
                var locationFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var locationTitle = new Paragraph("Location Statistics", locationFont);
                locationTitle.SpacingAfter = 10;
                document.Add(locationTitle);

                var locationTable = new PdfPTable(4);
                locationTable.WidthPercentage = 100;
                locationTable.SetWidths(new float[] { 2, 1, 1, 1 });

                // Header
                locationTable.AddCell(new PdfPCell(new Phrase("Location", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                locationTable.AddCell(new PdfPCell(new Phrase("Registered", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                locationTable.AddCell(new PdfPCell(new Phrase("Voted", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });
                locationTable.AddCell(new PdfPCell(new Phrase("Turnout %", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });

                foreach (var location in detailedStats.LocationStatistics.OrderBy(l => l.LocationName))
                {
                    locationTable.AddCell(location.LocationName);
                    locationTable.AddCell(location.RegisteredVoters.ToString());
                    locationTable.AddCell(location.BallotsCast.ToString());
                    locationTable.AddCell($"{location.TurnoutPercentage:F2}%");
                }

                document.Add(locationTable);
            }

            document.Close();
            writer.Close();

            _logger.LogInformation("PDF report generated successfully for election {ElectionId}", electionId);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report for election {ElectionId}", electionId);
            throw;
        }
    }

    /// <summary>
    /// Generates an Excel report for the specified election.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="filters">Optional filters to apply to the report.</param>
    /// <returns>A byte array containing the Excel report data.</returns>
    public async Task<byte[]> GenerateExcelReportAsync(Guid electionId, Dictionary<string, string>? filters = null)
    {
        _logger.LogInformation("Generating Excel report for election {ElectionId}", electionId);

        try
        {
            var electionReport = await _tallyService.GetElectionReportAsync(electionId);
            var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);

            using var workbook = new XLWorkbook();

            // Overview Sheet
            var overviewSheet = workbook.Worksheets.Add("Overview");
            overviewSheet.Cell(1, 1).Value = "Election Report";
            overviewSheet.Cell(1, 1).Style.Font.Bold = true;
            overviewSheet.Cell(1, 1).Style.Font.FontSize = 16;
            overviewSheet.Cell(2, 1).Value = electionReport.ElectionName;
            overviewSheet.Cell(2, 1).Style.Font.Bold = true;

            overviewSheet.Cell(4, 1).Value = "Election Overview";
            overviewSheet.Cell(4, 1).Style.Font.Bold = true;

            var overviewData = new[]
            {
                new { Label = "Election Date", Value = detailedStats.Overview.ElectionDate?.ToString("yyyy-MM-dd") ?? "Not specified" },
                new { Label = "Total Registered Voters", Value = detailedStats.Overview.TotalRegisteredVoters.ToString() },
                new { Label = "Total Ballots Cast", Value = detailedStats.Overview.TotalBallotsCast.ToString() },
                new { Label = "Valid Ballots", Value = detailedStats.Overview.ValidBallots.ToString() },
                new { Label = "Spoiled Ballots", Value = detailedStats.Overview.SpoiledBallots.ToString() },
                new { Label = "Total Votes", Value = detailedStats.Overview.TotalVotes.ToString() },
                new { Label = "Positions to Elect", Value = detailedStats.Overview.PositionsToElect.ToString() },
                new { Label = "Overall Turnout", Value = $"{detailedStats.Overview.OverallTurnoutPercentage:F2}%" }
            };

            for (int i = 0; i < overviewData.Length; i++)
            {
                overviewSheet.Cell(5 + i, 1).Value = overviewData[i].Label;
                overviewSheet.Cell(5 + i, 2).Value = overviewData[i].Value;
            }

            // Elected Candidates Sheet
            if (electionReport.Elected.Any())
            {
                var electedSheet = workbook.Worksheets.Add("Elected Candidates");
                electedSheet.Cell(1, 1).Value = "Elected Candidates";
                electedSheet.Cell(1, 1).Style.Font.Bold = true;

                electedSheet.Cell(3, 1).Value = "Rank";
                electedSheet.Cell(3, 2).Value = "Name";
                electedSheet.Cell(3, 3).Value = "Votes";
                electedSheet.Cell(3, 4).Value = "Section";

                var headerRange = electedSheet.Range("A3:D3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 4;
                foreach (var candidate in electionReport.Elected.OrderBy(c => c.Rank))
                {
                    electedSheet.Cell(row, 1).Value = candidate.Rank;
                    electedSheet.Cell(row, 2).Value = candidate.FullName;
                    electedSheet.Cell(row, 3).Value = candidate.VoteCount;
                    electedSheet.Cell(row, 4).Value = candidate.Section;
                    row++;
                }

                electedSheet.Columns().AdjustToContents();
            }

            // Location Statistics Sheet
            if (detailedStats.LocationStatistics.Any())
            {
                var locationSheet = workbook.Worksheets.Add("Location Statistics");
                locationSheet.Cell(1, 1).Value = "Location Statistics";
                locationSheet.Cell(1, 1).Style.Font.Bold = true;

                locationSheet.Cell(3, 1).Value = "Location";
                locationSheet.Cell(3, 2).Value = "Registered Voters";
                locationSheet.Cell(3, 3).Value = "Ballots Cast";
                locationSheet.Cell(3, 4).Value = "Valid Ballots";
                locationSheet.Cell(3, 5).Value = "Spoiled Ballots";
                locationSheet.Cell(3, 6).Value = "Turnout %";
                locationSheet.Cell(3, 7).Value = "Total Votes";

                var headerRange = locationSheet.Range("A3:G3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 4;
                foreach (var location in detailedStats.LocationStatistics.OrderBy(l => l.LocationName))
                {
                    locationSheet.Cell(row, 1).Value = location.LocationName;
                    locationSheet.Cell(row, 2).Value = location.RegisteredVoters;
                    locationSheet.Cell(row, 3).Value = location.BallotsCast;
                    locationSheet.Cell(row, 4).Value = location.ValidBallots;
                    locationSheet.Cell(row, 5).Value = location.SpoiledBallots;
                    locationSheet.Cell(row, 6).Value = location.TurnoutPercentage;
                    locationSheet.Cell(row, 7).Value = location.TotalVotes;
                    row++;
                }

                locationSheet.Columns().AdjustToContents();
            }

            // Candidate Performance Sheet
            if (detailedStats.CandidatePerformance.Any())
            {
                var candidateSheet = workbook.Worksheets.Add("Candidate Performance");
                candidateSheet.Cell(1, 1).Value = "Candidate Performance";
                candidateSheet.Cell(1, 1).Style.Font.Bold = true;

                candidateSheet.Cell(3, 1).Value = "Name";
                candidateSheet.Cell(3, 2).Value = "Total Votes";
                candidateSheet.Cell(3, 3).Value = "Vote %";
                candidateSheet.Cell(3, 4).Value = "Rank";
                candidateSheet.Cell(3, 5).Value = "Status";

                var headerRange = candidateSheet.Range("A3:E3");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 4;
                foreach (var candidate in detailedStats.CandidatePerformance.OrderBy(c => c.Rank))
                {
                    candidateSheet.Cell(row, 1).Value = candidate.FullName;
                    candidateSheet.Cell(row, 2).Value = candidate.TotalVotes;
                    candidateSheet.Cell(row, 3).Value = candidate.VotePercentage;
                    candidateSheet.Cell(row, 4).Value = candidate.Rank;
                    candidateSheet.Cell(row, 5).Value = candidate.IsElected ? "Elected" : candidate.IsEliminated ? "Eliminated" : "Other";
                    row++;
                }

                candidateSheet.Columns().AdjustToContents();
            }

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);

            _logger.LogInformation("Excel report generated successfully for election {ElectionId}", electionId);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel report for election {ElectionId}", electionId);
            throw;
        }
    }

    private static void AddTableRow(PdfPTable table, string label, string value)
    {
        table.AddCell(new PdfPCell(new Phrase(label, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))));
        table.AddCell(new PdfPCell(new Phrase(value, FontFactory.GetFont(FontFactory.HELVETICA, 10))));
    }
}