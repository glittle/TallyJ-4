using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.EF.Data;

public static partial class DbSeeder
{
    private static async Task SeedOnlineVotingTestElectionsAsync(MainDbContext context, ILogger logger)
    {
        var randomGuid = CreateGuid("OnlineVotingRandom2024");
        var bothGuid = CreateGuid("OnlineVotingBoth2024");

        if (!await context.Elections.AnyAsync(e => e.ElectionGuid == randomGuid))
        {
            logger.LogInformation("Seeding online voting test election (random mode B)...");

            context.Elections.Add(new Election
            {
                ElectionGuid = randomGuid,
                Name = "Online Voting Test — Random Names (B)",
                ElectionType = ElectionTypeEnum.LSA.Code,
                ElectionMode = ElectionModeEnum.Normal.Code,
                NumberToElect = 9,
                DateOfElection = DateTimeOffset.Now.AddDays(7),
                ElectionStage = ElectionStage.GatheringBallots,
                OnlineWhenOpen = DateTimeOffset.Now.AddDays(-1),
                OnlineWhenClose = DateTimeOffset.Now.AddDays(14),
                OnlineCloseIsEstimate = true,
                OnlineSelectionProcess = "B",
                VotingMethods = "OL",
                OwnerLoginId = "admin@tallyj.test",
                ShowAsTest = true
            });

            var voterPersonGuid = CreateGuid($"Person{randomGuid}VoterTest");
            context.People.Add(new Person
            {
                PersonGuid = voterPersonGuid,
                ElectionGuid = randomGuid,
                FirstName = "Test",
                LastName = "Voter",
                CanVote = true,
                CanReceiveVotes = true,
                Email = "voter-random@tallyj.test",
                VotingMethod = "O"
            });

            for (var i = 0; i < 12; i++)
            {
                context.People.Add(new Person
                {
                    PersonGuid = CreateGuid($"Person{randomGuid}{i}"),
                    ElectionGuid = randomGuid,
                    FirstName = $"Person{i}",
                    LastName = "Random",
                    CanVote = false,
                    CanReceiveVotes = true,
                    VotingMethod = "O"
                });
            }
        }

        if (!await context.Elections.AnyAsync(e => e.ElectionGuid == bothGuid))
        {
            logger.LogInformation("Seeding online voting test election (list + random mode C)...");

            context.Elections.Add(new Election
            {
                ElectionGuid = bothGuid,
                Name = "Online Voting Test — List + Pool (C)",
                ElectionType = ElectionTypeEnum.LSA.Code,
                ElectionMode = ElectionModeEnum.Normal.Code,
                NumberToElect = 9,
                DateOfElection = DateTimeOffset.Now.AddDays(7),
                ElectionStage = ElectionStage.GatheringBallots,
                OnlineWhenOpen = DateTimeOffset.Now.AddDays(-1),
                OnlineWhenClose = DateTimeOffset.Now.AddDays(14),
                OnlineCloseIsEstimate = true,
                OnlineSelectionProcess = "C",
                VotingMethods = "OL",
                OwnerLoginId = "admin@tallyj.test",
                ShowAsTest = true
            });

            context.People.Add(new Person
            {
                PersonGuid = CreateGuid($"Person{bothGuid}VoterTest"),
                ElectionGuid = bothGuid,
                FirstName = "Test",
                LastName = "Voter",
                CanVote = true,
                CanReceiveVotes = true,
                Email = "voter-both@tallyj.test",
                VotingMethod = "O"
            });

            for (var i = 0; i < 12; i++)
            {
                context.People.Add(new Person
                {
                    PersonGuid = CreateGuid($"Person{bothGuid}{i}"),
                    ElectionGuid = bothGuid,
                    FirstName = $"Person{i}",
                    LastName = "Both",
                    CanVote = false,
                    CanReceiveVotes = true,
                    VotingMethod = "O"
                });
            }
        }
    }
}
