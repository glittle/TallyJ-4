using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.Import;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for importing ballot data from CSV files.
/// </summary>
public class ImportService
{
    private readonly MainDbContext _context;
    private readonly IHubContext<BallotImportHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="hubContext">The SignalR hub context for ballot import notifications.</param>
    public ImportService(MainDbContext context, IHubContext<BallotImportHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Parses CSV headers and provides a preview of the data.
    /// </summary>
    /// <param name="csvContent">The CSV content to parse.</param>
    /// <param name="delimiter">The delimiter used in the CSV file (default: comma).</param>
    /// <returns>A task that represents the asynchronous operation, containing the parsed headers and preview rows.</returns>
    public async Task<ParseCsvHeadersResponseDto> ParseCsvHeadersAsync(string csvContent, string delimiter = ",")
    {
        var response = new ParseCsvHeadersResponseDto();

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim('\r', '\n'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (lines.Length == 0)
        {
            return response;
        }

        response.Headers = ParseCsvLine(lines[0], delimiter).ToList();
        response.TotalRows = lines.Length - 1;

        int previewCount = Math.Min(5, lines.Length - 1);
        for (int i = 1; i <= previewCount; i++)
        {
            response.PreviewRows.Add(ParseCsvLine(lines[i], delimiter));
        }

        return response;
    }

    /// <summary>
    /// Imports ballot data from a CSV file.
    /// </summary>
    /// <param name="request">The import request containing CSV content and configuration.</param>
    /// <returns>A task that represents the asynchronous operation, containing the import result.</returns>
    public async Task<ImportResultDto> ImportBallotDataAsync(ImportBallotRequestDto request)
    {
        var result = new ImportResultDto();
        var groupName = GetGroupName(request.ElectionGuid);

        try
        {
            var location = request.LocationGuid.HasValue
                ? await _context.Locations.FirstOrDefaultAsync(l => l.LocationGuid == request.LocationGuid.Value)
                : await _context.Locations.FirstOrDefaultAsync(l => l.ElectionGuid == request.ElectionGuid);

            if (location == null)
            {
                result.Errors.Add("No location found for this election. Please create a location first.");
                return result;
            }

            var lines = request.CsvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim('\r', '\n'))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length < request.Configuration.FirstDataRow)
            {
                result.Errors.Add($"CSV file must have at least {request.Configuration.FirstDataRow} rows");
                return result;
            }

            result.TotalRows = lines.Length - request.Configuration.FirstDataRow + 1;

            var ballotCodeMapping = request.Configuration.FieldMappings
                .FirstOrDefault(m => m.TargetField == "BallotCode");
            var votesMapping = request.Configuration.FieldMappings
                .FirstOrDefault(m => m.TargetField == "Votes");
            var teller1Mapping = request.Configuration.FieldMappings
                .FirstOrDefault(m => m.TargetField == "Teller1");
            var teller2Mapping = request.Configuration.FieldMappings
                .FirstOrDefault(m => m.TargetField == "Teller2");

            if (ballotCodeMapping == null || votesMapping == null)
            {
                result.Errors.Add("Field mappings must include BallotCode and Votes");
                return result;
            }

            string[] headers = Array.Empty<string>();
            if (request.Configuration.HasHeaderRow)
            {
                headers = ParseCsvLine(lines[0], request.Configuration.Delimiter);
            }

            var ballotCodeIndex = GetColumnIndex(headers, ballotCodeMapping.SourceColumn);
            var votesIndex = GetColumnIndex(headers, votesMapping.SourceColumn);
            var teller1Index = teller1Mapping != null ? GetColumnIndex(headers, teller1Mapping.SourceColumn) : -1;
            var teller2Index = teller2Mapping != null ? GetColumnIndex(headers, teller2Mapping.SourceColumn) : -1;

            var peopleCache = await _context.People
                .Where(p => p.ElectionGuid == request.ElectionGuid)
                .ToDictionaryAsync(p => $"{p.FirstName} {p.LastName}".ToLower());

            int ballotCounter = await _context.Ballots
                .Where(b => b.LocationGuid == location.LocationGuid)
                .CountAsync() + 1;

            for (int i = request.Configuration.FirstDataRow - 1; i < lines.Length; i++)
            {
                int rowNumber = i + 1;
                var values = ParseCsvLine(lines[i], request.Configuration.Delimiter);

                await ReportProgress(groupName, i - request.Configuration.FirstDataRow + 2, result.TotalRows,
                    $"Processing row {rowNumber}");

                if (!ValidateRow(values, ballotCodeIndex, votesIndex, rowNumber, result, request.Configuration.SkipInvalidRows))
                {
                    result.SkippedRows++;
                    continue;
                }

                var ballotCode = values[ballotCodeIndex]?.Trim();
                var votesText = values[votesIndex]?.Trim();
                var teller1 = teller1Index >= 0 && teller1Index < values.Length ? values[teller1Index]?.Trim() : null;
                var teller2 = teller2Index >= 0 && teller2Index < values.Length ? values[teller2Index]?.Trim() : null;

                if (string.IsNullOrEmpty(ballotCode) || string.IsNullOrEmpty(votesText))
                {
                    HandleRowError($"Row {rowNumber}: Missing ballot code or votes", result,
                        request.Configuration.SkipInvalidRows, rowNumber);
                    if (request.Configuration.SkipInvalidRows)
                    {
                        result.SkippedRows++;
                        continue;
                    }
                    return result;
                }

                var personNames = votesText.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();

                var ballotGuid = Guid.NewGuid();
                var now = DateTimeOffset.UtcNow;
                var ballot = new Ballot
                {
                    BallotGuid = ballotGuid,
                    LocationGuid = location.LocationGuid,
                    BallotCode = ballotCode,
                    StatusCode = BallotStatus.Ok,
                    ComputerCode = "IMPORT",
                    BallotNumAtComputer = ballotCounter++,
                    Teller1 = teller1,
                    Teller2 = teller2,
                    DateCreated = now,
                    DateUpdated = now
                };

                _context.Ballots.Add(ballot);

                for (int pos = 0; pos < personNames.Count; pos++)
                {
                    var personKey = personNames[pos].ToLower();
                    if (peopleCache.TryGetValue(personKey, out var person))
                    {
                        var vote = new Vote
                        {
                            BallotGuid = ballotGuid,
                            PersonGuid = person.PersonGuid,
                            PositionOnBallot = pos + 1,
                            VoteStatus = VoteStatus.Ok
                        };
                        _context.Votes.Add(vote);
                        result.VotesCreated++;
                    }
                    else
                    {
                        result.Warnings.Add($"Row {rowNumber}: Person '{personNames[pos]}' not found for ballot {ballotCode}");
                    }
                }

                result.BallotsCreated++;

                if (result.BallotsCreated % 100 == 0)
                {
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            await _hubContext.Clients.Group(groupName).SendAsync("importComplete", new
            {
                ballotsCreated = result.BallotsCreated,
                votesCreated = result.VotesCreated,
                skippedRows = result.SkippedRows,
                errors = result.Errors,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Ballot Import failed: {ex.Message}");
            await _hubContext.Clients.Group(groupName).SendAsync("importError", ex.Message, 0);
        }

        return result;
    }

    private bool ValidateRow(string[] values, int ballotCodeIndex, int votesIndex, int rowNumber,
        ImportResultDto result, bool skipInvalidRows)
    {
        if (values.Length <= Math.Max(ballotCodeIndex, votesIndex))
        {
            HandleRowError($"Row {rowNumber}: Insufficient columns", result, skipInvalidRows, rowNumber);
            return false;
        }
        return true;
    }

    private void HandleRowError(string error, ImportResultDto result, bool skipInvalidRows, int rowNumber)
    {
        if (skipInvalidRows)
        {
            result.Warnings.Add(error);
        }
        else
        {
            result.Errors.Add(error);
        }
        _ = _hubContext.Clients.Group(GetGroupName(Guid.Empty)).SendAsync("importError", error, rowNumber);
    }

    private async Task ReportProgress(string groupName, int processed, int total, string status)
    {
        await _hubContext.Clients.Group(groupName).SendAsync("importProgress", processed, total, status);
    }

    private int GetColumnIndex(string[] headers, string columnName)
    {
        if (headers.Length == 0)
        {
            if (int.TryParse(columnName, out int index))
            {
                return index;
            }
            return -1;
        }

        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private string[] ParseCsvLine(string line, string delimiter = ",")
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (line.Substring(i, Math.Min(delimiter.Length, line.Length - i)) == delimiter && !inQuotes)
            {
                result.Add(current);
                current = "";
                i += delimiter.Length - 1;
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result.ToArray();
    }

    private static string GetGroupName(Guid electionGuid) => $"BallotImport{electionGuid}";
}



