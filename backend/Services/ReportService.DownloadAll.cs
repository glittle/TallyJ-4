using System.IO.Compression;
using System.Text;
using Backend.DTOs.Reports;

namespace Backend.Services;

public partial class ReportService
{
    public async Task<(byte[] Content, string FileName)> GetAllReportsZipAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var available = await GetAvailableReportsAsync(electionGuid);

        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var item in available)
            {
                var payload = await GetReportByCodeAsync(electionGuid, item.Code);
                var entry = zip.CreateEntry($"{item.Code}.csv", CompressionLevel.Fastest);
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                ReportCsvFormatter.Write(writer, item.Code, item.Name, payload);
            }
        }

        var safeName = SanitizeFileName(election.Name);
        return (memory.ToArray(), $"{safeName}-reports.zip");
    }

    internal async Task<object> GetReportByCodeAsync(Guid electionGuid, string code)
    {
        return code switch
        {
            "Main" => await GetMainReportAsync(electionGuid),
            "VotesByNum" => await GetVotesByNumAsync(electionGuid),
            "VotesByName" => await GetVotesByNameAsync(electionGuid),
            "Ballots" => await GetBallotsReportAsync(electionGuid),
            "BallotsOnline" => await GetBallotsReportAsync(electionGuid, "Online"),
            "BallotsImported" => await GetBallotsReportAsync(electionGuid, "Imported"),
            "BallotsTied" => await GetBallotsReportAsync(electionGuid, "Tied"),
            "SpoiledVotes" => await GetSpoiledVotesAsync(electionGuid),
            "BallotAlignment" => await GetBallotAlignmentAsync(electionGuid),
            "BallotsSame" => await GetBallotsSameAsync(electionGuid),
            "BallotsSummary" => await GetBallotsSummaryAsync(electionGuid),
            "AllCanReceive" => await GetAllCanReceiveAsync(electionGuid),
            "Voters" => await GetVotersAsync(electionGuid),
            "Flags" => await GetFlagsReportAsync(electionGuid),
            "VotersOnline" => await GetVotersOnlineAsync(electionGuid),
            "VotersByArea" => await GetVotersByAreaAsync(electionGuid),
            "VotersByLocation" => await GetVotersByLocationAsync(electionGuid),
            "VotersByLocationArea" => await GetVotersByLocationAreaAsync(electionGuid),
            "ChangedPeople" => await GetChangedPeopleAsync(electionGuid),
            "AllNonEligible" => await GetAllNonEligibleAsync(electionGuid),
            "VoterEmails" => await GetVoterEmailsAsync(electionGuid),
            _ => throw new ArgumentException($"Unknown report code: {code}")
        };
    }

    private static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "election";
        }

        var chars = name.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c).ToArray();
        var cleaned = new string(chars).Trim('-');
        return string.IsNullOrEmpty(cleaned) ? "election" : cleaned;
    }
}

/// <summary>
/// CSV for the existing teller report family — not a new report type.
/// </summary>
internal static class ReportCsvFormatter
{
    public static void Write(TextWriter writer, string code, string title, object payload)
    {
        WriteRow(writer, "Report", title);
        WriteRow(writer, "Code", code);
        writer.WriteLine();

        switch (payload)
        {
            case MainReportDto main:
                WriteMain(writer, main);
                break;
            case VotesByNumDto votesByNum:
                WriteVotePeople(writer, votesByNum.ElectionName, votesByNum.People);
                break;
            case VotesByNameDto votesByName:
                WriteVotePeople(writer, votesByName.ElectionName, votesByName.People);
                break;
            case BallotsReportDto ballots:
                WriteBallots(writer, ballots);
                break;
            case SpoiledVotesReportDto spoiled:
                WriteRow(writer, "Election", spoiled.ElectionName);
                WriteRow(writer, "Name", "Votes", "Reason");
                foreach (var p in spoiled.People)
                {
                    WriteRow(writer, p.PersonName, p.VoteCount.ToString(), p.InvalidReasonDesc);
                }
                break;
            case BallotAlignmentReportDto alignment:
                WriteRow(writer, "Election", alignment.ElectionName);
                WriteRow(writer, "Matching names", "Ballot count");
                foreach (var row in alignment.Rows)
                {
                    WriteRow(writer, row.MatchingNames.ToString(), row.BallotCount.ToString());
                }
                break;
            case BallotsSameReportDto same:
                WriteRow(writer, "Election", same.ElectionName);
                foreach (var group in same.Groups)
                {
                    WriteRow(writer, $"Group {group.GroupNumber}");
                    WriteBallotItems(writer, group.Ballots);
                }
                break;
            case BallotsSummaryReportDto summary:
                WriteRow(writer, "Election", summary.ElectionName);
                WriteRow(writer, "Ballot", "Location", "Status", "Spoiled votes", "Teller 1", "Teller 2");
                foreach (var b in summary.Ballots)
                {
                    WriteRow(writer, b.BallotCode, b.Location, b.StatusCode, b.SpoiledVotes.ToString(), b.Teller1 ?? "", b.Teller2 ?? "");
                }
                break;
            case AllCanReceiveReportDto canReceive:
                WriteRow(writer, "Election", canReceive.ElectionName);
                WriteRow(writer, "Name");
                foreach (var name in canReceive.People)
                {
                    WriteRow(writer, name);
                }
                break;
            case VotersReportDto voters:
                WriteRow(writer, "Election", voters.ElectionName);
                WriteRow(writer, "Name", "Method", "Bahá'í ID", "Location");
                foreach (var p in voters.People)
                {
                    WriteRow(writer, p.PersonName, p.VotingMethod, p.BahaiId ?? "", p.Location ?? "");
                }
                break;
            case FlagsReportDto flags:
                WriteRow(writer, "Election", flags.ElectionName);
                WriteRow(writer, ["Name", "Location", .. flags.FlagNames]);
                foreach (var p in flags.People)
                {
                    var cells = new List<string> { p.PersonName, p.Location ?? "" };
                    cells.AddRange(flags.FlagNames.Select(f => p.Flags.Contains(f) ? "Y" : ""));
                    WriteRow(writer, cells.ToArray());
                }
                break;
            case VotersOnlineReportDto online:
                WriteRow(writer, "Election", online.ElectionName);
                WriteRow(writer, "Name", "Method", "Status", "Email", "Phone");
                foreach (var p in online.People)
                {
                    WriteRow(writer, p.FullName, p.VotingMethodDisplay, p.Status ?? "", p.Email ?? "", p.Phone ?? "");
                }
                break;
            case VotersByAreaReportDto byArea:
                WriteVotersByArea(writer, byArea);
                break;
            case VotersByLocationReportDto byLocation:
                WriteRow(writer, "Election", byLocation.ElectionName);
                WriteRow(writer, "Location", "Voters", "In Person", "Mailed In", "Dropped Off", "Called In", "Online", "Kiosk", "Imported");
                foreach (var row in byLocation.Locations.Concat([byLocation.Total]))
                {
                    WriteRow(writer, row.LocationName, row.TotalVoters.ToString(), row.InPerson.ToString(),
                        row.MailedIn.ToString(), row.DroppedOff.ToString(), row.CalledIn.ToString(),
                        row.Online.ToString(), row.OnlineKiosk.ToString(), row.Imported.ToString());
                }
                break;
            case VotersByLocationAreaReportDto byLocArea:
                WriteRow(writer, "Election", byLocArea.ElectionName);
                WriteRow(writer, "Location", "Area", "Count");
                foreach (var loc in byLocArea.Locations)
                {
                    foreach (var area in loc.Areas)
                    {
                        WriteRow(writer, loc.LocationName, area.AreaName, area.Count.ToString());
                    }
                }
                break;
            case ChangedPeopleReportDto changed:
                WriteRow(writer, "Election", changed.ElectionName);
                WriteRow(writer, "Change", "First", "Last", "Bahá'í ID", "Can vote", "Can receive");
                foreach (var p in changed.People)
                {
                    WriteRow(writer, p.Change, p.FirstName ?? "", p.LastName ?? "", p.BahaiId ?? "",
                        p.CanVote.ToString(), p.CanReceiveVotes.ToString());
                }
                break;
            case AllNonEligibleReportDto nonEligible:
                WriteRow(writer, "Election", nonEligible.ElectionName);
                WriteRow(writer, "Name", "Can vote", "Can receive", "Reason", "Method");
                foreach (var p in nonEligible.People)
                {
                    WriteRow(writer, p.PersonName, p.CanVote.ToString(), p.CanReceiveVotes.ToString(),
                        p.InvalidReasonDesc ?? "", p.VotingMethod ?? "");
                }
                break;
            case VoterEmailsReportDto emails:
                WriteRow(writer, "Election", emails.ElectionName);
                WriteRow(writer, "Name", "Bahá'í ID", "Email", "Phone", "Can vote", "Method");
                foreach (var p in emails.People)
                {
                    WriteRow(writer, p.FullName, p.BahaiId ?? "", p.Email ?? "", p.Phone ?? "",
                        p.CanVote.ToString(), p.VotingMethod ?? "");
                }
                break;
            default:
                WriteRow(writer, "Unsupported payload", payload.GetType().Name);
                break;
        }
    }

    private static void WriteMain(TextWriter writer, MainReportDto main)
    {
        WriteRow(writer, "Election", main.ElectionName);
        WriteRow(writer, "Convenor", main.Convenor ?? "");
        WriteRow(writer, "Eligible to vote (18+)", main.NumEligibleToVote.ToString());
        WriteRow(writer, "Envelopes collected", main.SumOfEnvelopesCollected.ToString());
        WriteRow(writer, "Ballots", main.NumBallotsWithManual.ToString());
        WriteRow(writer, "In person", main.InPersonBallots.ToString());
        WriteRow(writer, "Mailed in", main.MailedInBallots.ToString());
        WriteRow(writer, "Dropped off", main.DroppedOffBallots.ToString());
        WriteRow(writer, "Called in", main.CalledInBallots.ToString());
        WriteRow(writer, "Online", main.OnlineBallots.ToString());
        WriteRow(writer, "Imported", main.ImportedBallots.ToString());
        writer.WriteLine();
        WriteRow(writer, "Rank", "Name", "Votes", "Section");
        foreach (var person in main.Elected)
        {
            WriteRow(writer, person.Rank, person.Name, person.VoteCountDisplay, person.Section);
        }
    }

    private static void WriteVotePeople(TextWriter writer, string electionName, List<VotePersonDto> people)
    {
        WriteRow(writer, "Election", electionName);
        WriteRow(writer, "Name", "Votes", "Tie-break", "Section");
        foreach (var p in people)
        {
            WriteRow(writer, p.PersonName, p.VoteCount.ToString(), p.TieBreakCount?.ToString() ?? "", p.Section);
        }
    }

    private static void WriteBallots(TextWriter writer, BallotsReportDto ballots)
    {
        WriteRow(writer, "Election", ballots.ElectionName);
        WriteBallotItems(writer, ballots.Ballots);
    }

    private static void WriteBallotItems(TextWriter writer, List<BallotReportItemDto> ballots)
    {
        WriteRow(writer, "Ballot", "Location", "Status", "Votes");
        foreach (var b in ballots)
        {
            var votes = string.Join(" | ", b.Votes.Select(v => v.PersonName));
            WriteRow(writer, b.BallotCode, b.Location, b.StatusCode, votes);
        }
    }

    private static void WriteVotersByArea(TextWriter writer, VotersByAreaReportDto report)
    {
        WriteRow(writer, "Election", report.ElectionName);
        var headers = new List<string>
        {
            "Area", "18+", "18-21", "Voted", "In Person", "Mailed In", "Dropped Off", "Called In",
            "Online", "Kiosk"
        };
        if (report.ShowImported)
        {
            headers.Add("Imported");
        }

        WriteRow(writer, headers.ToArray());
        foreach (var row in report.Areas.Concat([report.Total]))
        {
            var cells = new List<string>
            {
                row.AreaName,
                row.Eligible18Plus.ToString(),
                row.Eligible18To21.ToString(),
                row.Voted.ToString(),
                row.InPerson.ToString(),
                row.MailedIn.ToString(),
                row.DroppedOff.ToString(),
                row.CalledIn.ToString(),
                row.Online.ToString(),
                row.OnlineKiosk.ToString()
            };
            if (report.ShowImported)
            {
                cells.Add(row.Imported.ToString());
            }

            WriteRow(writer, cells.ToArray());
        }
    }

    private static void WriteRow(TextWriter writer, params string[] cells)
    {
        writer.WriteLine(string.Join(',', cells.Select(CsvEscape)));
    }

    private static string CsvEscape(string? value)
    {
        var text = value ?? "";
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        return text;
    }
}
