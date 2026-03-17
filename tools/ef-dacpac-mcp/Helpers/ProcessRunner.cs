using System.Diagnostics;

namespace EfDacpacMcp.Helpers;

internal record ProcessResult(int ExitCode, string Output, string Error)
{
    public bool Success => ExitCode == 0;

    public string Combined =>
        string.IsNullOrWhiteSpace(Error) ? Output : $"{Output}\n\nSTDERR:\n{Error}";
}

internal static class ProcessRunner
{
    internal static async Task<ProcessResult> RunAsync(
        string command,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(command, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (workingDirectory is not null)
            psi.WorkingDirectory = workingDirectory;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {command}");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();

        return new ProcessResult(process.ExitCode, output, error);
    }
}
