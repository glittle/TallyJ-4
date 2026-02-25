using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.Domain.Entities;

namespace Backend.Application.Services;

public class ImportService
{
    private readonly MainDbContext _context;

    public ImportService(MainDbContext context)
    {
        _context = context;
    }

    public async Task<ImportResult> ImportBallotDataAsync(Guid electionGuid, string csvContent)
    {
        var result = new ImportResult();

        try
        {
            // Get the first location for this election
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.ElectionGuid == electionGuid);

            if (location == null)
            {
                result.Errors.Add("No location found for this election. Please create a location first.");
                return result;
            }

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                result.Errors.Add("CSV file must have at least a header row and one data row");
                return result;
            }

            var headers = ParseCsvLine(lines[0]);
            var ballotIndex = Array.IndexOf(headers, "Ballot");
            var votesIndex = Array.IndexOf(headers, "Votes");

            if (ballotIndex == -1 || votesIndex == -1)
            {
                result.Errors.Add("CSV must contain 'Ballot' and 'Votes' columns");
                return result;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                if (values.Length <= Math.Max(ballotIndex, votesIndex))
                {
                    result.Errors.Add($"Row {i + 1}: Insufficient columns");
                    continue;
                }

                var ballotCode = values[ballotIndex]?.Trim();
                var votesText = values[votesIndex]?.Trim();

                if (string.IsNullOrEmpty(ballotCode) || string.IsNullOrEmpty(votesText))
                {
                    result.Errors.Add($"Row {i + 1}: Missing ballot code or votes");
                    continue;
                }

                // Parse votes (assuming format like "Person1,Person2,Person3" for person names)
                var personNames = votesText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();

                // Create ballot
                var ballotGuid = Guid.NewGuid();
                var ballot = new Ballot
                {
                    BallotGuid = ballotGuid,
                    LocationGuid = location.LocationGuid,
                    BallotCode = ballotCode,
                    StatusCode = "Ok",
                    ComputerCode = "IMPORT",
                    BallotNumAtComputer = i
                };

                _context.Ballots.Add(ballot);

                // Create votes
                for (int pos = 0; pos < personNames.Count; pos++)
                {
                    // Try to find person by name (this is simplistic - in real implementation would need better matching)
                    var person = await _context.People
                        .FirstOrDefaultAsync(p => p.ElectionGuid == electionGuid &&
                                                 p.FirstName + " " + p.LastName == personNames[pos]);

                    if (person != null)
                    {
                        var vote = new Vote
                        {
                            BallotGuid = ballotGuid,
                            PersonGuid = person.PersonGuid,
                            PositionOnBallot = pos + 1,
                            StatusCode = "Ok"
                        };
                        _context.Votes.Add(vote);
                        result.VotesCreated++;
                    }
                    else
                    {
                        result.Errors.Add($"Person '{personNames[pos]}' not found for ballot {ballotCode}");
                    }
                }

                result.BallotsCreated++;
            }

            await _context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Ballot Import failed: {ex.Message}");
        }

        return result;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        int i = 0;
        while (i < line.Length)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }

            i++;
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}

public class ImportResult
{
    public bool Success { get; set; }
    public int BallotsCreated { get; set; }
    public int VotesCreated { get; set; }
    public List<string> Errors { get; } = new();
}

