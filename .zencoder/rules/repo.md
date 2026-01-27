---
description: Repository Information Overview
alwaysApply: true
---

# TallyJ 4 Repository Information Overview

## Repository Summary
TallyJ 4 is an election management and ballot tallying system designed for Bahá'í communities. It provides a comprehensive platform for managing elections, people, ballots, votes, and results with real-time capabilities.

## Repository Structure
- **backend/**: ASP.NET Core Web API with Entity Framework, SignalR, and JWT authentication
- **frontend/**: Vue 3 + Vite single-page application with TypeScript
- **TallyJ4.Tests/**: Unit and integration tests using xUnit
- **.github/**: GitHub Actions workflows for CI/CD
- **.vscode/**: VS Code configuration
- **.zencoder/**: Custom tooling workflows
- **.zenflow/**: Reverse engineering documentation

### Main Repository Components
- **Backend API**: Handles all business logic, database operations, and real-time notifications
- **Frontend SPA**: User interface for election management and voting
- **Test Suite**: Comprehensive testing for backend functionality

## Projects

### Backend (ASP.NET Core Web API)
**Configuration File**: backend/TallyJ4.csproj

#### Language & Runtime
**Language**: C#  
**Version**: .NET 10.0  
**Build System**: MSBuild  
**Package Manager**: NuGet

#### Dependencies
**Main Dependencies**:
- AutoMapper (12.0.1)
- FluentValidation (11.11.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (9.0.10)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.1)
- Microsoft.AspNetCore.OpenApi (9.0.10)
- Microsoft.AspNetCore.SignalR (1.2.0)
- Microsoft.EntityFrameworkCore.SqlServer (10.0.1)
- Newtonsoft.Json (13.0.4)
- Serilog (4.3.0)
- Swashbuckle.AspNetCore (9.0.6)

**Development Dependencies**:
- Microsoft.EntityFrameworkCore.Design (10.0.1)
- Microsoft.EntityFrameworkCore.Tools (10.0.1)

#### Build & Installation
```bash
cd backend
dotnet restore
dotnet build
```

#### Main Files & Resources
**Entry Point**: backend/Program.cs  
**Configuration Files**: backend/appsettings.json, backend/appsettings.Development.json  
**Database Scripts**: backend/scripts/  
**API Documentation**: Swagger UI at /swagger

#### Testing
**Framework**: xUnit  
**Test Location**: TallyJ4.Tests/  
**Naming Convention**: *Tests.cs  
**Configuration**: TallyJ4.Tests/TallyJ4.Tests.csproj

**Run Command**:
```bash
dotnet test
```

### Frontend (Vue 3 + Vite SPA)
**Configuration File**: frontend/package.json

#### Language & Runtime
**Language**: TypeScript/JavaScript  
**Version**: Node.js (not specified, uses modern versions)  
**Build System**: Vite  
**Package Manager**: npm

#### Dependencies
**Main Dependencies**:
- Vue (3.5.22)
- Vue Router (4.6.3)
- Pinia (3.0.3)
- Element Plus (2.11.5)
- Axios (1.13.2)
- Vue I18n (11.0.0)
- Microsoft SignalR (9.0.6)

**Development Dependencies**:
- Vite (7.1.14)
- Vitest (4.0.18)
- Vue TSC (3.1.0)
- TypeScript (5.9.3)

#### Build & Installation
```bash
cd frontend
npm install
npm run build
```

#### Main Files & Resources
**Entry Point**: frontend/index.html  
**Configuration Files**: frontend/vite.config.ts, frontend/tsconfig.json  
**Source Code**: frontend/src/  
**Static Assets**: frontend/public/

#### Testing
**Framework**: Vitest  
**Test Location**: frontend/src/ (tests alongside code)  
**Naming Convention**: *.test.ts, *.spec.ts  
**Configuration**: frontend/vitest.config.ts

**Run Command**:
```bash
npm run test
```

### Tests (xUnit Test Project)
**Configuration File**: TallyJ4.Tests/TallyJ4.Tests.csproj

#### Language & Runtime
**Language**: C#  
**Version**: .NET 10.0  
**Build System**: MSBuild  
**Package Manager**: NuGet

#### Dependencies
**Main Dependencies**:
- Microsoft.NET.Test.Sdk (17.14.1)
- xUnit (2.9.3)
- Moq (4.20.70)
- Microsoft.AspNetCore.Mvc.Testing (9.0.0)
- Microsoft.EntityFrameworkCore.InMemory (10.0.1)

#### Build & Installation
```bash
dotnet restore
dotnet build
```

#### Testing
**Framework**: xUnit  
**Test Location**: TallyJ4.Tests/UnitTests/, TallyJ4.Tests/IntegrationTests/  
**Naming Convention**: *Tests.cs  
**Configuration**: TallyJ4.Tests/TallyJ4.Tests.csproj

**Run Command**:
```bash
dotnet test
```