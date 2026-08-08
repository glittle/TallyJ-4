using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Entities;
using Backend.DTOs.Import;

namespace Backend.Services;

public partial class PeopleImportService
{
    private async Task<(List<string> headers, List<List<string>> rows, int totalDataRows)> ParseFileContentAsync(ImportFile importFile)
    {
        if (importFile.FileType == "xlsx")
        {
            return await ParseXlsxFileAsync(importFile.Contents!, importFile.FirstDataRow);
        }
        else
        {
            return ParseTextFile(importFile.Contents!, importFile.FileType!, importFile.CodePage ?? 65001);
        }
    }

    private async Task<(List<string> headers, List<List<string>> rows, int totalDataRows)> ParseXlsxFileAsync(byte[] content, int? firstDataRow = null)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = new List<string>();
        var rows = new List<List<string>>();

        // Determine header row: use provided firstDataRow or auto-detect
        int headerRowNumber;
        if (firstDataRow.HasValue && firstDataRow.Value > 0)
        {
            headerRowNumber = firstDataRow.Value;
        }
        else
        {
            headerRowNumber = DetectHeaderRow(worksheet);
        }

        // Read headers from detected row
        var headerRow = worksheet.Row(headerRowNumber);
        var columnCount = headerRow.CellsUsed().Count();
        foreach (var cell in headerRow.CellsUsed())
        {
            headers.Add(cell.GetValue<string>() ?? "");
        }

        // Read all data rows after the header row
        var allRows = worksheet.RowsUsed().ToList();
        var dataRowsStartIndex = allRows.FindIndex(r => r.RowNumber() == headerRowNumber) + 1;

        for (int i = dataRowsStartIndex; i < allRows.Count; i++)
        {
            var row = allRows[i];
            var rowData = new List<string>();
            for (int colNum = 1; colNum <= columnCount; colNum++)
            {
                var cell = row.Cell(colNum);
                rowData.Add(cell.GetValue<string>() ?? "");
            }
            rows.Add(rowData);
        }

        return (headers, rows, rows.Count);
    }

    /// <summary>
    /// Detects the row number (1-based) where column headers are likely located.
    /// Scans the first 10 rows looking for text-based headers that match known field names.
    /// The Take() method safely handles worksheets with fewer than 10 rows.
    /// </summary>
    private int DetectHeaderRow(ClosedXML.Excel.IXLWorksheet worksheet)
    {
        const int maxRowsToScan = 10;
        var allRows = worksheet.RowsUsed().Take(maxRowsToScan).ToList();

        if (!allRows.Any())
            return 1; // Default to first row if no rows found

        int bestRowNumber = 1;
        int bestScore = 0;

        foreach (var row in allRows)
        {
            int score = ScoreHeaderRow(row);
            if (score > bestScore)
            {
                bestScore = score;
                bestRowNumber = row.RowNumber();
            }
        }

        return bestRowNumber;
    }

    /// <summary>
    /// Scores a row based on how likely it is to be a header row.
    /// Higher scores indicate more header-like characteristics.
    /// Uses scoring weights: Text cells (+2), Known fields (+10), Header keywords (+5).
    /// </summary>
    private int ScoreHeaderRow(ClosedXML.Excel.IXLRow row)
    {
        int score = 0;
        var cells = row.CellsUsed().ToList();

        if (!cells.Any())
            return 0;

        foreach (var cell in cells)
        {
            var value = cell.GetValue<string>()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(value))
                continue;

            // Bonus: Cell contains text (not just numbers)
            if (!double.TryParse(value, out _))
            {
                score += TextCellScore;
            }

            // Bonus: Matches known field aliases
            var normalizedValue = NormalizeHeader(value);
            foreach (var fieldAliases in FieldAliases.Values)
            {
                if (fieldAliases.Any(alias => NormalizeHeader(alias) == normalizedValue))
                {
                    score += KnownFieldScore; // Strong indicator of a header
                    break;
                }
            }

            // Bonus: Contains common header keywords
            var lowerValue = value.ToLower();
            if (lowerValue.Contains("name") || lowerValue.Contains("id") ||
                lowerValue.Contains("email") || lowerValue.Contains("phone") ||
                lowerValue.Contains("area") || lowerValue.Contains("status") ||
                lowerValue.Contains("eligibility"))
            {
                score += HeaderKeywordScore;
            }
        }

        return score;
    }

    private (List<string> headers, List<List<string>> rows, int totalDataRows) ParseTextFile(byte[] content, string fileType, int codePage)
    {
        var encoding = Encoding.GetEncoding(codePage);
        var text = encoding.GetString(content);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim('\r', '\n'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (lines.Length == 0)
        {
            return (new List<string>(), new List<List<string>>(), 0);
        }

        var delimiter = fileType == "tab" ? '\t' : ',';
        var headers = ParseCsvLine(lines[0], delimiter).ToList();
        var rows = new List<List<string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            rows.Add(ParseCsvLine(lines[i], delimiter));
        }

        return (headers, rows, rows.Count);
    }

    private List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }

    private List<ColumnMappingDto> GenerateAutoMappings(List<string> headers)
    {
        var mappings = new List<ColumnMappingDto>();

        foreach (var header in headers)
        {
            var normalizedHeader = NormalizeHeader(header);
            var targetField = FindMatchingField(normalizedHeader);

            mappings.Add(new ColumnMappingDto
            {
                FileColumn = header,
                TargetField = targetField
            });
        }

        return mappings;
    }

    private string NormalizeHeader(string header)
    {
        return header
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }

    private string? FindMatchingField(string normalizedHeader)
    {
        foreach (var (field, aliases) in FieldAliases)
        {
            if (aliases.Contains(normalizedHeader))
            {
                return field;
            }
        }
        return null;
    }
}
