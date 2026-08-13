using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Entities;
using Backend.DTOs.Import;

namespace Backend.Services;

public partial class PeopleImportService
{
    private const int PreviewSamplesPerColumn = 3;

    private sealed record ParsedDataRow(int FileRowNumber, List<string> Cells);

    private async Task<(List<string> headers, List<ParsedDataRow> rows, int totalDataRows)> ParseFileContentAsync(
        ImportFile importFile,
        bool previewSamplesOnly = false)
    {
        if (importFile.FileType == "xlsx")
        {
            return await ParseXlsxFileAsync(importFile.Contents!, importFile.FirstDataRow, previewSamplesOnly);
        }

        return ParseTextFile(
            importFile.Contents!,
            importFile.FileType!,
            importFile.CodePage ?? 65001,
            previewSamplesOnly,
            importFile.FirstDataRow);
    }

    private async Task<(List<string> headers, List<ParsedDataRow> rows, int totalDataRows)> ParseXlsxFileAsync(
        byte[] content,
        int? firstDataRow = null,
        bool previewSamplesOnly = false)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = new List<string>();

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

        var allRows = worksheet.RowsUsed().ToList();
        var dataRowsStartIndex = allRows.FindIndex(r => r.RowNumber() == headerRowNumber) + 1;
        var rows = previewSamplesOnly
            ? new List<ParsedDataRow>()
            : new List<ParsedDataRow>(Math.Max(0, allRows.Count - dataRowsStartIndex));
        var samplesByColumn = previewSamplesOnly ? CreateSampleBuckets(columnCount) : null;
        var totalDataRows = 0;

        for (int i = dataRowsStartIndex; i < allRows.Count; i++)
        {
            var row = allRows[i];
            var rowData = new List<string>(columnCount);
            for (int colNum = 1; colNum <= columnCount; colNum++)
            {
                var cell = row.Cell(colNum);
                rowData.Add(cell.GetValue<string>() ?? "");
            }

            totalDataRows++;
            if (previewSamplesOnly)
            {
                CollectSamplesFromRow(samplesByColumn!, rowData);
            }
            else
            {
                rows.Add(new ParsedDataRow(row.RowNumber(), rowData));
            }
        }

        if (previewSamplesOnly)
        {
            return (headers, ToParsedPreviewRows(BuildPreviewRows(samplesByColumn!)), totalDataRows);
        }

        return (headers, rows, totalDataRows);
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

    private (List<string> headers, List<ParsedDataRow> rows, int totalDataRows) ParseTextFile(
        byte[] content,
        string fileType,
        int codePage,
        bool previewSamplesOnly = false,
        int? firstDataRow = null)
    {
        var encoding = Encoding.GetEncoding(codePage);
        var text = encoding.GetString(content);
        var lines = text.Split('\n')
            .Select(l => l.Trim('\r'))
            .ToArray();

        if (lines.All(string.IsNullOrWhiteSpace))
        {
            return (new List<string>(), new List<ParsedDataRow>(), 0);
        }

        var headerLineNumber = firstDataRow is > 0 ? firstDataRow.Value : 1;
        var headerIndex = headerLineNumber - 1;
        if (headerIndex < 0 || headerIndex >= lines.Length)
        {
            return (new List<string>(), new List<ParsedDataRow>(), 0);
        }

        var delimiter = fileType == "tab" ? '\t' : ',';
        var headers = ParseCsvLine(lines[headerIndex], delimiter).ToList();
        var columnCount = headers.Count;
        var rows = previewSamplesOnly
            ? new List<ParsedDataRow>()
            : new List<ParsedDataRow>(Math.Max(0, lines.Length - headerIndex - 1));
        var samplesByColumn = previewSamplesOnly ? CreateSampleBuckets(columnCount) : null;
        var totalDataRows = 0;

        for (int i = headerIndex + 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var row = ParseCsvLine(lines[i], delimiter);
            totalDataRows++;
            if (previewSamplesOnly)
            {
                CollectSamplesFromRow(samplesByColumn!, row);
            }
            else
            {
                rows.Add(new ParsedDataRow(i + 1, row));
            }
        }

        if (previewSamplesOnly)
        {
            return (headers, ToParsedPreviewRows(BuildPreviewRows(samplesByColumn!)), totalDataRows);
        }

        return (headers, rows, totalDataRows);
    }

    private static List<ParsedDataRow> ToParsedPreviewRows(List<List<string>> previewRows)
    {
        return previewRows.Select(cells => new ParsedDataRow(0, cells)).ToList();
    }

    private static List<List<string>> CreateSampleBuckets(int columnCount)
    {
        return Enumerable.Range(0, columnCount)
            .Select(_ => new List<string>(PreviewSamplesPerColumn))
            .ToList();
    }

    private static void CollectSamplesFromRow(List<List<string>> samplesByColumn, List<string> row)
    {
        for (var col = 0; col < samplesByColumn.Count; col++)
        {
            if (samplesByColumn[col].Count >= PreviewSamplesPerColumn)
            {
                continue;
            }

            var value = col < row.Count ? row[col]?.Trim() ?? "" : "";
            if (value.Length > 0)
            {
                samplesByColumn[col].Add(value);
            }
        }
    }

    private static List<List<string>> BuildPreviewRows(List<List<string>> samplesByColumn)
    {
        var maxSamples = samplesByColumn.Count == 0
            ? 0
            : samplesByColumn.Max(samples => samples.Count);
        var previewRows = new List<List<string>>(maxSamples);
        for (var i = 0; i < maxSamples; i++)
        {
            var previewRow = new List<string>(samplesByColumn.Count);
            foreach (var samples in samplesByColumn)
            {
                previewRow.Add(i < samples.Count ? samples[i] : "");
            }

            previewRows.Add(previewRow);
        }

        return previewRows;
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
        var mappings = headers
            .Select(header => new ColumnMappingDto { FileColumn = header, TargetField = null })
            .ToList();

        var candidates = new List<(int Index, string Field, int Score)>();
        for (var i = 0; i < headers.Count; i++)
        {
            var match = FindBestFieldMatch(headers[i]);
            if (match.HasValue)
            {
                candidates.Add((i, match.Value.Field, match.Value.Score));
            }
        }

        foreach (var group in candidates.GroupBy(candidate => candidate.Field))
        {
            var winner = group
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Index)
                .First();
            mappings[winner.Index].TargetField = winner.Field;
        }

        return mappings;
    }

    private string NormalizeHeader(string header)
    {
        var builder = new StringBuilder(header.Length);
        foreach (var character in header.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private (string Field, int Score)? FindBestFieldMatch(string header)
    {
        var normalizedHeader = NormalizeHeader(header);
        if (normalizedHeader.Length == 0)
        {
            return null;
        }

        string? bestField = null;
        var bestScore = 0;

        foreach (var (field, aliases) in FieldAliases)
        {
            var score = ScoreFieldMatch(normalizedHeader, field, aliases);
            if (score > bestScore)
            {
                bestScore = score;
                bestField = field;
            }
        }

        return bestField == null ? null : (bestField, bestScore);
    }

    private int ScoreFieldMatch(string normalizedHeader, string field, IReadOnlyList<string> aliases)
    {
        if (normalizedHeader == NormalizeHeader(field))
        {
            return 1000 + normalizedHeader.Length;
        }

        var bestAliasScore = 0;
        foreach (var alias in aliases)
        {
            var normalizedAlias = NormalizeHeader(alias);
            if (normalizedAlias != normalizedHeader)
            {
                continue;
            }

            // Longer aliases are more specific ("baha'i id" beats "id")
            var score = 100 + normalizedAlias.Length;
            if (score > bestAliasScore)
            {
                bestAliasScore = score;
            }
        }

        return bestAliasScore;
    }
}
