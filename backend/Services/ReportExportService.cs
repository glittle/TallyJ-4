using System.Globalization;
using Backend.Domain.Enumerations;
using Backend.DTOs.Results;
using ClosedXML.Excel;
using CsvHelper;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
namespace Backend.Services;

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
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);
            document.SetMargins(50, 50, 50, 50);

            // Fonts
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Title
            var title = new Paragraph($"Election Report - {electionReport.ElectionName}")
                .SetFont(boldFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(title);

            // Election Overview
            var overviewTitle = new Paragraph("Election Overview")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetMarginBottom(10);
            document.Add(overviewTitle);

            var overviewTable = new Table(new float[] { 1, 2 });
            overviewTable.SetWidth(UnitValue.CreatePercentValue(100));

            AddTableRow(overviewTable, "Election Date", detailedStats.Overview.ElectionDate?.ToString("yyyy-MM-dd") ?? "Not specified", boldFont, normalFont);
            AddTableRow(overviewTable, "Total Registered Voters", detailedStats.Overview.TotalRegisteredVoters.ToString(), boldFont, normalFont);
            AddTableRow(overviewTable, "Total Ballots Cast", detailedStats.Overview.TotalBallotsCast.ToString(), boldFont, normalFont);
            AddTableRow(overviewTable, "Valid Ballots", detailedStats.Overview.ValidBallots.ToString(), boldFont, normalFont);
            AddTableRow(overviewTable, "Spoiled Ballots", detailedStats.Overview.SpoiledBallots.ToString(), boldFont, normalFont);
            AddTableRow(overviewTable, "Total Votes", detailedStats.Overview.TotalVotes.ToString(), boldFont, normalFont);
            AddTableRow(overviewTable, "Positions to Elect", detailedStats.Overview.PositionsToElect.ToString(), boldFont, normalFont);
            AddTableRow(overviewTable, "Overall Turnout", $"{detailedStats.Overview.OverallTurnoutPercentage:F2}%", boldFont, normalFont);

            document.Add(overviewTable);
            document.Add(new Paragraph("\n"));

            // Elected Candidates
            if (electionReport.Elected.Any())
            {
                var electedTitle = new Paragraph("Elected Candidates")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginBottom(10);
                document.Add(electedTitle);

                var electedTable = new Table(new float[] { 1, 3, 1 });
                electedTable.SetWidth(UnitValue.CreatePercentValue(100));

                // Header
                electedTable.AddHeaderCell(new Cell().Add(new Paragraph("Rank").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                electedTable.AddHeaderCell(new Cell().Add(new Paragraph("Name").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                electedTable.AddHeaderCell(new Cell().Add(new Paragraph("Votes").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                foreach (var candidate in electionReport.Elected.OrderBy(c => c.Rank))
                {
                    electedTable.AddCell(new Cell().Add(new Paragraph(candidate.Rank.ToString()).SetFont(normalFont).SetFontSize(10)));
                    electedTable.AddCell(new Cell().Add(new Paragraph(candidate.FullName).SetFont(normalFont).SetFontSize(10)));
                    electedTable.AddCell(new Cell().Add(new Paragraph(candidate.VoteCount.ToString()).SetFont(normalFont).SetFontSize(10)));
                }

                document.Add(electedTable);
                document.Add(new Paragraph("\n"));
            }

            // Location Statistics
            if (detailedStats.LocationStatistics.Any())
            {
                var locationTitle = new Paragraph("Location Statistics")
                    .SetFont(boldFont)
                    .SetFontSize(14)
                    .SetMarginBottom(10);
                document.Add(locationTitle);

                var locationTable = new Table(new float[] { 2, 1, 1, 1 });
                locationTable.SetWidth(UnitValue.CreatePercentValue(100));

                // Header
                locationTable.AddHeaderCell(new Cell().Add(new Paragraph("Location").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                locationTable.AddHeaderCell(new Cell().Add(new Paragraph("Registered").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                locationTable.AddHeaderCell(new Cell().Add(new Paragraph("Voted").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                locationTable.AddHeaderCell(new Cell().Add(new Paragraph("Turnout %").SetFont(headerFont).SetFontSize(12)).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                foreach (var location in detailedStats.LocationStatistics.OrderBy(l => l.LocationName))
                {
                    locationTable.AddCell(new Cell().Add(new Paragraph(location.LocationName).SetFont(normalFont).SetFontSize(10)));
                    locationTable.AddCell(new Cell().Add(new Paragraph(location.RegisteredVoters.ToString()).SetFont(normalFont).SetFontSize(10)));
                    locationTable.AddCell(new Cell().Add(new Paragraph(location.BallotsCast.ToString()).SetFont(normalFont).SetFontSize(10)));
                    locationTable.AddCell(new Cell().Add(new Paragraph($"{location.TurnoutPercentage:F2}%").SetFont(normalFont).SetFontSize(10)));
                }

                document.Add(locationTable);
            }

            document.Close();

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
                    electedSheet.Cell(row, 4).Value = ResultSectionEnum.ToCodeString(candidate.SectionCode);
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

    /// <summary>
    /// Generates a CSV report for the specified election.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="filters">Optional filters to apply to the report.</param>
    /// <returns>A byte array containing the CSV report data.</returns>
    public async Task<byte[]> GenerateCsvReportAsync(Guid electionId, Dictionary<string, string>? filters = null)
    {
        _logger.LogInformation("Generating CSV report for election {ElectionId}", electionId);

        try
        {
            var electionReport = await _tallyService.GetElectionReportAsync(electionId);
            var detailedStats = await _tallyService.GetDetailedStatisticsAsync(electionId);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Write election overview
            csv.WriteField("Election Report");
            csv.NextRecord();
            csv.WriteField(electionReport.ElectionName);
            csv.NextRecord();
            csv.NextRecord();

            // Overview section
            csv.WriteField("Election Overview");
            csv.NextRecord();
            csv.WriteField("Metric");
            csv.WriteField("Value");
            csv.NextRecord();

            csv.WriteField("Election Date");
            csv.WriteField(detailedStats.Overview.ElectionDate?.ToString("yyyy-MM-dd") ?? "Not specified");
            csv.NextRecord();

            csv.WriteField("Total Registered Voters");
            csv.WriteField(detailedStats.Overview.TotalRegisteredVoters.ToString());
            csv.NextRecord();

            csv.WriteField("Total Ballots Cast");
            csv.WriteField(detailedStats.Overview.TotalBallotsCast.ToString());
            csv.NextRecord();

            csv.WriteField("Valid Ballots");
            csv.WriteField(detailedStats.Overview.ValidBallots.ToString());
            csv.NextRecord();

            csv.WriteField("Spoiled Ballots");
            csv.WriteField(detailedStats.Overview.SpoiledBallots.ToString());
            csv.NextRecord();

            csv.WriteField("Total Votes");
            csv.WriteField(detailedStats.Overview.TotalVotes.ToString());
            csv.NextRecord();

            csv.WriteField("Overall Turnout");
            csv.WriteField($"{detailedStats.Overview.OverallTurnoutPercentage:F2}%");
            csv.NextRecord();

            csv.NextRecord();

            // Elected candidates
            if (electionReport.Elected.Any())
            {
                csv.WriteField("Elected Candidates");
                csv.NextRecord();
                csv.WriteField("Rank");
                csv.WriteField("Name");
                csv.WriteField("Votes");
                csv.WriteField("Section");
                csv.NextRecord();

                foreach (var candidate in electionReport.Elected.OrderBy(c => c.Rank))
                {
                    csv.WriteField(candidate.Rank.ToString());
                    csv.WriteField(candidate.FullName);
                    csv.WriteField(candidate.VoteCount.ToString());
                    csv.WriteField(ResultSectionEnum.ToCodeString(candidate.SectionCode));
                    csv.NextRecord();
                }

                csv.NextRecord();
            }

            // Location statistics
            if (detailedStats.LocationStatistics.Any())
            {
                csv.WriteField("Location Statistics");
                csv.NextRecord();
                csv.WriteField("Location");
                csv.WriteField("Registered Voters");
                csv.WriteField("Ballots Cast");
                csv.WriteField("Valid Ballots");
                csv.WriteField("Spoiled Ballots");
                csv.WriteField("Turnout %");
                csv.WriteField("Total Votes");
                csv.NextRecord();

                foreach (var location in detailedStats.LocationStatistics.OrderBy(l => l.LocationName))
                {
                    csv.WriteField(location.LocationName);
                    csv.WriteField(location.RegisteredVoters.ToString());
                    csv.WriteField(location.BallotsCast.ToString());
                    csv.WriteField(location.ValidBallots.ToString());
                    csv.WriteField(location.SpoiledBallots.ToString());
                    csv.WriteField($"{location.TurnoutPercentage:F2}%");
                    csv.WriteField(location.TotalVotes.ToString());
                    csv.NextRecord();
                }
            }

            writer.Flush();
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CSV report for election {ElectionId}", electionId);
            throw;
        }
    }

    private static void AddTableRow(Table table, string label, string value, PdfFont boldFont, PdfFont normalFont)
    {
        var labelCell = new Cell().Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10));
        var valueCell = new Cell().Add(new Paragraph(value).SetFont(normalFont).SetFontSize(10));
        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }
}


