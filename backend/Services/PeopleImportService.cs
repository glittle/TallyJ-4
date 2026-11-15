using System;
using System.Collections.Generic;
using Backend.Context;
using Backend.Localization;

namespace Backend.Services;

/// <summary>
/// CSV/TSV/XLSX people import. Upload stores the file, parse returns up to
/// three non-empty samples per column, and load reads the saved column mapping.
/// Email, phone, and Bahá'í ID must be unique; duplicate names are kept.
/// A validation skip that increments PeopleSkipped does not roll the
/// transaction back. A null person that did not increment PeopleSkipped, a
/// non-transient row exception, or a failed batch save does, including batches
/// already saved in that transaction.
/// </summary>
public partial class PeopleImportService : IPeopleImportService
{
    private readonly MainDbContext _context;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IJsonLocalizationProvider _localizationProvider;
    private readonly IVoterContactReviewer? _voterContactReviewer;

    // Scoring weights for header detection
    private const int TextCellScore = 2;
    private const int KnownFieldScore = 10;
    private const int HeaderKeywordScore = 5;

    // Auto-mapping aliases (compared after stripping punctuation, spaces, and accents)
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> FieldAliases = new Dictionary<string, IReadOnlyList<string>>
    {
        ["FirstName"] = new[] { "first name", "firstname", "first_name", "given name", "givenname" },
        ["LastName"] = new[] { "last name", "lastname", "last_name", "surname", "family name", "familyname" },
        ["BahaiId"] = new[]
        {
            "bahai id",
            "bahaiid",
            "bahai_id",
            "baha'i id",
            "bahá'í id",
            "baha’i id",
            "membership id",
            "id",
        },
        ["IneligibleReasonDescription"] = new[] { "eligibility", "eligibility status", "status", "ineligible reason" },
        ["Area"] = new[] { "area", "region", "locality", "community" },
        ["Email"] = new[] { "email", "email address", "e-mail" },
        ["Phone"] = new[] { "phone", "phone number", "telephone", "tel", "mobile" },
        ["OtherNames"] = new[] { "other names", "othernames", "other_names", "middle name", "middlename" },
        ["OtherLastNames"] = new[] { "other last names", "otherlastnames", "maiden name", "former name", "formername" },
        ["OtherInfo"] = new[] { "other info", "otherinfo", "other_info", "notes", "comments" }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleImportService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="signalRNotificationService">Import progress and FrontDesk reload broadcasts.</param>
    /// <param name="localizationProvider">Resolves eligibility descriptions in the current UI language.</param>
    public PeopleImportService(
        MainDbContext context,
        ISignalRNotificationService signalRNotificationService,
        IJsonLocalizationProvider localizationProvider,
        IVoterContactReviewer? voterContactReviewer = null)
    {
        _context = context;
        _signalRNotificationService = signalRNotificationService;
        _localizationProvider = localizationProvider;
        _voterContactReviewer = voterContactReviewer;
    }
}
