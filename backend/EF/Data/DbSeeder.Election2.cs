using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Identity;
using Microsoft.AspNetCore.Identity;

namespace Backend.EF.Data;

public static partial class DbSeeder
{
    private static async Task SeedElection2Async(MainDbContext context, UserManager<AppUser> userManager, ILogger logger)
    {
        logger.LogInformation("Seeding Election 2: National Convention...");

        var electionGuid = CreateGuid("NationalConvention2024");

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "National Convention 2024",
            ElectionType = ElectionTypeEnum.Con.Code,
            ElectionMode = ElectionModeEnum.Normal.Code,
            NumberToElect = 9,
            DateOfElection = DateTimeOffset.Now.AddDays(-30),
            ElectionStage = ElectionStage.ProcessingBallots,
            ShowFullReport = true,
            VotingMethods = "IP",
            OwnerLoginId = "admin@tallyj.test",
            ShowAsTest = true
        };
        context.Elections.Add(election);

        var locationGuid = CreateGuid("ConventionHall");
        var location = new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = electionGuid,
            Name = "Convention Hall",
            ContactInfo = "National Center"
        };
        context.Locations.Add(location);

        var delegateNames = new[]
        {
            ("Alice", "Adams"), ("Bob", "Baker"), ("Carol", "Collins"),
            ("David", "Dixon"), ("Eve", "Evans"), ("Frank", "Foster"),
            ("Grace", "Green"), ("Henry", "Hughes"), ("Iris", "Irving"),
            ("Jack", "Jenkins"), ("Karen", "Kelly"), ("Leo", "Lopez"),
            ("Maria", "Morris"), ("Nathan", "Nelson"), ("Olivia", "Owen")
        };

        var people = new List<Person>();
        for (int i = 0; i < delegateNames.Length; i++)
        {
            var (firstName, lastName) = delegateNames[i];
            var personGuid = CreateGuid($"Delegate{electionGuid}{i}");

            var person = new Person
            {
                PersonGuid = personGuid,
                ElectionGuid = electionGuid,
                FirstName = firstName,
                LastName = lastName,
                CanVote = true,
                CanReceiveVotes = true,
                AgeGroup = "A",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@convention.test",
                Phone = $"555-{2000 + i:D4}",
                VotingMethod = "I",
                VotingLocationGuid = locationGuid
            };
            people.Add(person);
        }
        context.People.AddRange(people);

        var voteDistribution = new Dictionary<int, int>
        {
            [0] = 15,
            [1] = 14,
            [2] = 14,
            [3] = 13,
            [4] = 13,
            [5] = 12,
            [6] = 12,
            [7] = 12,
            [8] = 12,
            [9] = 5,
            [10] = 5,
            [11] = 4,
            [12] = 3,
            [13] = 2,
            [14] = 1
        };

        var ballots = new List<Ballot>();
        var votes = new List<Vote>();

        for (int b = 0; b < 15; b++)
        {
            var ballotGuid = CreateGuid($"BallotConv{electionGuid}{b}");

            var nowConv = DateTimeOffset.UtcNow;
            var ballot = new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = locationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A",
                BallotNumAtComputer = b + 1,
                Teller1 = "Convention Teller",
                DateCreated = nowConv,
                DateUpdated = nowConv
            };
            ballots.Add(ballot);

            var selectedPersonIndices = new HashSet<int>();
            while (selectedPersonIndices.Count < 9)
            {
                int personIndex = -1;
                var rand = new Random(b * 1000 + selectedPersonIndices.Count).Next(100);

                if (rand < 60 && !selectedPersonIndices.Contains(0)) personIndex = 0;
                else if (rand < 75 && !selectedPersonIndices.Contains(1)) personIndex = 1;
                else if (rand < 85 && !selectedPersonIndices.Contains(2)) personIndex = 2;
                else if (rand < 90 && !selectedPersonIndices.Contains(3)) personIndex = 3;
                else if (rand < 93 && !selectedPersonIndices.Contains(4)) personIndex = 4;
                else if (rand < 95 && !selectedPersonIndices.Contains(5)) personIndex = 5;
                else if (rand < 96 && !selectedPersonIndices.Contains(6)) personIndex = 6;
                else if (rand < 97 && !selectedPersonIndices.Contains(7)) personIndex = 7;
                else if (rand < 98 && !selectedPersonIndices.Contains(8)) personIndex = 8;
                else
                {
                    for (int i = 9; i < 15; i++)
                    {
                        if (!selectedPersonIndices.Contains(i))
                        {
                            personIndex = i;
                            break;
                        }
                    }
                }

                if (personIndex >= 0)
                {
                    selectedPersonIndices.Add(personIndex);
                }
            }

            int position = 1;
            foreach (var personIndex in selectedPersonIndices)
            {
                votes.Add(new Vote
                {
                    BallotGuid = ballotGuid,
                    PositionOnBallot = position++,
                    PersonGuid = people[personIndex].PersonGuid,
                    VoteStatus = VoteStatus.Ok
                });
            }
        }

        context.Ballots.AddRange(ballots);
        context.Votes.AddRange(votes);

        var voteCounts = votes
            .GroupBy(v => v.PersonGuid)
            .OrderByDescending(g => g.Count())
            .Select((g, index) => new { PersonGuid = g.Key, Count = g.Count(), Rank = index + 1 })
            .ToList();

        var results = new List<Result>();
        foreach (var vc in voteCounts)
        {
            results.Add(new Result
            {
                ElectionGuid = electionGuid,
                PersonGuid = vc.PersonGuid!.Value,
                VoteCount = vc.Count,
                Rank = vc.Rank,
                Section = vc.Rank <= 9 ? "T" : "F"
            });
        }
        context.Results.AddRange(results);

        context.ResultSummaries.Add(new ResultSummary
        {
            ElectionGuid = electionGuid,
            ResultType = "F",
            NumVoters = 15,
            BallotsNeedingReview = 0,
            TotalVotes = votes.Count,
            UseOnReports = true
        });

        var tieGroup = voteCounts.Where(vc => vc.Count == 5).Select(vc => vc.Rank).ToList();
        if (tieGroup.Count >= 2)
        {
            context.ResultTies.Add(new ResultTie
            {
                ElectionGuid = electionGuid,
                TieBreakGroup = 1,
                NumInTie = tieGroup.Count,
                NumToElect = 0,
                TieBreakRequired = true
            });
        }

        var adminUser = await userManager.FindByEmailAsync("admin@tallyj.test");
        var googleUser = await userManager.FindByEmailAsync("glen.little@gmail.com");

        if (adminUser != null)
        {
            context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = electionGuid,
                UserId = Guid.Parse(adminUser.Id),
                Role = "Owner"
            });
        }

        if (googleUser != null)
        {
            context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = electionGuid,
                UserId = Guid.Parse(googleUser.Id),
                Role = "Owner"
            });
        }

        logger.LogInformation("Seeded Election 2 with {PeopleCount} delegates, {BallotCount} ballots, {VoteCount} votes, {ResultCount} results",
            people.Count, ballots.Count, votes.Count, results.Count);
    }
}
