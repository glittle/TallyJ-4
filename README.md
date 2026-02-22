# TallyJ 4

TallyJ-4 is a full-stack, real-time election management and ballot tallying system for Bahá'í communities. It uses a .NET 10 ASP.NET Core Web API backend and a Vue 3 + Vite SPA frontend. The system is designed for multi-user collaboration, secure authentication, and robust election workflows, with a focus on feature parity and modernization from TallyJ v3.

## Project Structure

```
TallyJ-4/
├── backend/          # .NET 10.0 ASP.NET Core Web API
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

### Database Setup

1. **Install SQL Server Express** (or use Docker - see `backend/SETUP.md`)

2. **Create and seed database:**

   ```bash
   cd backend
   dotnet ef database update
   dotnet run
   ```

   The application will be available at `http://localhost:5000` and the database will be automatically seeded with test users and sample data.

3. **Verify setup:**
   
   Open your browser to `http://localhost:5000/swagger` to verify the API is running.

See **[backend/SETUP.md](backend/SETUP.md)** for detailed setup instructions.

### Test Credentials

| Email              | Password     | Role            |
| ------------------ | ------------ | --------------- |
| admin@tallyj.test  | TestPass123! | Administrator   |
| teller@tallyj.test | TestPass123! | Election Teller |
| voter@tallyj.test  | TestPass123! | Voter           |

## Development

### Backend (API)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

API available at: `http://localhost:5000`

**API Documentation**: Swagger UI available at `http://localhost:5000/swagger` for developers with access to the running application.

> **Note**: Google OAuth is available for officer/admin login. See [backend/SETUP.md](backend/SETUP.md#google-oauth-configuration-optional) for configuration instructions.

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

## Database Management

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

- .NET 10.0
- ASP.NET Core Web API
- Entity Framework Core 10.0
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

## Internationalization (i18n)

TallyJ 4 supports multiple languages using a unified JSON-based localization system shared between frontend and backend.

### Supported Languages

- **English** (en) - Default
- **French** (fr)

### Localization Architecture

Both frontend and backend use the same translation files located in `frontend/src/locales/`:

```
frontend/src/locales/
├── en/
│   ├── common.json       # Common UI strings
│   ├── auth.json         # Authentication
│   ├── elections.json    # Elections
│   ├── people.json       # People management
│   ├── ballots.json      # Ballots
│   ├── votes.json        # Votes
│   ├── results.json      # Results
│   ├── dashboard.json    # Dashboard
│   ├── errors.json       # Error messages
│   ├── nav.json          # Navigation
│   └── profile.json      # Profile and settings
├── fr/
│   └── [same files as en/]
├── common.json           # Non-translatable config (DO NOT TRANSLATE)
├── index.ts              # Loader for frontend
└── validate-translations.js  # Validation script
```

### Backend Configuration

The backend uses a custom JSON-based `IStringLocalizer` implementation that reads from the same locale files as the frontend.

**appsettings.json:**

```json
{
  "Localization": {
    "ResourcesPath": "../frontend/src/locales",
    "SupportedCultures": ["en", "fr"],
    "DefaultCulture": "en"
  }
}
```

For production deployments, update `ResourcesPath` to the absolute path where locale files are deployed.

### Adding New Translations

1. **Choose the appropriate file** based on the feature area (e.g., `auth.json` for authentication strings)

2. **Add the translation key** in **all locale files** with matching structure:

   **en/auth.json:**
   ```json
   {
     "auth": {
       "login": {
         "title": "Sign In"
       }
     }
   }
   ```

   **fr/auth.json:**
   ```json
   {
     "auth": {
       "login": {
         "title": "Se connecter"
       }
     }
   }
   ```

3. **Use the translation:**

   **Frontend (Vue):**
   ```vue
   <template>
     <h1>{{ $t('auth.login.title') }}</h1>
   </template>
   ```

   **Backend (C#):**
   ```csharp
   public class AuthService
   {
       private readonly IStringLocalizer _localizer;

       public AuthService(IStringLocalizer localizer)
       {
           _localizer = localizer;
       }

       public string GetLoginTitle()
       {
           return _localizer["auth.login.title"];
       }
   }
   ```

4. **Validate translations:**
   ```bash
   cd frontend
   npm run validate:i18n
   ```

### Translation Guidelines

- **Keys**: Use dot notation (e.g., `auth.errors.invalidCredentials`)
- **Structure**: Group related translations in the same file
- **Consistency**: All locale files must have matching keys
- **Empty values**: Never leave translation values empty
- **File splitting**: Keep files focused on a single feature area
- **Matching structure**: All translations in `en/auth.json` must have corresponding translations in `fr/auth.json`, `de/auth.json`, etc.

### Backend Localization

The backend respects the `Accept-Language` HTTP header and error messages are automatically localized based on the request language.

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

✅ **Phase 6: Real-time Features (SignalR)** - Complete

- SignalR integration for real-time election updates
- Live ballot status notifications
- Real-time result broadcasting during tallying
- WebSocket connections for teller coordination

🚧 **Phase 7: Advanced Reporting and Analytics** - Planned

- Election result visualizations and charts
- Historical election comparisons
- Export capabilities (PDF, Excel, CSV)
- Advanced filtering and search for election data
- Statistical analysis of voting patterns
- Custom report generation

## Contributing

This is a rebuild of the TallyJ election system. Comprehensive reverse engineering documentation is available in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`.

## License

TBD
