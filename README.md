# MentorEdu Backend

ASP.NET Core Web API for MentorEdu platform.

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- SQL Server

### Running the Application

```bash
dotnet run
```

The API will start on: **https://localhost:7082**

## Port Configuration

⚠️ **Important**: This application uses **ONLY port 7082** for the backend.

See [PORT_CONFIGURATION.md](docs/PORT_CONFIGURATION.md) for detailed information about port setup.

## Project Structure

```
MentorEdu/
├── Controllers/Api/    # API Controllers
├── Services/          # Business Logic Layer
├── Repositories/      # Data Access Layer
├── Models/            # Domain Models
├── DTOs/              # Data Transfer Objects
├── Data/              # DbContext & Migrations
└── docs/              # Documentation
```

## Documentation

- [Port Configuration](docs/PORT_CONFIGURATION.md) - Port setup and troubleshooting
- [Admin API Documentation](docs/ADMIN_API_DOCUMENTATION.md)
- [Google OAuth Guide](docs/GOOGLE_OAUTH_QUICK_GUIDE.md)

## Database Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update
```

## Environment

Default environment: **Development**

Configure via `appsettings.Development.json`
