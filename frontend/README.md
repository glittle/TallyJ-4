# TallyJ 4 - Election Management System

A modern, real-time election management and ballot tallying system designed for Bahá’í communities.

## Features

- **Election Management**: Create and configure elections with custom settings
- **People Management**: Import and manage voter lists with bulk operations
- **Ballot Management**: Handle ballot creation, distribution, and collection
- **Real-time Results**: Live tallying and result reporting with SignalR
- **Multi-user Collaboration**: Real-time updates for election officials
- **Responsive Design**: Works on desktop and mobile devices
- **Offline Support**: Service worker for offline functionality

## Quick Start

### Prerequisites

- Node.js 18+
- .NET 8.0+

### Installation

1. Clone the repository
2. Install frontend dependencies:
   ```bash
   cd frontend
   npm install
   ```
3. Install backend dependencies:
   ```bash
   cd backend
   dotnet restore
   ```

### Development

1. Start the backend API:

   ```bash
   cd backend
   dotnet run
   ```

2. Start the frontend development server:

   ```bash
   cd frontend
   npm run dev
   ```

3. Open http://localhost:8095 in your browser

### Building for Production

```bash
cd frontend
npm run build
```

## Environment Variables

Create a `.env` file in the frontend directory:

```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_SENTRY_DSN=your_sentry_dsn_here
```

## Testing

```bash
# Run unit tests
npm run test

# Run tests with coverage
npm run test:coverage

# Run backend tests
cd ../backend
dotnet test
```

## Deployment

The application is configured for deployment with:

- Frontend: Static hosting (Vercel, Netlify, etc.)
- Backend: Azure App Service, AWS, or any ASP.NET Core hosting
- Database: SQL Server (Azure SQL, AWS RDS, etc.)

## Architecture

- **Frontend**: Vue 3 + TypeScript + Vite + Element Plus
- **Backend**: ASP.NET Core + Entity Framework + SignalR
- **Database**: SQL Server
- **Real-time**: SignalR for live updates
- **Styling**: Element Plus component library

## Contributing

1. Follow the existing code style and conventions
2. Write tests for new features
3. Ensure all tests pass before submitting PRs
4. Update documentation as needed

## License

Copyright © Bahá’í communities. All rights reserved.
