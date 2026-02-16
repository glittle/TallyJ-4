using Backend.DTOs.Results;

namespace Backend.Services;

/// <summary>
/// Service interface for exporting election reports in different formats.
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// Generates a PDF report for the specified election.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="filters">Optional filters to apply to the report.</param>
    /// <returns>A byte array containing the PDF report data.</returns>
    Task<byte[]> GeneratePdfReportAsync(Guid electionId, Dictionary<string, string>? filters = null);

    /// <summary>
    /// Generates an Excel report for the specified election.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="filters">Optional filters to apply to the report.</param>
    /// <returns>A byte array containing the Excel report data.</returns>
    Task<byte[]> GenerateExcelReportAsync(Guid electionId, Dictionary<string, string>? filters = null);

    /// <summary>
    /// Generates a CSV report for the specified election.
    /// </summary>
    /// <param name="electionId">The unique identifier of the election.</param>
    /// <param name="filters">Optional filters to apply to the report.</param>
    /// <returns>A byte array containing the CSV report data.</returns>
    Task<byte[]> GenerateCsvReportAsync(Guid electionId, Dictionary<string, string>? filters = null);
}


