# Jama Connect CLI — Reference

## Overview

`jama-connect` is a cross-platform .NET 9 CLI that authenticates via OAuth 2.0 client credentials flow and lets you browse and interact with Jama Connect projects and items from a terminal.

---

## Global Options

| Option      | Description                         |
|-------------|-------------------------------------|
| `--help`    | Display help for any command        |
| `--version` | Display the tool version            |

---

## Commands

### `login`

Authenticates against the Jama Connect server using configured client credentials.

```bash
jama-connect login
```

On success, the tool prints:

```
Successfully authenticated with Jama Connect.
```

The access token is held in memory for the lifetime of the process. Re-authentication occurs automatically when the token is within 60 seconds of expiry.

---

### `projects`

Parent command for project-related sub-commands.

#### `projects list`

Lists all projects accessible with the configured credentials.

```bash
jama-connect projects list
```

**Example output:**

```
ID       Key          Name
------------------------------------------------------------
1001     MYPROJ       My Project
1002     ARCH         Architecture Documentation
1003                  (folder)
```

---

### `items`

Parent command for item-related sub-commands.

#### `items list`

Lists all items in the specified project.

```bash
jama-connect items list --project <id>
jama-connect items list -p <id>
```

**Options:**

| Option           | Alias | Required | Type | Description                  |
|------------------|-------|----------|------|------------------------------|
| `--project <id>` | `-p`  | Yes      | int  | Numeric project ID to query  |

**Example:**

```bash
jama-connect items list -p 1001
```

**Example output:**

```
ID       Document Key    Subject
------------------------------------------------------------
5001     REQ-001         The system shall authenticate users
5002     REQ-002         The system shall log all API calls
5003     TC-001          Verify login with valid credentials
```

---

## Configuration

Configuration is resolved in this order (later sources override earlier ones):

1. `appsettings.json` (next to the executable)
2. Environment variables prefixed with `JAMA_`

### `appsettings.json` schema

```json
{
  "JamaConnect": {
    "BaseUrl": "https://your-instance.jamacloud.com",
    "ClientId": "your-oauth-client-id",
    "ClientSecret": "your-oauth-client-secret",
    "TokenEndpoint": "/rest/oauth/token",
    "TimeoutSeconds": 30
  }
}
```

| Key              | Required | Default               | Description                              |
|------------------|----------|-----------------------|------------------------------------------|
| `BaseUrl`        | Yes      | —                     | HTTPS base URL of your Jama Connect host |
| `ClientId`       | Yes      | —                     | OAuth 2.0 client ID                      |
| `ClientSecret`   | Yes      | —                     | OAuth 2.0 client secret                  |
| `TokenEndpoint`  | No       | `/rest/oauth/token`   | Path to the token endpoint               |
| `TimeoutSeconds` | No       | `30`                  | HTTP request timeout in seconds          |

### Environment variable mapping

The `JAMA_` prefix maps to the `JamaConnect` configuration section using double-underscore as a hierarchy separator:

| Environment Variable               | Equivalent JSON key                      |
|------------------------------------|------------------------------------------|
| `JAMA_JamaConnect__BaseUrl`        | `JamaConnect.BaseUrl`                    |
| `JAMA_JamaConnect__ClientId`       | `JamaConnect.ClientId`                   |
| `JAMA_JamaConnect__ClientSecret`   | `JamaConnect.ClientSecret`               |
| `JAMA_JamaConnect__TokenEndpoint`  | `JamaConnect.TokenEndpoint`              |
| `JAMA_JamaConnect__TimeoutSeconds` | `JamaConnect.TimeoutSeconds`             |

---

## Authentication Flow

The CLI uses the **OAuth 2.0 Client Credentials** grant type. On every API call, the `OidcAuthenticationService` checks whether the cached token is still valid (with a 60-second buffer). If it has expired (or is not yet acquired), a new token is fetched automatically before the request proceeds.

```
CLI command
    └─► Handler (Application layer)
          └─► JamaConnectClient (Infrastructure layer)
                ├─► OidcAuthenticationService.GetAccessTokenAsync()
                │     ├─ Token valid? → return cached token
                │     └─ Token expired/missing? → POST /rest/oauth/token → cache & return
                └─► GET /rest/v1/... (with Bearer token)
```

---

## Exit Codes

| Code | Meaning                         |
|------|---------------------------------|
| `0`  | Success                         |
| `1`  | Parse error / unknown command   |
| Non-zero | Unhandled exception        |

---

## Building from Source

```bash
# Clone
git clone https://github.com/Tjeerd-menno/jama-connect-cli.git
cd jama-connect-cli

# Restore
dotnet restore JamaConnect.slnx

# Build (Release, warnings as errors)
dotnet build JamaConnect.slnx --configuration Release

# Test
dotnet test JamaConnect.slnx

# Publish self-contained executable (Linux x64)
dotnet publish src/JamaConnect.Cli \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  --output ./dist
```

---

## REST API Reference

The CLI wraps the following Jama Connect REST v1 endpoints:

| CLI Command          | HTTP Method | Endpoint                        |
|----------------------|-------------|---------------------------------|
| `projects list`      | `GET`       | `/rest/v1/projects`             |
| `items list -p <id>` | `GET`       | `/rest/v1/items?project=<id>`   |
| `login`              | `POST`      | `/rest/oauth/token`             |
