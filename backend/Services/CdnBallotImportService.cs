using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Domain.Context;
using Backend.DTOs.Import;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml;

namespace Backend.Services;

public class CdnBallotImportService : ElectionImportExportBase
{
    public CdnBallotImportService(MainDbContext context, IElectionService electionService)
        : base(context, electionService)
    {
    }

    private static (List<CdnVoter> voters, List<string> errors) ParseVotersFromXml(XmlElement electionNode)
    {
        var voters = new List<CdnVoter>();
        var errors = new List<string>();

        foreach (XmlElement voterNode in electionNode.SelectNodes("descendant::voter")!)
        {
            var bahaiid = voterNode.GetAttribute("bahaiid");
            var firstname = voterNode.GetAttribute("firstname");
            var lastname = voterNode.GetAttribute("lastname");

            if (string.IsNullOrEmpty(bahaiid))
            {
                errors.Add("Voter is missing required bahaiid attribute");
                continue;
            }
            if (string.IsNullOrEmpty(firstname))
            {
                errors.Add("Voter is missing required firstname attribute");
                continue;
            }
            if (string.IsNullOrEmpty(lastname))
            {
                errors.Add("Voter is missing required lastname attribute");
                continue;
            }

            var voter = new CdnVoter();
            voter.bahaiid = bahaiid;
            voter.firstname = firstname;
            voter.lastname = lastname;
            voters.Add(voter);
        }

        return (voters, errors);
    }

    private static (List<CdnBallot> ballots, List<string> errors) ParseBallotsFromXml(XmlElement electionNode)
    {
        var ballots = new List<CdnBallot>();
        var errors = new List<string>();

        foreach (XmlElement ballotNode in electionNode.SelectNodes("descendant::ballot")!)
        {
            var indexAttr = ballotNode.GetAttribute("index");
            var guidAttr = ballotNode.GetAttribute("guid");

            if (string.IsNullOrEmpty(indexAttr))
            {
                errors.Add("Ballot is missing required index attribute");
                continue;
            }
            if (string.IsNullOrEmpty(guidAttr))
            {
                errors.Add("Ballot is missing required guid attribute");
                continue;
            }

            if (!int.TryParse(indexAttr, out var index))
            {
                errors.Add($"Ballot has invalid index attribute: {indexAttr}");
                continue;
            }

            var ballot = new CdnBallot();
            ballot.index = index;
            ballot.guid = guidAttr;

            foreach (XmlElement voteNode in ballotNode.SelectNodes("vote")!)
            {
                var rawVote = new OnlineRawVote(voteNode.InnerText);
                ballot.Votes.Add(rawVote);
            }
            ballots.Add(ballot);
        }

        return (ballots, errors);
    }

    private static string? ValidateVoterBallotCounts(List<CdnVoter> voters, List<CdnBallot> ballots)
    {
        if (voters.Count != ballots.Count)
        {
            return $"Voter count ({voters.Count}) must match ballot count ({ballots.Count})";
        }
        return null;
    }

    private async Task<Location> EnsureImportedLocationExistsAsync(Guid electionGuid)
    {
        var importedLocation = await _context.Locations
            .FirstOrDefaultAsync(l => l.ElectionGuid == electionGuid && l.LocationTypeEnum == LocationType.Imported);

        if (importedLocation == null)
        {
            importedLocation = new Location
            {
                LocationGuid = Guid.NewGuid(),
                ElectionGuid = electionGuid,
                Name = "Imported",
                LocationTypeCode = LocationType.Imported.ToString()
            };
            _context.Locations.Add(importedLocation);
        }

        return importedLocation;
    }

    private async Task UpdateElectionVotingMethodsAsync(Guid electionGuid)
    {
        var election = await _context.Elections.FindAsync(electionGuid);
        if (election != null && (election.VotingMethods?.Contains("I") != true))
        {
            election.VotingMethods = (election.VotingMethods ?? "") + "I";
            _context.Elections.Update(election);
        }
    }

    private async Task<(Dictionary<string, Person> peopleCache, Dictionary<string, Person> peopleByName, int ballotCounter, List<string> missingBahaiIds)> PrepareVoterProcessingAsync(
        Guid electionGuid, Guid importedLocationGuid, List<CdnVoter> voters)
    {
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.BahaiId != null)
            .ToListAsync();

        var peopleCache = people.ToDictionary(p => p.BahaiId!);
        var peopleByName = people
            .Where(p => p.FirstName != null && p.LastName != null)
            .ToDictionary(p => $"{p.FirstName!.ToLower()}{p.LastName!.ToLower()}");

        var ballotCounter = await _context.Ballots
            .Where(b => b.LocationGuid == importedLocationGuid)
            .CountAsync() + 1;

        var voterBahaiIds = voters.Select(v => v.bahaiid).ToList();
        var missingBahaiIds = voterBahaiIds.Where(id => !peopleCache.ContainsKey(id)).ToList();

        return (peopleCache, peopleByName, ballotCounter, missingBahaiIds);
    }

    private void ProcessVoters(List<CdnVoter> voters, Dictionary<string, Person> peopleCache,
        Guid importedLocationGuid, ImportResultDto result)
    {
        var validVoters = voters.Where(v => peopleCache.ContainsKey(v.bahaiid)).ToList();
        foreach (var voter in validVoters)
        {
            var person = peopleCache[voter.bahaiid];

            if (!string.IsNullOrEmpty(person.VotingMethod) && person.VotingMethod != "I")
            {
                result.Warnings.Add($"{person.FullNameFl} has already voted with method {person.VotingMethod}");
                continue;
            }

            person.VotingMethod = "I";
            person.VotingLocationGuid = importedLocationGuid;
            person.RegistrationTime = DateTimeOffset.UtcNow;
            person.EnvNum = null;
            _context.People.Update(person);
        }
    }

    private void ProcessBallots(List<CdnBallot> ballots, Dictionary<string, Person> peopleCache, Dictionary<string, Person> peopleByName,
        Guid importedLocationGuid, ref int ballotCounter, ImportResultDto result)
    {
        foreach (var ballot in ballots)
        {
            var now = DateTimeOffset.UtcNow;
            var ballotEntity = new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = importedLocationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "IM",
                BallotNumAtComputer = ballotCounter++,
                DateCreated = now,
                DateUpdated = now,
                RowVersion = new byte[8]
            };

            _context.Ballots.Add(ballotEntity);

            var position = 1;
            foreach (var vote in ballot.Votes)
            {
                var nameKey = $"{vote.First?.ToLower()}{vote.Last?.ToLower()}";
                if (peopleByName.TryGetValue(nameKey, out var matchedPerson))
                {
                    var voteEntity = new Vote
                    {
                        BallotGuid = ballotEntity.BallotGuid,
                        PersonGuid = matchedPerson.PersonGuid,
                        PositionOnBallot = position,
                        VoteStatus = VoteStatus.Ok,
                        PersonCombinedInfo = matchedPerson.CombinedInfo,
                        RowVersion = new byte[8]
                    };
                    _context.Votes.Add(voteEntity);
                    result.VotesCreated++;
                }
                else
                {
                    result.Warnings.Add($"Could not match vote '{vote.First} {vote.Last}' in ballot {ballot.index}");
                }
                position++;
            }

            result.BallotsCreated++;
        }
    }

    // Job 1: Import from CdnBallotImport.xsd format
    public async Task<ImportResultDto> ImportCdnBallotsAsync(Guid electionGuid, Stream xmlStream)
    {
        var result = new ImportResultDto();
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "CdnBallotImport.xsd");
            var (validationErrors, xmlDoc) = await ValidateXmlAgainstSchemaAsync(xmlStream, schemaPath);

            if (validationErrors.Any())
            {
                result.Errors.AddRange(validationErrors);
                result.Success = false;
                return result;
            }

            var electionNode = xmlDoc.DocumentElement!;
            var (voters, voterErrors) = ParseVotersFromXml(electionNode);
            result.Errors.AddRange(voterErrors);

            var (ballots, ballotErrors) = ParseBallotsFromXml(electionNode);
            result.Errors.AddRange(ballotErrors);

            var countValidationError = ValidateVoterBallotCounts(voters, ballots);
            if (countValidationError != null)
            {
                result.Errors.Add(countValidationError);
                result.Success = false;
                return result;
            }

            var importedLocation = await EnsureImportedLocationExistsAsync(electionGuid);
            await UpdateElectionVotingMethodsAsync(electionGuid);

            var (peopleCache, peopleByName, ballotCounter, missingBahaiIds) = await PrepareVoterProcessingAsync(
                electionGuid, importedLocation.LocationGuid, voters);

            foreach (var missingId in missingBahaiIds)
            {
                result.Errors.Add($"Voter with BahaiId {missingId} not found in election");
            }

            ProcessVoters(voters, peopleCache, importedLocation.LocationGuid, result);

            ProcessBallots(ballots, peopleCache, peopleByName, importedLocation.LocationGuid, ref ballotCounter, result);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"Import failed: {ex.Message}");
            result.Success = false;
        }

        return result;
    }
}