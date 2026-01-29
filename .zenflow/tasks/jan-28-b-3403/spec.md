# Technical Specification: Implement Report Export Functionality

## Technical Context
- **Backend**: ASP.NET Core Web API (.NET 10.0) with Entity Framework, using C#
- **Frontend**: Vue 3 + TypeScript with Element Plus UI library
- **Database**: SQL Server via Entity Framework
- **Current Export Status**: Placeholder function exists in ReportingPage.vue that only shows a message

## Implementation Approach
Implement server-side report generation using established patterns in the codebase:
- Add new API endpoint in ReportsController for export requests
- Create ReportExportService to handle PDF/Excel generation logic
- Use proven libraries: ClosedXML for Excel, iTextSharp for PDF
- Follow existing service/repository patterns for data access
- Maintain consistent error handling and logging

## Source Code Structure Changes
### Backend Changes
- **Controllers/ReportsController.cs**: Add POST endpoint `/api/reports/export/{electionId}`
- **Services/ReportExportService.cs**: New service class for export logic
- **Models/ExportRequest.cs**: DTO for export parameters (format, electionId, filters)
- **TallyJ4.csproj**: Add NuGet packages for export libraries

### Frontend Changes
- **src/pages/results/ReportingPage.vue**: Update exportReport function to call API and handle download
- **src/api/reports.ts**: Add export API call function

## Data Model / API / Interface Changes
- **New API Endpoint**: 
  - Method: POST
  - Path: /api/reports/export/{electionId}
  - Body: { format: 'pdf' | 'excel', includeDetails: boolean }
  - Response: File download (application/pdf or application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)
- **No database schema changes required** - uses existing election and result data

## Verification Approach
- Unit tests for ReportExportService
- Integration tests for export endpoint
- Manual testing: Generate reports in both formats, verify content accuracy
- Run existing test suite: `dotnet test`
- Lint checks: Ensure code follows project conventions