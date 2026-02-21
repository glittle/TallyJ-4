using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Backend.Domain.Entities;
using Backend.DTOs.Import;
using Backend.Services;
using Backend.Domain.Enumerations;

namespace Backend.Tests.UnitTests.Services;

public class PeopleImportServiceTests : ServiceTestBase
{
    private readonly PeopleImportService _service;
    private readonly Mock<IHubContext<PeopleImportHub>> _hubContextMock;
    private readonly Mock<ILogger<PeopleImportService>> _loggerMock;

    public PeopleImportServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<PeopleImportHub>>();
        _loggerMock = new Mock<ILogger<PeopleImportService>>();
        _service = new PeopleImportService(Context, _hubContextMock.Object, _loggerMock.Object);
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
        var file = CreateFormFile("test.txt", "text/plain", "content");

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

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        // Act
        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.PeopleAdded);
        Assert.Equal(0, result.PeopleSkipped);

        var people = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync();
        Assert.Equal(2, people.Count);
        Assert.Contains(people, p => p.FirstName == "John" && p.LastName == "Doe");
        Assert.Contains(people, p => p.FirstName == "Jane" && p.LastName == "Smith");
    }

    [Fact]
    public async Task ImportPeopleAsync_DuplicateByBahaiId_SkipsDuplicate()
    {
        // Arrange
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

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        // Act
        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.PeopleAdded);
        Assert.Equal(1, result.PeopleSkipped);

        var people = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync();
        Assert.Single(people);
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

        // Setup SignalR mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        // Act
        var result = await _service.ImportPeopleAsync(electionGuid, importFile.RowId);

        // Assert
        Assert.True(result.Success);
        var people = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync();
        Assert.Equal(2, people.Count);

        var john = people.First(p => p.FirstName == "John");
        Assert.False(john.CanVote);
        Assert.False(john.CanReceiveVotes);
        Assert.Equal(IneligibleReasonEnum.X01_Deceased.ReasonGuid, john.IneligibleReasonGuid);

        var jane = people.First(p => p.FirstName == "Jane");
        Assert.True(jane.CanVote);
        Assert.True(jane.CanReceiveVotes);
        Assert.Null(jane.IneligibleReasonGuid);
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
        var remainingPeople = await Context.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync();
        Assert.Empty(remainingPeople);
    }

    [Fact]
    public async Task DeleteAllPeopleAsync_HasBallots_ThrowsException()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();
        var person = new Person { PersonGuid = Guid.NewGuid(), ElectionGuid = electionGuid, FirstName = "John", LastName = "Doe", RowVersion = new byte[8] };
        var ballot = new Ballot { BallotGuid = Guid.NewGuid(), ElectionGuid = electionGuid, LocationGuid = Guid.NewGuid(), BallotCode = "001", StatusCode = "Ok" };

        Context.People.Add(person);
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