# TallyJ 4

Election management and ballot tallying system for Bahá'í communities.

## Project Structure

```
TallyJ-4/
├── backend/          # .NET 9.0 ASP.NET Core Web API
│   ├── EF/          # Entity Framework models and migrations
│   ├── Helpers/     # Utility classes and extensions
│   └── scripts/     # Database management scripts
├── frontend/        # Vue 3 + Vite SPA
│   ├── src/         # Source code
│   ├── public/      # Static assets
│   └── dist/        # Production build output
└── .zenflow/        # Reverse engineering documentation
```

## Quick Start

### Prerequisites

- .NET SDK 10.0 or later
- SQL Server Express (or Docker SQL Server)
- Optional: SQL Server Management Studio or Azure Data Studio

### Database Setup

1. **Install SQL Server Express** (or use Docker - see `backend/SETUP.md`)

2. **Create and seed database:**
   ```bash
   cd backend
   dotnet ef database update
   dotnet run
   ```

3. **Verify setup:**
   ```bash
   curl -X POST http://localhost:5000/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@tallyj.test","password":"TestPass123!"}'
   ```

See **[backend/SETUP.md](backend/SETUP.md)** for detailed setup instructions.

### Test Credentials

| Email | Password | Role |
|-------|----------|------|
| admin@tallyj.test | TestPass123! | Administrator |
| teller@tallyj.test | TestPass123! | Election Teller |
| voter@tallyj.test | TestPass123! | Voter |

## Development

### Backend (API)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

API available at: `http://localhost:5000`

**API Documentation**: Swagger UI available at `http://localhost:5000/swagger`

**Core endpoints:**
- Authentication: `/auth/register`, `/auth/login`, `/auth/refresh`
- Elections: `/api/elections`
- People: `/api/people`
- Ballots: `/api/ballots`
- Votes: `/api/votes`
- Tellers: `/api/tellers`
- Results: `/api/results`
- Import: `/api/import`
- Logs: `/api/logs`

### Frontend (Vue 3 + Vite)

**Install dependencies:**
```bash
cd frontend
npm install
```

**Start development server:**
```bash
npm run dev
```

**Build for production:**
```bash
npm run build
```

**Preview production build:**
```bash
npm run preview
```

Frontend available at: `http://localhost:8095`

**Environment Configuration:**

The frontend uses environment variables for API configuration. Create a `.env.development` file in the `frontend/` directory:

```env
VITE_API_URL=http://localhost:5016/api
```

For production, create `.env.production`:

```env
VITE_API_URL=https://your-production-api.com/api
```

## Production Deployment

### Backend Deployment

1. **Build the application:**
   ```bash
   cd backend
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to server:**
   - Copy the `publish` folder to your production server
   - Configure IIS or reverse proxy (nginx/Apache)
   - Set environment variables for production database connection
   - Ensure SQL Server is accessible from the production environment

3. **Database migration:**
   ```bash
   dotnet ef database update --connection "your-production-connection-string"
   ```

### Frontend Deployment

1. **Build for production:**
   ```bash
   cd frontend
   npm run build
   ```

2. **Deploy static files:**
   - Copy the `dist` folder contents to your web server
   - Configure your web server to serve the static files
   - Set up proper routing for SPA (handle 404s by serving index.html)

3. **Web Server Configuration (nginx example):**
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;
       root /path/to/dist;
       index index.html;

       location / {
           try_files $uri $uri/ /index.html;
       }

       location /api {
           proxy_pass http://localhost:5000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

### Docker Deployment (Optional)

**Backend Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/TallyJ.csproj", "."]
RUN dotnet restore
COPY backend/ ./
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TallyJ.dll"]
```

**Frontend Dockerfile:**
```dockerfile
FROM node:18-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/nginx.conf
```

### Environment Variables

**Backend (.env or appsettings.Production.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=tallyj;User Id=your-user;Password=your-password;"
  },
  "Jwt": {
    "Key": "your-256-bit-secret-key",
    "Issuer": "your-domain.com",
    "Audience": "your-domain.com"
  }
}
```

**Frontend (.env.production):**
```env
VITE_API_URL=https://your-api-domain.com/api
```

### Security Considerations

- Use HTTPS in production
- Store secrets securely (Azure Key Vault, AWS Secrets Manager, etc.)
- Configure CORS properly for your domain
- Set up proper logging and monitoring
- Regular security updates for dependencies
- Database backups and recovery procedures

## API Usage Examples

### Authentication

**Login:**
```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"TestPass123!"}'
```

Response includes `accessToken` and `refreshToken`. Use the access token in subsequent requests:
```bash
export TOKEN="your_access_token_here"
```

### Elections API

**Create Election:**
```bash
curl -X POST http://localhost:5000/api/elections \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Local Assembly Election 2024",
    "dateOfElection": "2024-12-15T00:00:00Z",
    "electionType": "LSA",
    "numberOfElections": 1,
    "numberToElect": 9,
    "numberExtra": 0
  }'
```

**Get All Elections (Paginated):**
```bash
curl -X GET "http://localhost:5000/api/elections?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

**Get Election by GUID:**
```bash
curl -X GET http://localhost:5000/api/elections/{electionGuid} \
  -H "Authorization: Bearer $TOKEN"
```

**Update Election:**
```bash
curl -X PUT http://localhost:5000/api/elections/{electionGuid} \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Election Name",
    "tallyStatus": "InProgress",
    "numberOfElections": 1
  }'
```

**Delete Election:**
```bash
curl -X DELETE http://localhost:5000/api/elections/{electionGuid} \
  -H "Authorization: Bearer $TOKEN"
```

### People API

**Create Person:**
```bash
curl -X POST http://localhost:5000/api/people \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "electionGuid": "{electionGuid}",
    "canReceiveVotes": true,
    "canVote": true
  }'
```

**Get People by Election:**
```bash
curl -X GET "http://localhost:5000/api/people/election/{electionGuid}?pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

**Search People:**
```bash
curl -X GET "http://localhost:5000/api/people/search?electionGuid={electionGuid}&searchTerm=smith" \
  -H "Authorization: Bearer $TOKEN"
```

### Ballots API

**Create Ballot:**
```bash
curl -X POST http://localhost:5000/api/ballots \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "electionGuid": "{electionGuid}",
    "ballotCode": "A001",
    "statusCode": "Ok"
  }'
```

**Get Ballots by Election:**
```bash
curl -X GET "http://localhost:5000/api/ballots/election/{electionGuid}?pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

### Votes API

**Create Vote:**
```bash
curl -X POST http://localhost:5000/api/votes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ballotGuid": "{ballotGuid}",
    "personGuid": "{personGuid}",
    "singleNameElectionCount": 0
  }'
```

**Get Votes by Ballot:**
```bash
curl -X GET http://localhost:5000/api/votes/ballot/{ballotGuid} \
  -H "Authorization: Bearer $TOKEN"
```

### Results API

**Get Election Results:**
```bash
curl -X GET http://localhost:5000/api/results/election/{electionGuid} \
  -H "Authorization: Bearer $TOKEN"
```

**Get Final Results:**
```bash
curl -X GET http://localhost:5000/api/results/election/{electionGuid}/final \
  -H "Authorization: Bearer $TOKEN"
```

### Tellers API

**Assign Teller to Election:**
```bash
curl -X POST http://localhost:5000/api/tellers \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "electionGuid": "{electionGuid}",
    "userId": "{userId}",
    "role": "HeadTeller"
  }'
```

**Get Tellers by Election:**
```bash
curl -X GET http://localhost:5000/api/tellers/election/{electionGuid} \
  -H "Authorization: Bearer $TOKEN"
```

### Import API

**Import People CSV:**
```bash
curl -X POST http://localhost:5000/api/import/people \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@people.csv" \
  -F "electionGuid={electionGuid}"
```

### Logs API

**Get Logs by Election:**
```bash
curl -X GET "http://localhost:5000/api/logs/election/{electionGuid}?pageNumber=1&pageSize=50" \
  -H "Authorization: Bearer $TOKEN"
```

### Reset Database

```bash
# Windows
cd backend\scripts
.\reset-database.ps1

# Linux/macOS
cd backend/scripts
chmod +x reset-database.sh
./reset-database.sh
```

## Seeded Test Data

The database is automatically seeded with:

- **3 users** (admin, teller, voter)
- **2 elections:**
  - Springfield LSA Election 2024 (active, 30 voters, 20 ballots)
  - National Convention 2024 (completed, 15 delegates, with results)
- **Ballots and votes** demonstrating real election scenarios
- **Tellers, messages, and logs**

## Technology Stack

### Backend
- .NET 9.0
- ASP.NET Core Web API  
- Entity Framework Core 9.0
- SQL Server
- ASP.NET Core Identity (JWT Bearer authentication)
- Serilog (logging)

### Frontend
- Vue 3.5.22 (Composition API)
- TypeScript 5.9.3
- Vite 7.1.14
- Pinia 3.0.3 (state management)
- Vue Router 4.6.3
- Element Plus 2.11.5 (UI library)
- Axios 1.13.2
- Vue I18n 10.0.8

## Documentation

- **Setup Guide**: [backend/SETUP.md](backend/SETUP.md)
- **Reverse Engineering Docs**: [.zenflow/tasks/reverse-engineer-and-design-new-cd6a/](.zenflow/tasks/reverse-engineer-and-design-new-cd6a/)
- **Technical Spec**: [.zenflow/tasks/plan-new-build-b9db/spec.md](.zenflow/tasks/plan-new-build-b9db/spec.md)

## Project Status

✅ **Phase 1: Database Foundation** - Complete
✅ **Phase 2: API Layer** - Complete
  - 8 REST API controllers with full CRUD operations
  - DTOs, services, and FluentValidation for all endpoints
  - AutoMapper profiles for entity-DTO mapping
  - Global error handling and Swagger documentation
  - Comprehensive unit and integration tests

✅ **Phase 3: Tally Algorithms** - Complete
  - STV (Single Transferable Vote) algorithm implemented
  - Condorcet voting method implemented
  - Multi-seat election support
  - Tie detection and resolution
  - Result sectioning (Elected/Extra/Other)

✅ **Phase 4: Frontend Application** - Complete
  - Vue 3 SPA with TypeScript and modern tooling
  - 11 pages: Dashboard, Elections, People, Ballots, Tally, Results, Profile
  - 14+ reusable components with Element Plus UI library
  - JWT authentication with automatic token refresh
  - Internationalization (English/French)
  - WCAG 2.1 AA accessibility compliance
  - Comprehensive testing infrastructure (46 tests passing)
  - Cross-browser compatibility (Chrome, Firefox, Safari, Edge)
  - Fully responsive design with mobile navigation
  - Optimized production builds with code splitting

✅ **Phase 5: Production Readiness** - Complete
  - Accessibility audit and WCAG compliance
  - Cross-browser testing and compatibility
  - Mobile responsiveness improvements
  - Production deployment documentation
  - Security hardening and best practices

🚧 **Phase 6: Real-time Features (SignalR)** - Planned  

## Contributing

This is a rebuild of the TallyJ election system. Comprehensive reverse engineering documentation is available in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`.

## License

TBD
