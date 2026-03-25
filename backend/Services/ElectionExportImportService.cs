using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Domain.Context;
using Backend.DTOs.Import;
using Backend.DTOs.Elections;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;

namespace Backend.Services;

public class ElectionExportImportService : ElectionImportExportBase
{
    private readonly CdnBallotImportService _cdnBallotImportService;
    private readonly TallyJv2ElectionImportService _tallyJv2ElectionImportService;
    private readonly JsonElectionImportExportService _jsonElectionImportExportService;

    public ElectionExportImportService(
        MainDbContext context,
        IElectionService electionService,
        CdnBallotImportService cdnBallotImportService,
        TallyJv2ElectionImportService tallyJv2ElectionImportService,
        JsonElectionImportExportService jsonElectionImportExportService)
        : base(context, electionService)
    {
        _cdnBallotImportService = cdnBallotImportService;
        _tallyJv2ElectionImportService = tallyJv2ElectionImportService;
        _jsonElectionImportExportService = jsonElectionImportExportService;
    }

    // Job 1: Import from CdnBallotImport.xsd format
    public async Task<ImportResultDto> ImportCdnBallotsAsync(Guid electionGuid, Stream xmlStream)
    {
        return await _cdnBallotImportService.ImportCdnBallotsAsync(electionGuid, xmlStream);
    }

    // Job 2: Import from TallyJv2-Export.xsd format
    public async Task<ElectionDto> ImportTallyJv2ElectionAsync(Stream xmlStream, Guid? userId = null)
    {
        return await _tallyJv2ElectionImportService.ImportTallyJv2ElectionAsync(xmlStream, userId);
    }

    // Job 3: Export election to new JSON format
    public async Task<string> ExportElectionToJsonAsync(Guid electionGuid)
    {
        return await _jsonElectionImportExportService.ExportElectionToJsonAsync(electionGuid);
    }

    // Job 3: Import from new JSON format
    public async Task<ElectionDto> ImportElectionFromJsonAsync(Stream jsonStream, Guid? userId = null)
    {
        return await _jsonElectionImportExportService.ImportElectionFromJsonAsync(jsonStream, userId);
    }
}