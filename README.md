"# TallyJ 4

Election management and ballot tallying system for Bahá'í communities.

## Project Structure

```
TallyJ-4/
├── backend/          # .NET 9.0 ASP.NET Core Web API
│   ├── EF/          # Entity Framework models and migrations  
│   ├── Helpers/     # Utility classes and extensions
│   └── scripts/     # Database management scripts
├── frontend/        # Vue 3 + Vite SPA (coming soon)
└── .zenflow/        # Reverse engineering documentation
```

## Quick Start

### Prerequisites

- .NET SDK 9.0 or later
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
     -d '{"email":"admin@tallyj.test","password":"Admin@123"}'
   ```

See **[backend/SETUP.md](backend/SETUP.md)** for detailed setup instructions.

### Test Credentials

| Email | Password | Role |
|-------|----------|------|
| admin@tallyj.test | Admin@123 | Administrator |
| teller@tallyj.test | Teller@123 | Election Teller |
| voter@tallyj.test | Voter@123 | Voter |

## Development

### Backend (API)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

API available at: `http://localhost:5000`

**Available endpoints:**
- `POST /auth/register` - Register new user
- `POST /auth/login` - Login and receive JWT token  
- `POST /auth/refresh` - Refresh access token
- `GET /protected` - Test authenticated endpoint

### Frontend (coming soon)

```bash
cd frontend
npm install
npm run dev
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

### Frontend (planned)
- Vue 3 (Composition API)
- TypeScript
- Vite
- Pinia (state management)
- Element Plus (UI library)

## Documentation

- **Setup Guide**: [backend/SETUP.md](backend/SETUP.md)
- **Reverse Engineering Docs**: [.zenflow/tasks/reverse-engineer-and-design-new-cd6a/](.zenflow/tasks/reverse-engineer-and-design-new-cd6a/)
- **Technical Spec**: [.zenflow/tasks/plan-new-build-b9db/spec.md](.zenflow/tasks/plan-new-build-b9db/spec.md)

## Project Status

✅ Database layer (migrations, seeding)  
🚧 API endpoints (in progress)  
🚧 Business logic services  
🚧 Frontend application  
🚧 SignalR real-time features  

## Contributing

This is a rebuild of the TallyJ election system. Comprehensive reverse engineering documentation is available in `.zenflow/tasks/reverse-engineer-and-design-new-cd6a/`.

## License

TBD" 
