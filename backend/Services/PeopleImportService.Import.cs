using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Import;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class PeopleImportService
{
    /// <summary>
    /// Executes the import of people from the configured file.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="rowId">The row ID of the import file.</param>
    /// <returns>The result of the import operation.</returns>
    public async Task<ImportPeopleResult> ImportPeopleAsync(Guid electionGuid, int rowId)
    {
        var result = new ImportPeopleResult();
        var startTime = DateTimeOffset.UtcNow;

        var importFile = await _context.ImportFiles
            .FirstOrDefaultAsync(f => f.ElectionGuid == electionGuid && f.RowId == rowId);

        if (importFile == null || importFile.HasContent != true || importFile.Contents == null)
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.fileNotFound",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        // Deserialize column mappings
        if (string.IsNullOrEmpty(importFile.ColumnsToRead))
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.noMappings",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        var mappings = JsonSerializer.Deserialize<List<ColumnMappingDto>>(importFile.ColumnsToRead);
        if (mappings == null || mappings.Count == 0)
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.invalidMappings",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        // Validate required mappings
        var firstNameMapping = mappings.FirstOrDefault(m => m.TargetField == "FirstName");
        var lastNameMapping = mappings.FirstOrDefault(m => m.TargetField == "LastName");

        if (firstNameMapping == null || lastNameMapping == null)
        {
            result.Success = false;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.missingRequiredMappings",
                Parameters = new Dictionary<string, string>()
            });
            return result;
        }

        // Load existing people for deduplication
        var existingPeople = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .ToListAsync();

        var bahaiIdLookup = existingPeople
            .Where(p => !string.IsNullOrEmpty(p.BahaiId))
            .Select(p => p.BahaiId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // for voter login, the email and phone must be unique
        var emailLookup = existingPeople
        .Where(p => !string.IsNullOrEmpty(p.Email))
        .Select(p => p.Email!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var phoneLookup = existingPeople
        .Where(p => !string.IsNullOrEmpty(p.Phone))
        .Select(p => p.Phone!)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nameLookup = existingPeople
            .Select(p => $"{p.FirstName ?? ""} {p.LastName}".Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        using var transaction = await _context.Database.BeginTransactionAsync();

        var rowNumber = 0; // Initialize row number for error reporting

        try
        {
            // Parse file content
            var (headers, allRows, _) = await ParseFileContentAsync(importFile);
            var firstRowNum = importFile!.FirstDataRow ?? 0;
            var dataRows = allRows.Skip(firstRowNum - 1).ToList();

            result.TotalRows = dataRows.Count;

            // Process in batches
            const int batchSize = 100;
            var peopleToAdd = new List<Person>();
            var errorsFound = false;

            for (int i = 0; i < dataRows.Count; i++)
            {
                var row = dataRows[i];
                rowNumber = i + firstRowNum + 1; // Calculate actual row number in the file for error reporting

                await ReportProgress(electionGuid, i + 1, dataRows.Count, $"Processing row {rowNumber}");

                try
                {
                    var skippedBefore = result.PeopleSkipped;
                    var person = CreatePersonFromRow(row, headers, mappings, electionGuid, bahaiIdLookup, emailLookup, phoneLookup, nameLookup, rowNumber, result);
                    if (person != null)
                    {
                        peopleToAdd.Add(person);
                    }
                    else if (result.PeopleSkipped == skippedBefore)
                    {
                        errorsFound = true;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportErrorDto
                    {
                        Key = "import.errors.lineError",
                        Parameters = new Dictionary<string, string>
                        {
                            ["rowNumber"] = rowNumber.ToString(),
                            ["message"] = ex.Message
                        }
                    });
                    result.PeopleSkipped++;
                    errorsFound = true;
                    continue;
                }

                // Save batch
                if (!errorsFound && (peopleToAdd.Count >= batchSize || i == dataRows.Count - 1))
                {
                    _context.People.AddRange(peopleToAdd);
                    try
                    {
                        await _context.SaveChangesAsync();
                        result.PeopleAdded += peopleToAdd.Count;
                        peopleToAdd.Clear();
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new ImportErrorDto
                        {
                            Key = "import.errors.batchSaveFailed",
                            Parameters = new Dictionary<string, string>
                            {
                                ["message"] = ex.InnerException?.Message ?? ex.Message
                            }
                        });
                        errorsFound = true;
                    }
                }
            }

            if (!errorsFound)
            {
                // If no errors, update status to Imported
                importFile.ProcessingStatus = "Imported";
                importFile.ImportTime = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                result.Success = true;
                result.TimeElapsedSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

                await _signalRNotificationService.SendPeopleImportCompleteAsync(electionGuid, result);

                // Open front desk / people / ballot-entry sessions re-fetch lists
                // (parity with single-person PersonAdded/Updated from People Management).
                await _signalRNotificationService.RequestFrontDeskReloadAsync(electionGuid);
            }
            else
            {
                // If errors found, rollback the transaction
                await transaction.RollbackAsync();
            }
        }
        catch (Exception ex)
        {
            // Rollback transaction on any exception
            await transaction.RollbackAsync();

            result.Success = false;
            if (rowNumber > 0)
            {
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.importFailedAtLine",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["message"] = ex.Message
                    }
                });
            }
            else
            {
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.importFailed",
                    Parameters = new Dictionary<string, string>
                    {
                        ["message"] = ex.Message
                    }
                });
            }
            await _signalRNotificationService.SendPeopleImportErrorAsync(electionGuid, ex.Message);
        }

        return result;
    }

    private Person? CreatePersonFromRow(List<string> cellsInRow, List<string> headers, List<ColumnMappingDto> mappings,
        Guid electionGuid, HashSet<string> bahaiIdLookup, HashSet<string> emailLookup, HashSet<string> phoneLookup, HashSet<string> nameLookup,
        int rowNumber, ImportPeopleResult result)
    {
        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            CanVote = true, // Default to eligible
            CanReceiveVotes = true // Default to eligible
        };

        // Apply mappings
        foreach (var mapping in mappings.Where(m => !string.IsNullOrEmpty(m.TargetField)))
        {
            var columnIndex = headers.IndexOf(mapping.FileColumn);
            if (columnIndex >= 0 && columnIndex < cellsInRow.Count)
            {
                var value = cellsInRow[columnIndex]?.Trim();
                ApplyFieldMapping(person, mapping.TargetField!, value);
            }
        }

        var foundErrors = false;

        // Validate required fields

        if (string.IsNullOrEmpty(person.LastName))
        {
            result.PeopleSkipped++;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.missingLastName",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString()
                }
            });
            foundErrors = true;
        }

        if (string.IsNullOrEmpty(person.FirstName))
        {
            result.PeopleSkipped++;
            result.Errors.Add(new ImportErrorDto
            {
                Key = "import.errors.missingFirstName",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString()
                }
            });
            foundErrors = true;
        }

        if (!string.IsNullOrEmpty(person.BahaiId))
        {
            if (bahaiIdLookup.Contains(person.BahaiId))
            {
                result.PeopleSkipped++;
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.duplicateBahaiId",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["bahaiId"] = person.BahaiId
                    }
                });
                foundErrors = true;
            }
            else
            {
                // add it
                bahaiIdLookup.Add(person.BahaiId);
            }
        }


        if (!string.IsNullOrEmpty(person.Email))
        {
            if (emailLookup.Contains(person.Email))
            {
                result.PeopleSkipped++;
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.duplicateEmail",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["email"] = person.Email
                    }
                });
                foundErrors = true;
            }
            else
            {
                // add it
                emailLookup.Add(person.Email);
            }
        }


        if (!string.IsNullOrEmpty(person.Phone))
        {
            if (phoneLookup.Contains(person.Phone))
            {
                result.PeopleSkipped++;
                result.Errors.Add(new ImportErrorDto
                {
                    Key = "import.errors.duplicatePhone",
                    Parameters = new Dictionary<string, string>
                    {
                        ["rowNumber"] = rowNumber.ToString(),
                        ["phone"] = person.Phone
                    }
                });
                foundErrors = true;
            }
            else
            {
                // add it
                phoneLookup.Add(person.Phone);
            }
        }


        var nameKey = $"{person.FirstName ?? ""} {person.LastName}".Trim().ToLowerInvariant();
        if (nameLookup.Contains(nameKey))
        {
            result.PeopleSkipped++;
            result.Warnings.Add(new ImportWarningDto
            {
                Key = "import.errors.duplicateName",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString(),
                    ["firstName"] = person.FirstName ?? "",
                    ["lastName"] = person.LastName ?? ""
                }
            });
            foundErrors = true;
        }
        else
        {
            // add it
            nameLookup.Add(nameKey);
        }


        // Set eligibility
        var ineligibleReasonMapping = mappings.FirstOrDefault(m => m.TargetField == "IneligibleReasonDescription");
        if (ineligibleReasonMapping != null)
        {
            var columnIndex = headers.IndexOf(ineligibleReasonMapping.FileColumn);
            if (columnIndex >= 0 && columnIndex < cellsInRow.Count)
            {
                var eligibilityValue = cellsInRow[columnIndex]?.Trim();
                SetEligibility(person, eligibilityValue, result, rowNumber);
            }
        }

        if (foundErrors)
        {
            return null; // Skip this person due to validation errors
        }

        return person;
    }

    private void ApplyFieldMapping(Person person, string targetField, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        switch (targetField)
        {
            case "FirstName":
                person.FirstName = value;
                break;
            case "LastName":
                person.LastName = value;
                break;
            case "BahaiId":
                person.BahaiId = value;
                break;
            case "Area":
                person.Area = value;
                break;
            case "Email":
                person.Email = value;
                break;
            case "Phone":
                person.Phone = value;
                break;
            case "OtherNames":
                person.OtherNames = value;
                break;
            case "OtherLastNames":
                person.OtherLastNames = value;
                break;
            case "OtherInfo":
                person.OtherInfo = value;
                break;
        }
    }

    private void SetEligibility(Person person, string? eligibilityValue, ImportPeopleResult result, int rowNumber)
    {
        if (string.IsNullOrEmpty(eligibilityValue))
        {
            person.CanVote = true;
            person.CanReceiveVotes = true;
            person.IneligibleReasonGuid = null;
            return;
        }

        var reason = IneligibleReasonEnum.GetByDescription(eligibilityValue) ??
                    IneligibleReasonEnum.GetByCode(eligibilityValue);

        if (reason != null)
        {
            person.CanVote = reason.CanVote;
            person.CanReceiveVotes = reason.CanReceiveVotes;
            person.IneligibleReasonGuid = reason.ReasonGuid;
        }
        else
        {
            // Unrecognized eligibility value - treat as eligible but warn
            person.CanVote = true;
            person.CanReceiveVotes = true;
            person.IneligibleReasonGuid = null;
            result.Warnings.Add(new ImportWarningDto
            {
                Key = "import.warnings.unrecognizedEligibility",
                Parameters = new Dictionary<string, string>
                {
                    ["rowNumber"] = rowNumber.ToString(),
                    ["eligibilityValue"] = eligibilityValue
                }
            });
        }
    }

    private async Task ReportProgress(Guid electionGuid, int processed, int total, string status)
    {
        await _signalRNotificationService.SendPeopleImportProgressAsync(electionGuid, processed, total, status);
    }
}
