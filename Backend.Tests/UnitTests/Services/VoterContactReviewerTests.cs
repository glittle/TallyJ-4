using Backend.Configuration;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        AddPerson("bad-1", null);
        AddPerson("bad-2", null);
        AddPerson("bad-3", null);
        await Context.SaveChangesAsync();
        await CreateReviewer(threshold: 3).ReviewElectionAsync(_electionId);
        Assert.False(Context.ElectionSendControls.SingleOrDefault()?.Flagged ?? false);

        AddPerson("bad-4", null);
        await Context.SaveChangesAsync();
        await CreateReviewer(threshold: 3).ReviewElectionAsync(_electionId);

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
    public async Task ReviewFailure_FlagsTheElection_AndKeepsThePeople()
    {
        AddPerson(null, "a@boom.example");
        await Context.SaveChangesAsync();
        _mx.Failure = new InvalidOperationException("dns client failed");

        var thrown = await Record.ExceptionAsync(() => CreateReviewer(threshold: 10).ReviewElectionAsync(_electionId));

        Assert.Null(thrown);
        Assert.Single(Context.People);
        var control = Context.ElectionSendControls.Single();
        Assert.True(control.Flagged);
        Assert.Contains(Context.VoterContactFlags, row => row.Active && row.Reason == VoterContactFlagReason.ReviewFailed);
        Assert.Single(_alerts.Flagged);
        Assert.Contains(_alerts.Flagged[0].Rows, row => row.Reason == VoterContactFlagReason.ReviewFailed);
    }

    private void AddPerson(string? phone, string? email)
    {
        Context.People.Add(new Person
        {
            ElectionGuid = _electionId,
            PersonGuid = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            Phone = phone,
            Email = email,
            RowVersion = new byte[8]
        });
    }

    private VoterContactReviewer CreateReviewer(int threshold, int runLength = 4)
    {
        return new VoterContactReviewer(
            Context,
            new FakeDisposableDomains(new[] { "mailinator.com" }),
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

    private static IReadOnlyList<string> FourConsecutiveCanadianNumbers()
    {
        var util = PhoneNumberUtil.GetInstance();
        var example = util.Format(util.GetExampleNumber("CA"), PhoneNumberFormat.E164);
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
                    var parsed = util.Parse(text, "CA");
                    if (!util.IsValidNumber(parsed) || util.GetRegionCodeForNumber(parsed) != "CA")
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

        throw new InvalidOperationException("Could not find four consecutive valid Canadian numbers");
    }

    private sealed class FakeDisposableDomains : IDisposableDomainList
    {
        private readonly HashSet<string> _domains;

        public FakeDisposableDomains(IEnumerable<string> domains)
        {
            _domains = new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase);
        }

        public bool Contains(string domain) => _domains.Contains(domain);
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
    }
}
