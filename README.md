# Jama Connect CLI

A .NET 9 command-line interface for interacting with [Jama Connect](https://www.jamasoftware.com/platform/jama-connect/) requirements management platform.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A Jama Connect instance with API access
- OAuth 2.0 client credentials (Client ID and Client Secret)

## Installation

### Build from source

```bash
git clone https://github.com/Tjeerd-menno/jama-connect-cli.git
cd jama-connect-cli
dotnet build src/JamaConnect.Cli/JamaConnect.Cli.csproj --configuration Release
```

### Run directly

```bash
dotnet run --project src/JamaConnect.Cli -- [command] [options]
```

## Configuration

Create or edit `appsettings.json` in the same directory as the executable (or the project directory when running via `dotnet run`):

```json
{
  "JamaConnect": {
    "BaseUrl": "https://your-instance.jamacloud.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "TokenEndpoint": "/rest/oauth/token",
    "TimeoutSeconds": 30
  }
}
```

You can also configure via environment variables prefixed with `JAMA_`:

| Environment Variable               | Description                             |
|------------------------------------|-----------------------------------------|
| `JAMA_JamaConnect__BaseUrl`        | Base URL of your Jama Connect instance  |
| `JAMA_JamaConnect__ClientId`       | OAuth 2.0 Client ID                     |
| `JAMA_JamaConnect__ClientSecret`   | OAuth 2.0 Client Secret                 |
| `JAMA_JamaConnect__TokenEndpoint`  | Token endpoint path                     |
| `JAMA_JamaConnect__TimeoutSeconds` | Request timeout in seconds              |

## Usage

```
jama-connect [command] [options]
```

### Commands

#### `login`

Authenticate with your Jama Connect server.

```bash
jama-connect login
```

#### `projects list`

List all accessible projects.

```bash
jama-connect projects list
```

**Output:**
```
ID       Key          Name
------------------------------------------------------------
1001     MYPROJ       My Project
1002     ARCH         Architecture
```

#### `items list`

List items within a project.

```bash
jama-connect items list --project <project-id>
jama-connect items list -p <project-id>
```

**Options:**

| Option           | Short | Required | Description                   |
|------------------|-------|----------|-------------------------------|
| `--project <id>` | `-p`  | Yes      | The numeric ID of the project |

**Output:**
```
ID       Document Key    Subject
------------------------------------------------------------
5001     REQ-001         The system shall support login
5002     REQ-002         The system shall encrypt data at rest
```

## Architecture

The solution follows Clean Architecture principles:

```
src/
├── JamaConnect.Domain/           # Core domain models and interfaces
│   ├── Models/                   # Project, Item, ItemStatus
│   └── Interfaces/               # IProjectService, IItemService, IAuthenticationService
│
├── JamaConnect.Application/      # Use cases / application logic
│   ├── Abstractions/             # IQueryHandler<TQuery,TResult>, ICommandHandler<TCommand>
│   ├── Projects/                 # GetProjectsQuery + Handler
│   ├── Items/                    # GetItemsQuery + Handler
│   └── Authentication/           # LoginCommand + Handler
│
├── JamaConnect.Infrastructure/   # External services implementation
│   ├── Authentication/           # OidcAuthenticationService (client_credentials flow)
│   ├── JamaConnect/              # JamaConnectClient (REST API client)
│   │   └── Dto/                  # JSON DTOs for API responses
│   ├── Options/                  # JamaConnectOptions (configuration binding)
│   └── Extensions/               # ServiceCollectionExtensions (DI registration)
│
└── JamaConnect.Cli/              # Console entry point (System.CommandLine)
    ├── Commands/                 # CLI command builders
    └── Program.cs                # Composition root

tests/
├── JamaConnect.Domain.Tests/       # Domain model unit tests
└── JamaConnect.Application.Tests/  # Application handler unit tests
```

## Development

### Build

```bash
dotnet build JamaConnect.slnx
```

### Test

```bash
dotnet test JamaConnect.slnx
```

### CI

GitHub Actions workflow runs on every push and pull request to `main`:
- Restores NuGet packages
- Builds in Release configuration (warnings treated as errors)
- Runs all unit tests

## License

[MIT](LICENSE)