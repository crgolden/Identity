# ef-dacpac-mcp

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server that exposes EF Core migration management and SQL Server DACPAC schema-drift analysis as tools for AI assistants.

Designed for .NET solutions that use **EF Core for development** and **DACPAC (`.sqlproj`) for production** deployments — the dual-track strategy where drift between the two is the biggest source of "works locally, breaks in prod" issues.

## Tools

### EF Core (`dotnet-ef`)

| Tool | Description |
|---|---|
| `ef_list_migrations` | Lists all migrations, showing which are applied and which are pending |
| `ef_migration_script` | Generates an idempotent SQL script for a migration range |
| `ef_dbcontext_info` | Shows provider, connection string source, and migrations assembly |
| `ef_dbcontext_list` | Lists all DbContext types in the project |

### DACPAC (`sqlpackage`)

| Tool | Description |
|---|---|
| `dacpac_build` | Builds a `.dacpac` from a `.sqlproj` |
| `dacpac_deploy_report` | XML report of all changes sqlpackage would apply |
| `dacpac_drift_check` | Human-readable summary of schema drift between the dacpac and a live database |
| `dacpac_script` | Full T-SQL deployment script for pre-deploy review |

## Prerequisites

```bash
# EF Core tools
dotnet tool install -g dotnet-ef

# sqlpackage
dotnet tool install -g microsoft.sqlpackage
```

## Installation

### Build and run locally

```bash
git clone <repo>
cd ef-dacpac-mcp
dotnet run
```

### Configure in Claude Code

Add to `.mcp.json` at your repo root:

```json
{
  "mcpServers": {
    "ef-dacpac-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/ef-dacpac-mcp.csproj", "--no-launch-profile"]
    }
  }
}
```

For faster startup after an initial publish:

```bash
dotnet publish -c Release -o ./publish
```

```json
{
  "mcpServers": {
    "ef-dacpac-mcp": {
      "command": "./publish/ef-dacpac-mcp"
    }
  }
}
```

## Requirements

- .NET 9 SDK
- `dotnet-ef` global tool (for EF Core tools)
- `sqlpackage` global tool (for DACPAC tools)

## License

MIT
