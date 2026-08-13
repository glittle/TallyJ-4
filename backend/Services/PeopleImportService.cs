using System;
using System.Collections.Generic;
using Backend.Context;

namespace Backend.Services;

/// <summary>
/// Service for managing people import operations including file upload, parsing, mapping, and import execution.
/// Implementation is split across partial files by concern.
/// </summary>
public partial class PeopleImportService : IPeopleImportService
{
    private readonly MainDbContext _context;
    private readonly ISignalRNotificationService _signalRNotificationService;

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
    public PeopleImportService(
        MainDbContext context,
        ISignalRNotificationService signalRNotificationService)
    {
        _context = context;
        _signalRNotificationService = signalRNotificationService;
    }
}
