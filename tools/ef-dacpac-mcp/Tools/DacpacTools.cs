using System.ComponentModel;
using System.Text;
using System.Xml.Linq;
using EfDacpacMcp.Helpers;
using ModelContextProtocol.Server;

namespace EfDacpacMcp.Tools;

[McpServerToolType]
public class DacpacTools
{
    [McpServerTool(Name = "dacpac_build")]
    [Description(
        "Builds a .dacpac from a SQL Server Database Project (.sqlproj). " +
        "Requires the Microsoft.Build.Sql SDK (installed automatically via the .sqlproj).")]
    public static async Task<string> BuildDacpac(
        [Description("Path to the .sqlproj file")] string sqlprojPath,
        [Description("Build configuration: Debug or Release (default: Release)")] string configuration = "Release",
        CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(sqlprojPath));
        var result = await ProcessRunner.RunAsync(
            "dotnet", $"build \"{sqlprojPath}\" --configuration {configuration}",
            dir, cancellationToken);

        return result.Success
            ? result.Output
            : $"Error (exit {result.ExitCode}):\n{result.Combined}";
    }

    [McpServerTool(Name = "dacpac_deploy_report")]
    [Description(
        "Generates an XML deployment report showing exactly which schema changes sqlpackage " +
        "would apply to bring the target database in sync with the dacpac. " +
        "Use this to preview changes before deploying. " +
        "Requires sqlpackage (dotnet tool install -g microsoft.sqlpackage).")]
    public static async Task<string> DeployReport(
        [Description("Path to the .dacpac file")] string dacpacPath,
        [Description("ADO.NET connection string for the target SQL Server database")] string connectionString,
        [Description("Target database name")] string databaseName,
        CancellationToken cancellationToken = default)
    {
        var outputPath = TempFile("deploy-report", "xml");
        try
        {
            var args = BuildSqlPackageArgs(
                "DeployReport", dacpacPath, connectionString, databaseName,
                $"/OutputPath:\"{outputPath}\"");

            var result = await ProcessRunner.RunAsync("sqlpackage", args, cancellationToken: cancellationToken);

            if (!result.Success)
                return $"Error (exit {result.ExitCode}):\n{result.Combined}";

            return File.Exists(outputPath)
                ? await File.ReadAllTextAsync(outputPath, cancellationToken)
                : result.Output;
        }
        finally
        {
            TryDelete(outputPath);
        }
    }

    [McpServerTool(Name = "dacpac_drift_check")]
    [Description(
        "Checks for schema drift between the .dacpac (desired state) and the live database. " +
        "Returns a human-readable summary of differences; empty means the database matches the dacpac. " +
        "Requires sqlpackage (dotnet tool install -g microsoft.sqlpackage).")]
    public static async Task<string> DriftCheck(
        [Description("Path to the .dacpac file")] string dacpacPath,
        [Description("ADO.NET connection string for the target SQL Server database")] string connectionString,
        [Description("Target database name")] string databaseName,
        CancellationToken cancellationToken = default)
    {
        var outputPath = TempFile("drift-report", "xml");
        try
        {
            var args = BuildSqlPackageArgs(
                "DeployReport", dacpacPath, connectionString, databaseName,
                $"/OutputPath:\"{outputPath}\"");

            var result = await ProcessRunner.RunAsync("sqlpackage", args, cancellationToken: cancellationToken);

            if (!result.Success)
                return $"Error generating drift report (exit {result.ExitCode}):\n{result.Combined}";

            if (!File.Exists(outputPath))
                return result.Output;

            var xml = await File.ReadAllTextAsync(outputPath, cancellationToken);
            return SummarizeDriftReport(xml);
        }
        finally
        {
            TryDelete(outputPath);
        }
    }

    [McpServerTool(Name = "dacpac_script")]
    [Description(
        "Generates the full T-SQL deployment script that sqlpackage would execute to bring " +
        "the target database in sync with the dacpac. Ideal for pre-deploy code review. " +
        "Requires sqlpackage (dotnet tool install -g microsoft.sqlpackage).")]
    public static async Task<string> GenerateScript(
        [Description("Path to the .dacpac file")] string dacpacPath,
        [Description("ADO.NET connection string for the target SQL Server database")] string connectionString,
        [Description("Target database name")] string databaseName,
        CancellationToken cancellationToken = default)
    {
        var outputPath = TempFile("deploy-script", "sql");
        try
        {
            var args = BuildSqlPackageArgs(
                "Script", dacpacPath, connectionString, databaseName,
                $"/OutputPath:\"{outputPath}\"");

            var result = await ProcessRunner.RunAsync("sqlpackage", args, cancellationToken: cancellationToken);

            if (!result.Success)
                return $"Error (exit {result.ExitCode}):\n{result.Combined}";

            return File.Exists(outputPath)
                ? await File.ReadAllTextAsync(outputPath, cancellationToken)
                : result.Output;
        }
        finally
        {
            TryDelete(outputPath);
        }
    }

    // -------------------------------------------------------------------------

    private static string BuildSqlPackageArgs(
        string action,
        string dacpacPath,
        string connectionString,
        string databaseName,
        string extraArgs)
        => $"/Action:{action} " +
           $"/SourceFile:\"{dacpacPath}\" " +
           $"/TargetConnectionString:\"{connectionString}\" " +
           $"/TargetDatabaseName:\"{databaseName}\" " +
           extraArgs;

    private static string TempFile(string prefix, string extension)
        => Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}.{extension}");

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }

    private static string SummarizeDriftReport(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var operations = doc
                .Descendants(ns + "Operation")
                .Select(op => new
                {
                    Name  = op.Attribute("Name")?.Value ?? "Unknown",
                    Items = op.Elements(ns + "Item")
                              .Select(i => i.Attribute("Value")?.Value ?? i.Value)
                              .Where(v => !string.IsNullOrWhiteSpace(v))
                              .ToList(),
                })
                .Where(op => op.Items.Count > 0)
                .ToList();

            if (operations.Count == 0)
                return "No schema drift detected. The database matches the dacpac.";

            var sb = new StringBuilder();
            var totalChanges = operations.Sum(o => o.Items.Count);
            sb.AppendLine(
                $"Schema drift detected — {totalChanges} change(s) across " +
                $"{operations.Count} operation type(s):\n");

            foreach (var op in operations)
            {
                sb.AppendLine($"{op.Name} ({op.Items.Count})");
                foreach (var item in op.Items)
                    sb.AppendLine($"  - {item}");
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
        catch
        {
            // Return raw XML if the report format is unexpected
            return xml;
        }
    }
}
