using System.Text;
using System.Text.Json;
using Backend.Configuration;
using Backend.DTOs.Import;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Localization;
using Backend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PhoneNumbers;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Phone, email, consecutive-run, and threshold checks on a voter list.
/// </summary>
public class VoterContactReviewerTests : ServiceTestBase
{
    private readonly Guid _electionId = Guid.NewGuid();
    private readonly RecordingAlerts _alerts = new();
    private readonly RecordingMx _mx = new();
    private readonly FakeDisposableDomains _domains = new(new[] { "mailinator.com" });

    public VoterContactReviewerTests()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = _electionId,
            Name = "Review",
            ElectionStage = ElectionStage.SettingUp,
            ExpectedPhoneRegions = "CA",
            RowVersion = new byte[8]
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task InvalidPhone_AndUnexpectedCountry_AreFlagged()
    {
        AddPerson("not-a-phone", null);
        AddPerson("+442079460958", null);
        await Context.SaveChangesAsync();

        await CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId);

        var reasons = Context.VoterContactFlags.Where(row => row.Active).Select(row => row.Reason).ToList();
        Assert.Contains(VoterContactFlagReason.InvalidPhone, reasons);
        Assert.Contains(VoterContactFlagReason.UnexpectedCountry, reasons);
        Assert.False(Context.ElectionSendControls.Any(row => row.Flagged));
    }

    [Fact]
    public async Task FourFlaggedContacts_FlagTheElection_ThreeDoNot()
    {
        var first = AddPerson("bad-1", null);
        var second = AddPerson("bad-2", null);
        var third = AddPerson("bad-3", null);
        await Context.SaveChangesAsync();
        await CreateReviewer(threshold: 3).ReviewElectionAsync(_electionId, AsUpload(first, second, third));
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false);

        var fourth = AddPerson("bad-4", null);
        await Context.SaveChangesAsync();
        await CreateReviewer(threshold: 3).ReviewElectionAsync(_electionId, AsUpload(fourth));
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false);

        await CreateReviewer(threshold: 3).ReviewElectionAsync(
            _electionId,
            AsUpload(first, second, third, fourth));

        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged);
        Assert.Equal(4, control.FlaggedEntryCount);
        Assert.Single(_alerts.Flagged);
        Assert.Contains(
            Context.SecurityAuditLogs,
            log => log.EventType == SecurityEventType.ElectionFlagged);
    }

    [Fact]
    public async Task ConsecutiveValidNumbers_AreFlagged()
    {
        var numbers = FourConsecutiveCanadianNumbers();
        foreach (var number in numbers)
        {
            AddPerson(number, null);
        }

        await Context.SaveChangesAsync();
        await CreateReviewer(threshold: 10, runLength: 4).ReviewElectionAsync(_electionId);

        Assert.Equal(4, Context.VoterContactFlags.Count(row => row.Active && row.Reason == VoterContactFlagReason.ConsecutiveRun));
    }

    [Fact]
    public async Task DisposableDomain_IsFlagged_WithoutMxLookup()
    {
        AddPerson(null, "a@mailinator.com");
        await Context.SaveChangesAsync();

        await CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId);

        Assert.Contains(Context.VoterContactFlags, row => row.Active && row.Reason == VoterContactFlagReason.DisposableDomain);
        Assert.Empty(_mx.LookedUp);
    }

    [Fact]
    public async Task MissingMx_IsFlagged_TimeoutIsNot()
    {
        AddPerson(null, "a@nomail.example");
        AddPerson(null, "b@slow.example");
        await Context.SaveChangesAsync();
        _mx.Results["nomail.example"] = MxLookupResult.NoMx;
        _mx.Results["slow.example"] = MxLookupResult.Unknown;

        await CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId);

        var flagged = Context.VoterContactFlags.Where(row => row.Active).Select(row => row.ContactKey).ToList();
        Assert.Contains(VoterContactReviewer.HashContactKey("email:a@nomail.example"), flagged);
        Assert.DoesNotContain(VoterContactReviewer.HashContactKey("email:b@slow.example"), flagged);
    }

    [Fact]
    public async Task NullMx_IsFlagged()
    {
        AddPerson(null, "a@nullmx.example");
        await Context.SaveChangesAsync();
        _mx.Results["nullmx.example"] = MxLookupResult.NullMx;

        await CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId);

        Assert.Contains(Context.VoterContactFlags, row => row.Active && row.Reason == VoterContactFlagReason.NoMx);
    }

    [Fact]
    public async Task SourceRowNumber_IsStored()
    {
        var personId = Guid.NewGuid();
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = personId,
            FirstName = "A",
            LastName = "B",
            Phone = "bad",
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        await CreateReviewer(threshold: 10).ReviewElectionAsync(
            _electionId,
            new Dictionary<Guid, int> { [personId] = 12 });

        Assert.Equal(12, Context.VoterContactFlags.Single(row => row.Active).SourceRowNumber);
    }

    [Fact]
    public async Task LongEmail_FitsTheFlagColumns()
    {
        var domain = new string('x', 243) + ".test";
        var email = "a@" + domain;
        Assert.Equal(250, email.Length);
        AddPerson(null, email);
        await Context.SaveChangesAsync();
        _mx.Results[domain] = MxLookupResult.NoMx;

        await CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId);

        var flag = Context.VoterContactFlags.Single(row => row.Active);
        Assert.Equal(VoterContactFlagReason.NoMx, flag.Reason);
        Assert.InRange(flag.MaskedValue.Length, 1, 80);
        Assert.Equal(64, flag.ContactKey.Length);
        Assert.Equal(VoterContactReviewer.HashContactKey("email:" + email), flag.ContactKey);
    }

    [Fact]
    public async Task DefaultThreshold_ThreeRealisticPhonesDoNotFlag_FourDo()
    {
        var first = AddPerson("000-000-0000", null);
        var second = AddPerson("111-111-1111", null);
        var third = AddPerson("222-222-2222", null);
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(first, second, third));

        Assert.InRange(DistinctActiveKeys(), 1, 3);
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false);

        var fourth = AddPerson("333-333-3333", null);
        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(
            _electionId,
            AsUpload(first, second, third, fourth));

        var described = DescribeActiveFlags();
        Assert.Equal(4, DistinctActiveKeys());
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, described);
        Assert.Equal(4, control.FlaggedEntryCount);
    }

    [Fact]
    public async Task TwoPhonesAndOneDisposableEmail_DoNotFlag()
    {
        var first = AddPerson("000-000-0000", null);
        var second = AddPerson("111-111-1111", null);
        var third = AddPerson(null, "a@mailinator.com");
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(first, second, third));

        Assert.Equal(3, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task ThreeUnexpectedCountryNumbers_CountOnceEach_AndDoNotFlag()
    {
        var first = AddPerson("+442079460111", null);
        var second = AddPerson("+442079460222", null);
        var third = AddPerson("+442079460333", null);
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(first, second, third));

        Assert.Equal(3, DistinctActiveKeys());
        Assert.Equal(3, Context.VoterContactFlags.Count(row => row.Active));
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task Import_ThreeBadPhones_DoNotFlag_ALaterSinglePhoneDoesNotFlag()
    {
        var three = await ImportContacts(
            "FirstName,LastName,Phone\nA,One,000-000-0000\nB,Two,111-111-1111\nC,Three,222-222-2222",
            PhoneMappings());

        Assert.True(three.Success, string.Join("; ", three.Errors.Select(error => error.Key)));
        Assert.Equal(3, Context.People.Count(person => person.ElectionGuid == _electionId));
        Assert.InRange(DistinctActiveKeys(), 1, 3);
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());

        var four = await ImportContacts(
            "FirstName,LastName,Phone\nD,Four,333-333-3333",
            PhoneMappings());

        Assert.True(four.Success, string.Join("; ", four.Errors.Select(error => error.Key)));
        Assert.Equal(4, Context.People.Count(person => person.ElectionGuid == _electionId));
        Assert.Equal(4, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task Import_FourBadPhonesInOneFile_Flags()
    {
        var result = await ImportContacts(
            "FirstName,LastName,Phone\nA,One,000-000-0000\nB,Two,111-111-1111\nC,Three,222-222-2222\nD,Four,333-333-3333",
            PhoneMappings());

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(error => error.Key)));
        Assert.Equal(4, DistinctActiveKeys());
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, DescribeActiveFlags());
        Assert.Equal(4, control.FlaggedEntryCount);
    }

    [Fact]
    public async Task Import_TwoPhonesAndOneEmail_DoNotFlag()
    {
        var result = await ImportContacts(
            "FirstName,LastName,Phone,Email\nA,One,000-000-0000,\nB,Two,111-111-1111,\nC,Three,,a@mailinator.com",
            [
                new ColumnMappingDto { FileColumn = "FirstName", TargetField = "FirstName" },
                new ColumnMappingDto { FileColumn = "LastName", TargetField = "LastName" },
                new ColumnMappingDto { FileColumn = "Phone", TargetField = "Phone" },
                new ColumnMappingDto { FileColumn = "Email", TargetField = "Email" }
            ]);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(error => error.Key)));
        Assert.Equal(3, Context.People.Count(person => person.ElectionGuid == _electionId));
        Assert.Equal(3, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task MxLookupFailure_DoesNotFlagTheElection_AndKeepsThePeople()
    {
        var personId = AddPerson(null, "a@boom.example");
        await Context.SaveChangesAsync();
        _mx.Failure = new InvalidOperationException("dns client failed");

        var thrown = await Record.ExceptionAsync(() =>
            CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId, AsUpload(personId)));

        Assert.Null(thrown);
        Assert.Single(Context.People);
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false);
        Assert.DoesNotContain(
            Context.VoterContactFlags,
            row => row.Active && row.Reason == VoterContactFlagReason.ReviewFailed);
        Assert.Empty(_alerts.Flagged);
    }

    [Fact]
    public async Task ReviewThrow_FlagsTheElection_AndKeepsThePeople()
    {
        var personId = AddPerson(null, "a@example.com");
        await Context.SaveChangesAsync();
        _domains.Failure = new InvalidOperationException("domain list unavailable");

        var thrown = await Record.ExceptionAsync(() =>
            CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(personId)));

        Assert.Null(thrown);
        Assert.Single(Context.People);
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged);
        Assert.Contains(Context.VoterContactFlags, row => row.Active && row.Reason == VoterContactFlagReason.ReviewFailed);
        Assert.Single(_alerts.Flagged);
        Assert.Contains(_alerts.Flagged[0].Rows, row => row.Reason == VoterContactFlagReason.ReviewFailed);
    }

    [Fact]
    public async Task ReviewFailure_StillFlagsWhenTheUploadIsOverTheThreshold()
    {
        var phones = new[]
        {
            AddPerson("000-000-0000", null),
            AddPerson("111-111-1111", null),
            AddPerson("222-222-2222", null),
            AddPerson("333-333-3333", "a@boom.example")
        };
        await Context.SaveChangesAsync();
        _mx.Failure = new InvalidOperationException("dns client failed");

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(phones));

        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, DescribeActiveFlags());
        Assert.Equal(4, control.FlaggedEntryCount);
        Assert.DoesNotContain(
            Context.VoterContactFlags,
            row => row.Reason == VoterContactFlagReason.ReviewFailed);
    }

    [Fact]
    public async Task Upload_ThreeFictionalPhones_DoNotFlag()
    {
        var phones = new[]
        {
            AddPerson("+1 555 0100", null),
            AddPerson("+1 555 0101", null),
            AddPerson("+1 555 0102", null)
        };
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(phones));

        Assert.Equal(3, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task Upload_TwoFictionalPhonesAndOneEmail_DoNotFlagWhenOlderContactsExist()
    {
        var older = AddPerson("+1 555 0199", null);
        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(older));
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());

        var upload = new[]
        {
            AddPerson("+1 555 0100", null),
            AddPerson("+1 555 0101", null),
            AddPerson(null, "a@mailinator.com")
        };
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(upload));

        Assert.Equal(4, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task Upload_FourFictionalPhones_Flag()
    {
        var phones = new[]
        {
            AddPerson("+1 555 0100", null),
            AddPerson("+1 555 0101", null),
            AddPerson("+1 555 0102", null),
            AddPerson("+1 555 0103", null)
        };
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(phones));

        Assert.Equal(4, DistinctActiveKeys());
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, DescribeActiveFlags());
        Assert.Equal(4, control.FlaggedEntryCount);
    }

    [Fact]
    public async Task SamePhone_TwoSpellings_AreOneContact()
    {
        var spaced = AddPerson("+1 555 0100", null);
        var compact = AddPerson("+15550100", null);
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(spaced, compact));

        Assert.Equal(1, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task UnexpectedCountryAndConsecutiveRun_ShareOneKeyPerNumber()
    {
        var numbers = FourConsecutiveNumbers("GB");
        var people = numbers.Select(number => AddPerson(number, null)).ToArray();
        await Context.SaveChangesAsync();

        await CreateReviewer(threshold: 10, runLength: 4).ReviewElectionAsync(_electionId, AsUpload(people));

        Assert.Equal(4, DistinctActiveKeys());
        Assert.Equal(8, Context.VoterContactFlags.Count(row => row.Active));
        Assert.Equal(4, Context.VoterContactFlags.Count(row => row.Active && row.Reason == VoterContactFlagReason.UnexpectedCountry));
        Assert.Equal(4, Context.VoterContactFlags.Count(row => row.Active && row.Reason == VoterContactFlagReason.ConsecutiveRun));
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task Upload_ThreeWithOlderContactsAtTheCap_DoNotFlag_PastTheCapDoes()
    {
        for (var index = 0; index < 7; index++)
        {
            AddPerson($"{index:000}-000-0000", null);
        }

        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId);
        Assert.Equal(7, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());

        var upload = new[]
        {
            AddPerson("+1 555 0100", null),
            AddPerson("+1 555 0101", null),
            AddPerson(null, "a@mailinator.com")
        };
        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(upload));
        Assert.Equal(10, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());

        var extra = AddPerson("+1 555 0102", null);
        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(extra));

        Assert.Equal(11, DistinctActiveKeys());
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, DescribeActiveFlags());
        Assert.Equal(11, control.FlaggedEntryCount);
    }

    [Fact]
    public async Task UploadsOfThree_FlagOnceTheElectionTotalPassesTheCap()
    {
        for (var file = 0; file < 3; file++)
        {
            var people = new List<Guid>();
            for (var row = 0; row < 3; row++)
            {
                people.Add(AddPerson($"{file:0}{row:00}-000-0000", null));
            }

            await Context.SaveChangesAsync();
            await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(people.ToArray()));
            Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
        }

        Assert.Equal(9, DistinctActiveKeys());

        var fourth = new List<Guid>();
        for (var row = 0; row < 3; row++)
        {
            fourth.Add(AddPerson($"3{row:00}-000-0000", null));
        }

        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(fourth.ToArray()));

        Assert.Equal(12, DistinctActiveKeys());
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, DescribeActiveFlags());
        Assert.Equal(12, control.FlaggedEntryCount);
    }

    [Fact]
    public async Task ManualAdds_FlagWhenTheElectionTotalExceedsTheCap()
    {
        for (var index = 0; index < 10; index++)
        {
            AddPerson($"{index:000}-111-0000", null);
            await Context.SaveChangesAsync();
            await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId);
            Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
        }

        Assert.Equal(10, DistinctActiveKeys());
        AddPerson("010-111-0000", null);
        await Context.SaveChangesAsync();
        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId);

        Assert.Equal(11, DistinctActiveKeys());
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged, DescribeActiveFlags());
        Assert.Equal(11, control.FlaggedEntryCount);
    }

    [Fact]
    public async Task ReviewWithoutUploadMap_FourBadContacts_DoNotFlag()
    {
        AddPerson("000-000-0000", null);
        AddPerson("111-111-1111", null);
        AddPerson("222-222-2222", null);
        AddPerson("333-333-3333", null);
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId);

        Assert.Equal(4, DistinctActiveKeys());
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    [Fact]
    public async Task BlankContacts_NeverCount()
    {
        for (var index = 0; index < 12; index++)
        {
            AddPerson(index % 2 == 0 ? null : " ", index % 3 == 0 ? null : "");
        }

        var upload = new[]
        {
            AddPerson("000-000-0000", null),
            AddPerson("111-111-1111", null),
            AddPerson(null, "a@mailinator.com")
        };
        await Context.SaveChangesAsync();

        await CreateReviewerWithDefaults().ReviewElectionAsync(_electionId, AsUpload(upload));

        Assert.Equal(3, DistinctActiveKeys());
        Assert.Equal(15, Context.People.Count(person => person.ElectionGuid == _electionId));
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false, DescribeActiveFlags());
    }

    private Guid AddPerson(string? phone, string? email)
    {
        var personId = Guid.NewGuid();
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = personId,
            FirstName = "A",
            LastName = "B",
            Phone = phone,
            Email = email,
            RowVersion = new byte[8]
        });
        return personId;
    }

    private static Dictionary<Guid, int> AsUpload(params Guid[] personIds)
    {
        var rows = new Dictionary<Guid, int>();
        for (var index = 0; index < personIds.Length; index++)
        {
            rows[personIds[index]] = index + 1;
        }

        return rows;
    }

    private VoterContactReviewer CreateReviewer(int threshold, int runLength = 4)
    {
        return new VoterContactReviewer(
            Context,
            _domains,
            _mx,
            _alerts,
            new SecurityAuditService(Context, NullLogger<SecurityAuditService>.Instance),
            Options.Create(new AntiAbuseOptions
            {
                FlaggedEntryThreshold = threshold,
                ConsecutivePhoneRunLength = runLength,
                DefaultPhoneRegionCode = "CA"
            }),
            NullLogger<VoterContactReviewer>.Instance);
    }

    private VoterContactReviewer CreateReviewerWithDefaults()
    {
        return new VoterContactReviewer(
            Context,
            _domains,
            _mx,
            _alerts,
            new SecurityAuditService(Context, NullLogger<SecurityAuditService>.Instance),
            Options.Create(new AntiAbuseOptions()),
            NullLogger<VoterContactReviewer>.Instance);
    }

    private int DistinctActiveKeys()
    {
        return Context.VoterContactFlags
            .Where(row => row.Active)
            .Select(row => row.ContactKey)
            .Distinct()
            .Count();
    }

    private string DescribeActiveFlags()
    {
        return string.Join(
            " | ",
            Context.VoterContactFlags
                .Where(row => row.Active)
                .Select(row => row.Reason + " " + row.MaskedValue + " " + row.ContactKey));
    }

    private static List<ColumnMappingDto> PhoneMappings()
    {
        return
        [
            new ColumnMappingDto { FileColumn = "FirstName", TargetField = "FirstName" },
            new ColumnMappingDto { FileColumn = "LastName", TargetField = "LastName" },
            new ColumnMappingDto { FileColumn = "Phone", TargetField = "Phone" }
        ];
    }

    private async Task<ImportPeopleResult> ImportContacts(string csv, List<ColumnMappingDto> mappings)
    {
        var importFile = new ImportFile
        {
            ElectionGuid = _electionId,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(csv),
            HasContent = true,
            ColumnsToRead = JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var service = new PeopleImportService(
            Context,
            new Mock<ISignalRNotificationService>().Object,
            new Mock<IJsonLocalizationProvider>().Object,
            CreateReviewerWithDefaults());
        return await service.ImportPeopleAsync(_electionId, importFile.RowId);
    }

    private static IReadOnlyList<string> FourConsecutiveCanadianNumbers() => FourConsecutiveNumbers("CA");

    private static IReadOnlyList<string> FourConsecutiveNumbers(string region)
    {
        var util = PhoneNumberUtil.GetInstance();
        var example = util.Format(util.GetExampleNumber(region), PhoneNumberFormat.E164);
        var start = long.Parse(example.TrimStart('+'), System.Globalization.CultureInfo.InvariantCulture);
        for (var offset = 0L; offset < 5000; offset++)
        {
            var numbers = new List<string>();
            var allValid = true;
            for (var step = 0; step < 4; step++)
            {
                var text = "+" + (start + offset + step).ToString(System.Globalization.CultureInfo.InvariantCulture);
                try
                {
                    var parsed = util.Parse(text, region);
                    if (!util.IsValidNumber(parsed) || util.GetRegionCodeForNumber(parsed) != region)
                    {
                        allValid = false;
                        break;
                    }

                    numbers.Add(util.Format(parsed, PhoneNumberFormat.E164));
                }
                catch (NumberParseException)
                {
                    allValid = false;
                    break;
                }
            }

            if (allValid)
            {
                return numbers;
            }
        }

        throw new InvalidOperationException("Could not find four consecutive valid numbers for " + region);
    }

    private sealed class FakeDisposableDomains : IDisposableDomainList
    {
        private readonly HashSet<string> _domains;

        public FakeDisposableDomains(IEnumerable<string> domains)
        {
            _domains = new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase);
        }

        public Exception? Failure { get; set; }

        public bool Contains(string domain)
        {
            if (Failure != null)
            {
                throw Failure;
            }

            return _domains.Contains(domain);
        }
    }

    private sealed class RecordingMx : IMailExchangerLookup
    {
        public Dictionary<string, MxLookupResult> Results { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<string> LookedUp { get; } = new();

        public Exception? Failure { get; set; }

        public Task<MxLookupResult> LookupAsync(string domain, CancellationToken cancellationToken = default)
        {
            LookedUp.Add(domain);
            if (Failure != null)
            {
                throw Failure;
            }

            return Task.FromResult(Results.TryGetValue(domain, out var result) ? result : MxLookupResult.HasMx);
        }
    }

    private sealed class RecordingAlerts : IAbuseAlertService
    {
        public List<AbuseElectionFlaggedAlert> Flagged { get; } = new();

        public Task NotifyCapHitAsync(AbuseCapHitAlert alert, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyFirstPaidSendAsync(AbuseFirstPaidSendAlert alert, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyElectionFlaggedAsync(AbuseElectionFlaggedAlert alert, CancellationToken cancellationToken = default)
        {
            Flagged.Add(alert);
            return Task.CompletedTask;
        }

        public Task NotifyPrefixLimitAsync(AbusePrefixLimitAlert alert, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
