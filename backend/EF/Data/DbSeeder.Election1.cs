using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.EF.Data;

public static partial class DbSeeder
{
    private static async Task SeedElection1Async(MainDbContext context, UserManager<AppUser> userManager, ILogger logger)
    {
        logger.LogInformation("Seeding Election 1: Springfield LSA...");

        var electionGuid = CreateGuid("SpringfieldLSA2024");

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Springfield Local Spiritual Assembly Election 2024",
            ElectionType = ElectionTypeEnum.LSA.Code,
            ElectionMode = ElectionModeEnum.Normal.Code,
            NumberToElect = 9,
            DateOfElection = DateTimeOffset.Now.AddDays(-3),
            ElectionStage = ElectionStage.ProcessingBallots,
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTimeOffset.Now.AddDays(-7),
            OnlineWhenClose = DateTimeOffset.Now.AddDays(3),
            OnlineCloseIsEstimate = true,
            OnlineSelectionProcess = "A",
            VotingMethods = "IP,OL",
            OwnerLoginId = "admin@tallyj.test",
            ShowAsTest = true
        };
        context.Elections.Add(election);

        var mainHallGuid = CreateGuid("MainHall");
        var commCenterGuid = CreateGuid("CommunityCenter");

        var locations = new[]
        {
            new Location
            {
                LocationGuid = mainHallGuid,
                ElectionGuid = electionGuid,
                Name = "Main Hall",
                ContactInfo = "123 Main Street"
            },
            new Location
            {
                LocationGuid = commCenterGuid,
                ElectionGuid = electionGuid,
                Name = "Community Center",
                ContactInfo = "456 Center Avenue"
            }
        };
        context.Locations.AddRange(locations);

        var rng = new Random(42);
        var firstNames = new[] { "John", "Mary", "Robert", "Patricia", "Michael", "Jennifer", "William", "Linda", "David", "Elizabeth",
            "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy",
            "Daniel", "Lisa", "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor",
            "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin", "Thompson", "Garcia", "Martinez", "Robinson",
            "Clark", "Rodriguez", "Lewis", "Lee", "Walker", "Hall", "Allen", "Young", "King", "Wright" };

        var people = new List<Person>();
        for (int i = 0; i < 30; i++)
        {
            var firstName = firstNames[rng.Next(firstNames.Length)];
            var lastName = lastNames[rng.Next(lastNames.Length)];
            var personGuid = CreateGuid($"Person{electionGuid}{i}");

            var person = new Person
            {
                PersonGuid = personGuid,
                ElectionGuid = electionGuid,
                FirstName = firstName,
                LastName = lastName,
                CanVote = i < 28,
                CanReceiveVotes = true,
                AgeGroup = "A",
                Email = i < 20 ? $"{firstName.ToLower()}.{lastName.ToLower()}{i}@test.com" : null,
                Phone = i < 15 && i % 2 == 0 ? $"555-{1000 + i:D4}" : null,
                VotingMethod = i < 15 ? "I" : (i < 25 ? "O" : "K"),
                BahaiId = i % 3 == 0 ? $"{100000000 + i}" : null,
                VotingLocationGuid = i % 2 == 0 ? mainHallGuid : commCenterGuid
            };
            people.Add(person);
        }
        people.Add(new Person
        {
            PersonGuid = CreateGuid($"Person{electionGuid}VoterTest"),
            ElectionGuid = electionGuid,
            FirstName = "Test",
            LastName = "Voter",
            CanVote = true,
            CanReceiveVotes = false,
            Email = "voter@tallyj.test",
            KioskCode = "VTEST",
            VotingMethod = "O",
            VotingLocationGuid = mainHallGuid
        });

        context.People.AddRange(people);

        var ballots = new List<Ballot>();
        var votes = new List<Vote>();

        for (int i = 0; i < 15; i++)
        {
            var ballotGuid = CreateGuid($"BallotIP{electionGuid}{i}");
            var statusCode = i < 12 ? BallotStatus.Ok : (i < 14 ? BallotStatus.Review : BallotStatus.Verify);
            var computerCode = i < 8 ? "A" : "B";
            var ballotNum = (i < 8 ? i : i - 8) + 1;

            var now = DateTimeOffset.UtcNow;
            var ballot = new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = i % 2 == 0 ? mainHallGuid : commCenterGuid,
                StatusCode = statusCode,
                ComputerCode = computerCode,
                BallotNumAtComputer = ballotNum,
                Teller1 = "Teller A",
                Teller2 = "Teller B",
                DateCreated = now,
                DateUpdated = now
            };
            ballots.Add(ballot);

            if (statusCode == BallotStatus.Ok)
            {
                var numVotes = rng.Next(1, 10);
                for (int v = 0; v < numVotes; v++)
                {
                    var votePersonGuid = people[rng.Next(people.Count)].PersonGuid;
                    votes.Add(new Vote
                    {
                        BallotGuid = ballotGuid,
                        PositionOnBallot = v + 1,
                        PersonGuid = votePersonGuid,
                        VoteStatus = VoteStatus.Ok
                    });
                }
            }
        }

        for (int i = 0; i < 5; i++)
        {
            var ballotGuid = CreateGuid($"BallotOL{electionGuid}{i}");

            var nowOl = DateTimeOffset.UtcNow;
            var ballot = new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = mainHallGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "OL",
                BallotNumAtComputer = i + 1,
                DateCreated = nowOl,
                DateUpdated = nowOl
            };
            ballots.Add(ballot);

            var numVotes = rng.Next(5, 10);
            for (int v = 0; v < numVotes; v++)
            {
                var votePersonGuid = people[rng.Next(people.Count)].PersonGuid;
                votes.Add(new Vote
                {
                    BallotGuid = ballotGuid,
                    PositionOnBallot = v + 1,
                    PersonGuid = votePersonGuid,
                    VoteStatus = VoteStatus.Ok
                });
            }
        }

        context.Ballots.AddRange(ballots);
        context.Votes.AddRange(votes);

        var messages = new[]
        {
            new Message
            {
                ElectionGuid = electionGuid,
                Title = "Welcome",
                Details = "Welcome to the Springfield LSA Election 2024",
                AsOf = DateTimeOffset.Now.AddDays(-5)
            },
            new Message
            {
                ElectionGuid = electionGuid,
                Title = "Voting Instructions",
                Details = "Please vote for up to 9 people",
                AsOf = DateTimeOffset.Now.AddDays(-4)
            }
        };
        context.Messages.AddRange(messages);

        for (int i = 0; i < 5; i++)
        {
            var person = people[i * 4];
            context.OnlineVotingInfos.Add(new OnlineVotingInfo
            {
                ElectionGuid = electionGuid,
                PersonGuid = person.PersonGuid,
                Status = i < 3 ? "Used" : "Sent",
                WhenBallotCreated = i < 3 ? DateTimeOffset.Now.AddDays(-rng.Next(1, 5)) : null,
                WhenStatus = DateTimeOffset.Now.AddHours(-rng.Next(1, 48))
            });
        }

        var adminUser = await userManager.FindByEmailAsync("admin@tallyj.test");
        var tellerUser = await userManager.FindByEmailAsync("teller@tallyj.test");
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

        if (tellerUser != null)
        {
            context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = electionGuid,
                UserId = Guid.Parse(tellerUser.Id),
                Role = "Teller"
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

        logger.LogInformation("Seeded Election 1 with {LocationCount} locations, {PeopleCount} people, {BallotCount} ballots, {VoteCount} votes",
            locations.Length, people.Count, ballots.Count, votes.Count);
    }

    private static async Task SeedElection1OnlineVotingAsync(MainDbContext context, ILogger logger)
    {
        var electionGuid = CreateGuid("SpringfieldLSA2024");
        var election = await context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);
        if (election == null)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        election.UseOnlineVoting = true;
        if (election.OnlineWhenOpen == null || election.OnlineWhenOpen > now)
        {
            election.OnlineWhenOpen = now.AddDays(-7);
        }

        if (election.OnlineWhenClose == null || election.OnlineWhenClose <= now)
        {
            election.OnlineWhenClose = now.AddDays(3);
        }

        if (string.IsNullOrEmpty(election.OnlineSelectionProcess))
        {
            election.OnlineSelectionProcess = "A";
        }

        if (election.VotingMethods == null || !election.VotingMethods.Contains("OL", StringComparison.OrdinalIgnoreCase))
        {
            election.VotingMethods = string.IsNullOrWhiteSpace(election.VotingMethods)
                ? "OL"
                : $"{election.VotingMethods},OL";
        }

        var mainHallGuid = CreateGuid("MainHall");
        if (!await context.Locations.AnyAsync(l => l.LocationGuid == mainHallGuid))
        {
            context.Locations.Add(new Location
            {
                LocationGuid = mainHallGuid,
                ElectionGuid = electionGuid,
                Name = "Main Hall",
                ContactInfo = "123 Main Street"
            });
        }

        var voterPersonGuid = CreateGuid($"Person{electionGuid}VoterTest");
        var existingVoter = await context.People
            .FirstOrDefaultAsync(p => p.ElectionGuid == electionGuid &&
                                      (p.Email == "voter@tallyj.test" || p.KioskCode == "VTEST"));

        if (existingVoter == null)
        {
            logger.LogInformation("Adding Election 1 online test voter (voter@tallyj.test / VTEST)...");
            context.People.Add(new Person
            {
                PersonGuid = voterPersonGuid,
                ElectionGuid = electionGuid,
                FirstName = "Test",
                LastName = "Voter",
                CanVote = true,
                CanReceiveVotes = false,
                Email = "voter@tallyj.test",
                KioskCode = "VTEST",
                VotingMethod = "O",
                VotingLocationGuid = mainHallGuid
            });
        }
        else
        {
            existingVoter.Email ??= "voter@tallyj.test";
            existingVoter.KioskCode ??= "VTEST";
            existingVoter.CanVote = true;
            existingVoter.VotingMethod = "O";
        }

        var phoneVoter = await context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true && p.Phone != null)
            .OrderBy(p => p.RowId)
            .FirstOrDefaultAsync();
        if (phoneVoter != null)
        {
            phoneVoter.Phone = "+15551000";
        }
    }
}
