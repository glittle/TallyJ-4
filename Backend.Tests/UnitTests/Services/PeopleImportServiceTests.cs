using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Entities;
using Backend.DTOs.Import;
using Backend.Localization;
using Backend.Services;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.UnitTests.Services;

public class PeopleImportServiceTests : ServiceTestBase
{
    private readonly PeopleImportService _service;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Mock<ILogger<PeopleImportService>> _loggerMock;
    private readonly Mock<IJsonLocalizationProvider> _localizationMock;

    public PeopleImportServiceTests()
    {
        _signalRMock = new Mock<ISignalRNotificationService>();
        _loggerMock = new Mock<ILogger<PeopleImportService>>();
        _localizationMock = new Mock<IJsonLocalizationProvider>();
        _service = new PeopleImportService(Context, _signalRMock.Object, _localizationMock.Object);
    }

    [Fact]
    public async Task UploadFileAsync_ValidCsvFile_ReturnsImportFileDto()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName\nJohn,Doe\nJane,Smith";
        var file = CreateFormFile("test.csv", "text/csv", fileContent);

        // Act
        var result = await _service.UploadFileAsync(electionGuid, file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(electionGuid, result.ElectionGuid);
        Assert.Equal("test.csv", result.OriginalFileName);
        Assert.Equal("csv", result.FileType);
        Assert.Equal("Uploaded", result.ProcessingStatus);
        Assert.True(result.HasContent);
        Assert.Equal(65001, result.CodePage); // UTF-8 default
        Assert.Equal(1, result.FirstDataRow);
    }

    [Fact]
    public async Task UploadFileAsync_ValidXlsxFile_ReturnsImportFileDto()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var file = CreateFormFile("test.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "fake xlsx content");

        // Act
        var result = await _service.UploadFileAsync(electionGuid, file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("xlsx", result.FileType);
        Assert.Null(result.CodePage); // No code page for XLSX
    }

    [Fact]
    public async Task UploadFileAsync_InvalidFileExtension_ThrowsArgumentException()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var file = CreateFormFile("test.pdf", "application/pdf", "content");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadFileAsync(electionGuid, file));
        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public async Task UploadFileAsync_FileTooLarge_ThrowsArgumentException()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var largeContent = new string('x', 11 * 1024 * 1024); // 11MB
        var file = CreateFormFile("large.csv", "text/csv", largeContent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UploadFileAsync(electionGuid, file));
        Assert.Contains("exceeds the maximum", exception.Message);
    }

    [Fact]
    public async Task ParseFileAsync_CsvFile_ReturnsParseResponse()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var fileContent = "First Name,Last Name,Bahai ID\nJohn,Doe,123\nJane,Smith,456";
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Headers.Count);
        Assert.Contains("First Name", result.Headers);
        Assert.Contains("Last Name", result.Headers);
        Assert.Contains("Bahai ID", result.Headers);
        Assert.Equal(2, result.TotalDataRows);
        Assert.Equal(2, result.PreviewRows.Count);
        Assert.NotEmpty(result.AutoMappings);
    }

    [Theory]
    [InlineData("Baha'i ID")]
    [InlineData("Bahá'í ID")]
    [InlineData("Baha’i ID")]
    [InlineData("Bahai_ID")]
    public async Task ParseFileAsync_BahaiIdHeaderVariants_AutoMapsToBahaiId(string header)
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = $"First Name,Last Name,{header}\nMinnie,Mouse,T-124";
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        var mapping = result.AutoMappings.Single(m => m.FileColumn == header);
        Assert.Equal("BahaiId", mapping.TargetField);
    }

    [Fact]
    public async Task ParseFileAsync_TwoHeadersMatchSameField_KeepsMoreSpecificMapping()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "First Name,Last Name,Baha'i ID,ID\nMinnie,Mouse,T-124,99";
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        Assert.Equal("BahaiId", result.AutoMappings.Single(m => m.FileColumn == "Baha'i ID").TargetField);
        Assert.Null(result.AutoMappings.Single(m => m.FileColumn == "ID").TargetField);
        Assert.Equal(1, result.AutoMappings.Count(m => m.TargetField == "BahaiId"));
    }

    [Fact]
    public async Task UpdateFileSettingsAsync_PersistsFirstDataRow()
    {
        var electionGuid = Guid.NewGuid();
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            FirstDataRow = 1,
            CodePage = 65001,
            HasContent = true,
            Contents = Encoding.UTF8.GetBytes("skip,this\nFirst Name,Last Name\nMinnie,Mouse")
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var updated = await _service.UpdateFileSettingsAsync(
            electionGuid,
            importFile.RowId,
            new UpdateFileSettingsDto { FirstDataRow = 2, CodePage = 65001 });

        Assert.Equal(2, updated.FirstDataRow);
        var stored = await Context.ImportFiles.FindAsync(importFile.RowId);
        Assert.Equal(2, stored!.FirstDataRow);
    }

    [Fact]
    public async Task ParseFileAsync_CsvHeadersOnLine2_UsesSecondLineAsHeaders()
    {
        var electionGuid = Guid.NewGuid();
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 2,
            HasContent = true,
            Contents = Encoding.UTF8.GetBytes("Report Title,\nFirst Name,Last Name\nMinnie,Mouse")
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        Assert.Equal("First Name", result.Headers[0]);
        Assert.Equal("Last Name", result.Headers[1]);
        Assert.Equal("Minnie", result.PreviewRows[0][0]);
    }

    [Fact]
    public async Task ParseFileAsync_SparseColumns_CollectsSamplesFromLaterRows()
    {
        // Arrange — like 2021-04-22-with units.csv: some columns are empty on the first rows
        var electionGuid = Guid.NewGuid();
        var fileContent =
            "LastName,FirstName,MiddleName,FormerName,Nickname\n" +
            "Abdai,Manouchehr,,,\n" +
            "Adegbesan,Kehinde,,,\n" +
            "Afshar,Bejan,,,\n" +
            "Afshar,Nima,,,\n" +
            "Agahi,Ghazaleh,,Ghazaleh Agahi Najafabadi,Nila Jessa\n" +
            "Agahi,Heshmatollah,,Hesmatollah,\n" +
            "Agbor,Sallyanne,M,Ghorbanpoor,\n";
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.Equal(7, result.TotalDataRows);
        Assert.True(result.PreviewRows.Count <= 3);
        Assert.Equal("Manouchehr", result.PreviewRows[0][1]);
        Assert.Equal("Kehinde", result.PreviewRows[1][1]);
        Assert.Equal("Bejan", result.PreviewRows[2][1]);
        Assert.Equal("M", result.PreviewRows[0][2]);
        Assert.Equal("Ghazaleh Agahi Najafabadi", result.PreviewRows[0][3]);
        Assert.Equal("Hesmatollah", result.PreviewRows[1][3]);
        Assert.Equal("Ghorbanpoor", result.PreviewRows[2][3]);
        Assert.Equal("Nila Jessa", result.PreviewRows[0][4]);
    }

    [Fact]
    public async Task ParseFileAsync_CsvFile_CountsAllRowsAfterPreviewSamplesAreFull()
    {
        var electionGuid = Guid.NewGuid();
        var rows = new StringBuilder("First Name,Last Name\n");
        for (var i = 1; i <= 20; i++)
        {
            rows.AppendLine($"Person{i},Last{i}");
            if (i == 10)
            {
                rows.AppendLine();
            }
        }

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(rows.ToString()),
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        Assert.Equal(20, result.TotalDataRows);
        Assert.Equal(3, result.PreviewRows.Count);
        Assert.Equal("Person1", result.PreviewRows[0][0]);
        Assert.Equal("Person3", result.PreviewRows[2][0]);
    }

    [Fact]
    public async Task ParseFileAsync_XlsxFile_CountsAllRowsAfterPreviewSamplesAreFull()
    {
        var electionGuid = Guid.NewGuid();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        worksheet.Cell(1, 1).Value = "FirstName";
        worksheet.Cell(1, 2).Value = "LastName";
        for (var i = 1; i <= 15; i++)
        {
            worksheet.Cell(i + 1, 1).Value = $"Person{i}";
            worksheet.Cell(i + 1, 2).Value = $"Last{i}";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "xlsx",
            Contents = stream.ToArray(),
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        Assert.Equal(15, result.TotalDataRows);
        Assert.Equal(3, result.PreviewRows.Count);
        Assert.Equal("Person1", result.PreviewRows[0][0]);
        Assert.Equal("Person3", result.PreviewRows[2][0]);
    }

    [Fact]
    public async Task ParseFileAsync_XlsxFile_ReturnsParseResponse()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        // Create a simple XLSX file using ClosedXML
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        worksheet.Cell(1, 1).Value = "FirstName";
        worksheet.Cell(1, 2).Value = "LastName";
        worksheet.Cell(2, 1).Value = "John";
        worksheet.Cell(2, 2).Value = "Doe";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileContent = stream.ToArray();

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "xlsx",
            Contents = fileContent,
            HasContent = true
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Headers.Count);
        Assert.Contains("FirstName", result.Headers);
        Assert.Contains("LastName", result.Headers);
        Assert.Equal(1, result.TotalDataRows);
    }

    [Fact]
    public async Task ParseFileAsync_XlsxFileWithHeadersOnRow5_AutoDetectsHeaders()
    {
        // Arrange - Simulates Canadian XLSX files where headers start at row 5
        var electionGuid = Guid.NewGuid();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // Add some metadata/description rows at the top (like Canadian files)
        worksheet.Cell(1, 1).Value = "Government of Canada";
        worksheet.Cell(2, 1).Value = "Voter Registration List";
        worksheet.Cell(3, 1).Value = "Election Date: 2024-01-01";
        worksheet.Cell(4, 1).Value = ""; // Empty row

        // Headers on row 5
        worksheet.Cell(5, 1).Value = "First Name";
        worksheet.Cell(5, 2).Value = "Last Name";
        worksheet.Cell(5, 3).Value = "Email";

        // Data rows
        worksheet.Cell(6, 1).Value = "John";
        worksheet.Cell(6, 2).Value = "Doe";
        worksheet.Cell(6, 3).Value = "john@example.com";
        worksheet.Cell(7, 1).Value = "Jane";
        worksheet.Cell(7, 2).Value = "Smith";
        worksheet.Cell(7, 3).Value = "jane@example.com";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileContent = stream.ToArray();

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "xlsx",
            Contents = fileContent,
            HasContent = true,
            FirstDataRow = null // Let it auto-detect
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Headers.Count);
        Assert.Contains("First Name", result.Headers);
        Assert.Contains("Last Name", result.Headers);
        Assert.Contains("Email", result.Headers);
        Assert.Equal(2, result.TotalDataRows); // Should only have 2 data rows after header row 5
        Assert.Equal(2, result.PreviewRows.Count);

        // Verify data starts after header row
        Assert.Equal("John", result.PreviewRows[0][0]);
        Assert.Equal("Doe", result.PreviewRows[0][1]);
    }

    [Fact]
    public async Task UploadFileAsync_XlsxWithHeadersOnRow6_DetectsCorrectHeaderRow()
    {
        // Arrange - Test upload auto-detection
        var electionGuid = Guid.NewGuid();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        // Add metadata rows
        worksheet.Cell(1, 1).Value = "Report Title";
        worksheet.Cell(2, 1).Value = "Generated: 2024-01-01";
        worksheet.Cell(3, 1).Value = 12345; // Numeric value
        worksheet.Cell(4, 1).Value = ""; // Empty row
        worksheet.Cell(5, 1).Value = "Summary info";

        // Headers on row 6 with known field names
        worksheet.Cell(6, 1).Value = "FirstName";
        worksheet.Cell(6, 2).Value = "LastName";
        worksheet.Cell(6, 3).Value = "BahaiId";

        // Data
        worksheet.Cell(7, 1).Value = "Alice";
        worksheet.Cell(7, 2).Value = "Johnson";
        worksheet.Cell(7, 3).Value = "789";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileContent = stream.ToArray();

        var file = new FormFile(new MemoryStream(fileContent), 0, fileContent.Length, "file", "test.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        // Act
        var result = await _service.UploadFileAsync(electionGuid, file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.FirstDataRow); // Should auto-detect row 6 as header row
        Assert.Equal("xlsx", result.FileType);
    }

    [Fact]
    public async Task ParseFileAsync_XlsxWithManualFirstDataRow_UsesProvidedRow()
    {
        // Arrange - Test that manual override works
        var electionGuid = Guid.NewGuid();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "Skip this";
        worksheet.Cell(2, 1).Value = "Skip this too";
        worksheet.Cell(3, 1).Value = "FirstName";
        worksheet.Cell(3, 2).Value = "LastName";
        worksheet.Cell(4, 1).Value = "Bob";
        worksheet.Cell(4, 2).Value = "Brown";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileContent = stream.ToArray();

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "xlsx",
            Contents = fileContent,
            HasContent = true,
            FirstDataRow = 3 // Manually specify row 3
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ParseFileAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Headers.Count);
        Assert.Equal("FirstName", result.Headers[0]);
        Assert.Equal("LastName", result.Headers[1]);
        Assert.Equal(1, result.TotalDataRows);
        Assert.Equal("Bob", result.PreviewRows[0][0]);
    }

    [Fact]
    public async Task SaveColumnMappingsAsync_ValidMappings_SavesToDatabase()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            ProcessingStatus = "Uploaded"
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "First Name", TargetField = "FirstName" },
            new() { FileColumn = "Last Name", TargetField = "LastName" },
            new() { FileColumn = "ID", TargetField = null }
        };

        // Act
        await _service.SaveColumnMappingsAsync(electionGuid, importFile.RowId, mappings);

        // Assert
        var updatedFile = await Context.ImportFiles.FindAsync(importFile.RowId);
        Assert.NotNull(updatedFile);
        Assert.Equal("Mapped", updatedFile.ProcessingStatus);
        Assert.NotNull(updatedFile.ColumnsToRead);
    }

    [Fact]
    public async Task ImportPeopleAsync_ValidData_ImportsSuccessfully()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName\nJohn,Doe\nJane,Smith";
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" }
        };

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.PeopleAdded);
        Assert.Equal(0, result.PeopleSkipped);

        var people = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>();
        Assert.Equal(2, people.Count);
        Assert.Contains(people, p => p.FirstName == "John" && p.LastName == "Doe");
        Assert.Contains(people, p => p.FirstName == "Jane" && p.LastName == "Smith");

        // Import complete + front desk soft-refresh after bulk people import.
        _signalRMock.Verify(
            s => s.SendPeopleImportCompleteAsync(electionGuid, It.IsAny<object>()),
            Times.Once);
        _signalRMock.Verify(
            s => s.RequestFrontDeskReloadAsync(electionGuid),
            Times.Once);
    }

    [Fact]
    public async Task ImportPeopleAsync_DuplicateName_ImportsBoth()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Other Info\nJohn,Doe,North\nJohn,Doe,South";
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" },
            new() { FileColumn = "Other Info", TargetField = "OtherInfo" }
        };

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        Assert.Equal(2, result.PeopleAdded);
        Assert.Equal(0, result.PeopleSkipped);
        Assert.Empty(result.Errors);
        var people = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>();
        Assert.Equal(2, people.Count);
    }

    [Fact]
    public async Task ImportPeopleAsync_DuplicateBahaiId_SkipsAndReportsMatchedLine()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Bahai ID\nJohn,Doe,123\nJane,Smith,123";
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" },
            new() { FileColumn = "Bahai ID", TargetField = "BahaiId" }
        };

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        Assert.Equal(1, result.PeopleAdded);
        Assert.Equal(1, result.PeopleSkipped);
        var duplicate = Assert.Single(result.Errors);
        Assert.Equal("import.errors.duplicateBahaiId", duplicate.Key);
        Assert.Equal("3", duplicate.Parameters["rowNumber"]);
        Assert.Equal("2", duplicate.Parameters["matchedRowNumber"]);
    }

    [Fact]
    public async Task ImportPeopleAsync_DuplicatePhone_SkipsAndReportsMatchedLine()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Phone\nJohn,Doe,403-809-1573\nJane,Smith,403-809-1573";
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" },
            new() { FileColumn = "Phone", TargetField = "Phone" }
        };

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        Assert.Equal(1, result.PeopleAdded);
        Assert.Equal(1, result.PeopleSkipped);
        var duplicate = Assert.Single(result.Errors);
        Assert.Equal("import.errors.duplicatePhone", duplicate.Key);
        Assert.Equal("3", duplicate.Parameters["rowNumber"]);
        Assert.Equal("2", duplicate.Parameters["matchedRowNumber"]);
    }

    [Fact]
    public async Task ImportPeopleAsync_XlsxHeadersOnRow6_ReportsExcelRowNumbers()
    {
        var electionGuid = Guid.NewGuid();
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        worksheet.Cell(6, 1).Value = "FirstName";
        worksheet.Cell(6, 2).Value = "LastName";
        worksheet.Cell(6, 3).Value = "Phone";
        worksheet.Cell(7, 1).Value = "John";
        worksheet.Cell(7, 2).Value = "Doe";
        worksheet.Cell(7, 3).Value = "403-809-1573";
        worksheet.Cell(12, 1).Value = "Jane";
        worksheet.Cell(12, 2).Value = "Smith";
        worksheet.Cell(12, 3).Value = "403-809-1573";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" },
            new() { FileColumn = "Phone", TargetField = "Phone" }
        };
        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "xlsx",
            FirstDataRow = 6,
            Contents = stream.ToArray(),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        Assert.Equal(1, result.PeopleAdded);
        Assert.Equal(1, result.PeopleSkipped);
        var duplicate = Assert.Single(result.Errors);
        Assert.Equal("12", duplicate.Parameters["rowNumber"]);
        Assert.Equal("7", duplicate.Parameters["matchedRowNumber"]);
    }

    [Fact]
    public async Task ImportPeopleAsync_IneligibleReasonByDescription_SetsEligibility()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Eligibility\nJohn,Doe,Deceased\nJane,Smith,";
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" },
            new() { FileColumn = "Eligibility", TargetField = "IneligibleReasonDescription" }
        };

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.True(result.Success);
        var people = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>();
        Assert.Equal(2, people.Count);

        var john = people.First(p => p.FirstName == "John");
        Assert.False(john.CanVote);
        Assert.False(john.CanReceiveVotes);
        Assert.Equal(IneligibleReasonEnum.X01_Deceased.Code, john.IneligibleReasonCode);

        var jane = people.First(p => p.FirstName == "Jane");
        Assert.True(jane.CanVote);
        Assert.True(jane.CanReceiveVotes);
        Assert.Null(jane.IneligibleReasonCode);
    }

    [Fact]
    public async Task ImportPeopleAsync_IneligibleReasonByCode_SetsEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Eligibility\nJohn,Doe,V04";
        var importFile = await AddMappedEligibilityFile(electionGuid, fileContent);

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        var john = Assert.Single(await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>());
        Assert.True(john.CanVote);
        Assert.False(john.CanReceiveVotes);
        Assert.Equal(IneligibleReasonEnum.V04_RightsRemovedCannotBeVotedFor.Code, john.IneligibleReasonCode);
    }

    [Fact]
    public async Task ImportPeopleAsync_EligibleText_SetsFullEligibility()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Eligibility\nJohn,Doe,Eligible";
        var importFile = await AddMappedEligibilityFile(electionGuid, fileContent);

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        var john = Assert.Single(await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>());
        Assert.True(john.CanVote);
        Assert.True(john.CanReceiveVotes);
        Assert.Null(john.IneligibleReasonCode);
    }

    [Fact]
    public async Task ImportPeopleAsync_LocalizedEligibilityText_SetsEligibility()
    {
        _localizationMock
            .Setup(p => p.GetString("eligibility.X01", It.IsAny<CultureInfo>()))
            .Returns("Décédé");

        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Eligibility\nJohn,Doe,Décédé";
        var importFile = await AddMappedEligibilityFile(electionGuid, fileContent);

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        var john = Assert.Single(await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>());
        Assert.False(john.CanVote);
        Assert.False(john.CanReceiveVotes);
        Assert.Equal(IneligibleReasonEnum.X01_Deceased.Code, john.IneligibleReasonCode);
    }

    [Fact]
    public async Task ImportPeopleAsync_UnrecognizedEligibility_SkipsRow()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Eligibility\nJohn,Doe,Not a real status\nJane,Smith,";
        var importFile = await AddMappedEligibilityFile(electionGuid, fileContent);

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        Assert.Equal(1, result.PeopleAdded);
        Assert.Equal(1, result.PeopleSkipped);
        var error = Assert.Single(result.Errors);
        Assert.Equal("import.errors.unrecognizedEligibility", error.Key);
        Assert.Equal("2", error.Parameters["rowNumber"]);
        Assert.Equal("Not a real status", error.Parameters["eligibilityValue"]);

        var jane = Assert.Single(await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>());
        Assert.Equal("Jane", jane.FirstName);
    }

    [Fact]
    public async Task ImportPeopleAsync_InternalOnlyEligibilityCode_SkipsRow()
    {
        var electionGuid = Guid.NewGuid();
        var fileContent = "FirstName,LastName,Eligibility\nJohn,Doe,U01";
        var importFile = await AddMappedEligibilityFile(electionGuid, fileContent);

        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        Assert.True(result.Success);
        Assert.Equal(0, result.PeopleAdded);
        Assert.Equal(1, result.PeopleSkipped);
        Assert.Empty(await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>());
        Assert.Equal("import.errors.unrecognizedEligibility", Assert.Single(result.Errors).Key);
    }

    private async Task<ImportFile> AddMappedEligibilityFile(Guid electionGuid, string fileContent)
    {
        var mappings = new List<ColumnMappingDto>
        {
            new() { FileColumn = "FirstName", TargetField = "FirstName" },
            new() { FileColumn = "LastName", TargetField = "LastName" },
            new() { FileColumn = "Eligibility", TargetField = "IneligibleReasonDescription" }
        };

        var importFile = new ImportFile
        {
            ElectionGuid = electionGuid,
            FileType = "csv",
            CodePage = 65001,
            FirstDataRow = 1,
            Contents = Encoding.UTF8.GetBytes(fileContent),
            HasContent = true,
            ColumnsToRead = System.Text.Json.JsonSerializer.Serialize(mappings)
        };
        Context.ImportFiles.Add(importFile);
        await Context.SaveChangesAsync();
        return importFile;
    }

    [Fact]
    public async Task DeleteAllPeopleAsync_NoBallots_DeletesSuccessfully()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var people = new[]
        {
            new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = electionGuid, FirstName = "John", LastName = "Doe", RowVersion = new byte[8] },
            new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = electionGuid, FirstName = "Jane", LastName = "Smith", RowVersion = new byte[8] }
        };
        Context.People.AddRange(people);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAllPeopleAsync(electionGuid);

        // Assert
        Assert.Equal(2, result.DeletedCount);
        var remainingPeople = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync<Person>();
        Assert.Empty(remainingPeople);
        _signalRMock.Verify(
            s => s.RequestFrontDeskReloadAsync(electionGuid),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAllPeopleAsync_HasBallots_ThrowsException()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var person = new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = electionGuid, FirstName = "John", LastName = "Doe", RowVersion = new byte[8] };
        var locationGuid = Guid.NewGuid();
        var location = new Location { LocationGuid = locationGuid, ElectionGuid = electionGuid, Name = "Test Location" };
        var ballot = new Ballot { BallotGuid = Guid.NewGuid(), LocationGuid = locationGuid, StatusCode = BallotStatus.Ok, ComputerCode = "T1" };

        Context.People.Add(person);
        Context.Locations.Add(location);
        Context.Ballots.Add(ballot);
        await Context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAllPeopleAsync(electionGuid));
        Assert.Contains("ballots exist", exception.Message);
    }

    [Fact]
    public async Task DeleteAllPeopleAsync_HasRegisteredPeople_ThrowsException()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var person = new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = electionGuid,
            FirstName = "John",
            LastName = "Doe",
            RegistrationTime = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
        Context.People.Add(person);
        await Context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAllPeopleAsync(electionGuid));
        Assert.Contains("voting status set", exception.Message);
    }

    [Fact]
    public async Task GetPeopleCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var people = new[]
        {
            new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = electionGuid, FirstName = "John", LastName = "Doe", RowVersion = new byte[8] },
            new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = electionGuid, FirstName = "Jane", LastName = "Smith", RowVersion = new byte[8] }
        };
        Context.People.AddRange(people);
        await Context.SaveChangesAsync();

        // Act
        var count = await _service.GetPeopleCountAsync(electionGuid);

        // Assert
        Assert.Equal(2, count);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}