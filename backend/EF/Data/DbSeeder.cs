using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.Domain.Enumerations;
using Backend.Domain.Identity;
using Backend.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.EF.Data;

/// <summary>
/// Static class responsible for seeding the database with initial test data.
/// Creates sample elections, users, roles, and related entities for development and testing.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds the database with initial data if it hasn't been seeded already.
    /// Creates roles, users, and sample elections with associated data.
    /// </summary>
    /// <param name="context">The main database context.</param>
    /// <param name="userManager">The user manager for identity operations.</param>
    /// <param name="roleManager">The role manager for identity operations.</param>
    /// <param name="logger">The logger for recording seeding operations.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static async Task SeedAsync(
        MainDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        if (await context.Elections.AnyAsync())
        {
            logger.LogInformation("Database already seeded");
            return;
        }

        logger.LogInformation("Starting database seeding...");

        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, logger);
        await SeedElection1Async(context, userManager, logger);
        await SeedElection2Async(context, userManager, logger);
        await SeedLogsAsync(context, logger);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeding complete");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        logger.LogInformation("Seeding roles...");

        var roles = new[] { "Admin", "Teller", "Guest" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {Role}", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager, ILogger logger)
    {
        logger.LogInformation("Seeding users...");

        var users = new[]
        {
            new { Email = "admin@tallyj.test", Password = "TestPass123!", Role = "Admin" },
            new { Email = "teller@tallyj.test", Password = "TestPass123!", Role = "Teller" },
            new { Email = "voter@tallyj.test", Password = "TestPass123!", Role = "Guest" }
        };

        foreach (var userData in users)
        {
            var existingUser = await userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {
                var user = new AppUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, userData.Role);
                    logger.LogInformation("Created user: {Email} with role {Role}", userData.Email, userData.Role);
                }
                else
                {
                    logger.LogError("Failed to create user {Email}: {Errors}",
                        userData.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

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
            DateOfElection = DateTime.Now.AddDays(-3),
            TallyStatus = "Tallying",
            OnlineWhenOpen = DateTime.Now.AddDays(-7),
            OnlineWhenClose = DateTime.Now.AddDays(3),
            OnlineCloseIsEstimate = true,
            VotingMethods = "IP,OL",
            OwnerLoginId = "admin@tallyj.test",
            ListForPublic = false,
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
        context.People.AddRange(people);

        var ballots = new List<Ballot>();
        var votes = new List<Vote>();

        for (int i = 0; i < 15; i++)
        {
            var ballotGuid = CreateGuid($"BallotIP{electionGuid}{i}");
            var statusCode = i < 12 ? BallotStatus.Ok : (i < 14 ? BallotStatus.Review : BallotStatus.Verify);
            var computerCode = i < 8 ? "A" : "B";
            var ballotNum = (i < 8 ? i : i - 8) + 1;

            var ballot = new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = i % 2 == 0 ? mainHallGuid : commCenterGuid,
                StatusCode = statusCode,
                ComputerCode = computerCode,
                BallotNumAtComputer = ballotNum,
                Teller1 = "Teller A",
                Teller2 = "Teller B"
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
                        StatusCode = "Ok"
                    });
                }
            }
        }

        for (int i = 0; i < 5; i++)
        {
            var ballotGuid = CreateGuid($"BallotOL{electionGuid}{i}");

            var ballot = new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = mainHallGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "OL",
                BallotNumAtComputer = i + 1
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
                    StatusCode = "Ok"
                });
            }
        }

        context.Ballots.AddRange(ballots);
        context.Votes.AddRange(votes);

        var tellers = new[]
        {
            new Teller
            {
                ElectionGuid = electionGuid,
                Name = "Admin User",
                IsHeadTeller = true
            },
            new Teller
            {
                ElectionGuid = electionGuid,
                Name = "Teller User",
                IsHeadTeller = false
            }
        };
        context.Tellers.AddRange(tellers);

        var messages = new[]
        {
            new Message
            {
                ElectionGuid = electionGuid,
                Title = "Welcome",
                Details = "Welcome to the Springfield LSA Election 2024",
                AsOf = DateTime.Now.AddDays(-5)
            },
            new Message
            {
                ElectionGuid = electionGuid,
                Title = "Voting Instructions",
                Details = "Please vote for up to 9 candidates",
                AsOf = DateTime.Now.AddDays(-4)
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
                WhenBallotCreated = i < 3 ? DateTime.Now.AddDays(-rng.Next(1, 5)) : null,
                WhenStatus = DateTime.Now.AddHours(-rng.Next(1, 48))
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
            DateOfElection = DateTime.Now.AddDays(-30),
            TallyStatus = "Finalized",
            ShowFullReport = true,
            VotingMethods = "IP",
            OwnerLoginId = "admin@tallyj.test",
            ListForPublic = false,
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

            var ballot = new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = locationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A",
                BallotNumAtComputer = b + 1,
                Teller1 = "Convention Teller"
            };
            ballots.Add(ballot);

            var selectedCandidates = new HashSet<int>();
            while (selectedCandidates.Count < 9)
            {
                int candidateIndex = -1;
                var rand = new Random(b * 1000 + selectedCandidates.Count).Next(100);

                if (rand < 60 && !selectedCandidates.Contains(0)) candidateIndex = 0;
                else if (rand < 75 && !selectedCandidates.Contains(1)) candidateIndex = 1;
                else if (rand < 85 && !selectedCandidates.Contains(2)) candidateIndex = 2;
                else if (rand < 90 && !selectedCandidates.Contains(3)) candidateIndex = 3;
                else if (rand < 93 && !selectedCandidates.Contains(4)) candidateIndex = 4;
                else if (rand < 95 && !selectedCandidates.Contains(5)) candidateIndex = 5;
                else if (rand < 96 && !selectedCandidates.Contains(6)) candidateIndex = 6;
                else if (rand < 97 && !selectedCandidates.Contains(7)) candidateIndex = 7;
                else if (rand < 98 && !selectedCandidates.Contains(8)) candidateIndex = 8;
                else
                {
                    for (int i = 9; i < 15; i++)
                    {
                        if (!selectedCandidates.Contains(i))
                        {
                            candidateIndex = i;
                            break;
                        }
                    }
                }

                if (candidateIndex >= 0)
                {
                    selectedCandidates.Add(candidateIndex);
                }
            }

            int position = 1;
            foreach (var candidateIndex in selectedCandidates)
            {
                votes.Add(new Vote
                {
                    BallotGuid = ballotGuid,
                    PositionOnBallot = position++,
                    PersonGuid = people[candidateIndex].PersonGuid,
                    StatusCode = "Ok"
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

    private static async Task SeedLogsAsync(MainDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding logs...");

        var electionGuid = CreateGuid("SpringfieldLSA2024");
        var logs = new[]
        {
            new Log
            {
                AsOf = DateTime.Now.AddDays(-30),
                ElectionGuid = electionGuid,
                Details = "Election created"
            },
            new Log
            {
                AsOf = DateTime.Now.AddDays(-25),
                ElectionGuid = electionGuid,
                Details = "Voters imported from CSV"
            },
            new Log
            {
                AsOf = DateTime.Now.AddDays(-20),
                ElectionGuid = electionGuid,
                Details = "Online voting enabled"
            },
            new Log
            {
                AsOf = DateTime.Now.AddDays(-7),
                ElectionGuid = electionGuid,
                Details = "Voting period started"
            },
            new Log
            {
                AsOf = DateTime.Now.AddDays(-3),
                ElectionGuid = electionGuid,
                Details = "Ballot entry began"
            }
        };
        context.Logs.AddRange(logs);
    }

    private static Guid CreateGuid(string seed)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }
}



