using System.ComponentModel;
using EfDacpacMcp.Helpers;
using ModelContextProtocol.Server;

namespace EfDacpacMcp.Tools;

[McpServerToolType]
public class EfCoreTools
{
    [McpServerTool(Name = "ef_list_migrations")]
    [Description(
        "Lists all EF Core migrations for a project, showing which have been applied " +
        "and which are pending. Requires dotnet-ef global tool (dotnet tool install -g dotnet-ef).")]
    public static async Task<string> ListMigrations(
        [Description("Path to the EF Core project (.csproj)")] string projectPath,
        [Description("Path to the startup project, if different from the EF project")] string? startupProject = null,
        [Description("DbContext class name, required when the project has more than one")] string? context = null,
        CancellationToken cancellationToken = default)
    {
        var args = BuildEfArgs("migrations list", projectPath, startupProject, context);
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"ef {args}",
            workingDirectory: null, cancellationToken);

        return result.Success
            ? result.Output
            : $"Error (exit {result.ExitCode}):\n{result.Combined}";
    }

    [McpServerTool(Name = "ef_migration_script")]
    [Description(
        "Generates an idempotent SQL script for all or a range of EF Core migrations. " +
        "Useful for reviewing what SQL will be applied to production.")]
    public static async Task<string> MigrationScript(
        [Description("Path to the EF Core project (.csproj)")] string projectPath,
        [Description("Starting migration name (exclusive). Omit to start from the very beginning.")] string? from = null,
        [Description("Ending migration name (inclusive). Omit to script through the latest.")] string? to = null,
        [Description("Path to the startup project, if different from the EF project")] string? startupProject = null,
        [Description("DbContext class name, required when the project has more than one")] string? context = null,
        CancellationToken cancellationToken = default)
    {
        var rangeArgs = (from, to) switch
        {
            (not null, not null) => $" --from {from} --to {to}",
            (not null, null)     => $" --from {from}",
            (null, not null)     => $" --to {to}",
            _                    => string.Empty,
        };

        var baseArgs = BuildEfArgs("migrations script --idempotent", projectPath, startupProject, context);
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"ef {baseArgs}{rangeArgs}",
            workingDirectory: null, cancellationToken);

        return result.Success
            ? result.Output
            : $"Error (exit {result.ExitCode}):\n{result.Combined}";
    }

    [McpServerTool(Name = "ef_dbcontext_info")]
    [Description(
        "Returns information about the configured DbContext: provider, data source, " +
        "migrations assembly, and whether a database exists.")]
    public static async Task<string> DbContextInfo(
        [Description("Path to the EF Core project (.csproj)")] string projectPath,
        [Description("Path to the startup project, if different from the EF project")] string? startupProject = null,
        [Description("DbContext class name, required when the project has more than one")] string? context = null,
        CancellationToken cancellationToken = default)
    {
        var args = BuildEfArgs("dbcontext info", projectPath, startupProject, context);
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"ef {args}",
            workingDirectory: null, cancellationToken);

        return result.Success
            ? result.Output
            : $"Error (exit {result.ExitCode}):\n{result.Combined}";
    }

    [McpServerTool(Name = "ef_dbcontext_list")]
    [Description("Lists all DbContext types discovered in the EF Core project.")]
    public static async Task<string> DbContextList(
        [Description("Path to the EF Core project (.csproj)")] string projectPath,
        [Description("Path to the startup project, if different from the EF project")] string? startupProject = null,
        CancellationToken cancellationToken = default)
    {
        var args = BuildEfArgs("dbcontext list", projectPath, startupProject, context: null);
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"ef {args}",
            workingDirectory: null, cancellationToken);

        return result.Success
            ? result.Output
            : $"Error (exit {result.ExitCode}):\n{result.Combined}";
    }

    // -------------------------------------------------------------------------

    private static string BuildEfArgs(
        string subCommand,
        string projectPath,
        string? startupProject,
        string? context)
    {
        var args = $"{subCommand} --project \"{projectPath}\"";

        if (startupProject is not null)
            args += $" --startup-project \"{startupProject}\"";

        if (context is not null)
            args += $" --context \"{context}\"";

        args += " --no-color";
        return args;
    }
}
