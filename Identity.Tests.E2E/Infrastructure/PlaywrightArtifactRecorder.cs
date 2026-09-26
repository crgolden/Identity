namespace Identity.Tests.E2E.Infrastructure;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

public sealed class PlaywrightArtifactRecorder
{
    private static readonly ConcurrentDictionary<string, ConcurrentBag<PendingArtifact>> PendingArtifacts =
        new ConcurrentDictionary<string, ConcurrentBag<PendingArtifact>>();

    private readonly PlaywrightSettings _settings;
    private readonly string _appName;
    private readonly string _suiteName;
    private readonly string _testId;
    private readonly string _testName;
    private readonly string _artifactName;
    private readonly string _tempDirectory;
    private readonly string _finalDirectory;
    private readonly bool _belongsToTest;
    private readonly List<string> _events = new List<string>();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    private PlaywrightArtifactRecorder(PlaywrightSettings settings, string appName, string suiteName)
    {
        var test = TestContext.Current.Test;
        _settings = settings;
        _appName = appName;
        _suiteName = suiteName;
        _belongsToTest = test is not null;
        _testId = test?.UniqueID ?? $"unknown-{Guid.NewGuid():N}";
        _testName = test?.TestDisplayName ?? _testId;
        _artifactName = Sanitize(_testName, settings.MaxArtifactNameLength);

        var root = Path.Combine(AppContext.BaseDirectory, settings.TestResultsFolderName, settings.ArtifactsFolderName);
        var runFolderName = Guid.NewGuid().ToString("N");
        _tempDirectory = Path.Combine(root, settings.TempFolderName, _testId, runFolderName);
        _finalDirectory = Path.Combine(root, _suiteName, _artifactName);
        Directory.CreateDirectory(_tempDirectory);
    }

    public static async Task<(IAsyncDisposable Context, IPage Page)> CreateSessionAsync(
        IBrowser browser,
        string appName,
        string suiteName,
        BrowserNewContextOptions options,
        PlaywrightSettings settings)
    {
        ArgumentNullException.ThrowIfNull(browser);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(settings);

        var recorder = new PlaywrightArtifactRecorder(settings, appName, suiteName);
        options.RecordVideoDir = recorder._tempDirectory;
        options.RecordVideoSize = new RecordVideoSize
        {
            Width = settings.RecordedVideoWidth,
            Height = settings.RecordedVideoHeight,
        };

        var context = await browser.NewContextAsync(options).ConfigureAwait(false);
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        }).ConfigureAwait(false);

        var page = await context.NewPageAsync().ConfigureAwait(false);
        recorder.Attach(page);
        return (new PlaywrightArtifactSession(recorder, context, page), page);
    }

    public static void Clear(string testId)
    {
        if (PendingArtifacts.TryRemove(testId, out var artifacts))
        {
            foreach (var artifact in artifacts)
            {
                DeleteDirectory(artifact.TempDirectory);
            }
        }
    }

    public static void Finalize(string testId, TestResultState? state)
    {
        if (!PendingArtifacts.TryRemove(testId, out var artifacts))
        {
            return;
        }

        var failed = state?.Result == TestResult.Failed;
        foreach (var artifact in artifacts)
        {
            var tempParent = Path.GetDirectoryName(artifact.TempDirectory);
            if (!failed)
            {
                DeleteDirectory(artifact.TempDirectory);
                DeleteDirectoryIfEmpty(tempParent);
                continue;
            }

            Directory.CreateDirectory(artifact.FinalDirectory);
            var targetDirectory = Path.Combine(artifact.FinalDirectory, artifact.ContextId.ToString("N"));
            if (Directory.Exists(targetDirectory))
            {
                DeleteDirectory(targetDirectory);
            }

            Directory.Move(artifact.TempDirectory, targetDirectory);
            DeleteDirectoryIfEmpty(tempParent);
            WriteFailureMetadata(Path.Combine(targetDirectory, artifact.FailureFileName), state);
        }
    }

    public void Attach(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Console += (_, msg) => AddEvent($"{nameof(IPage.Console)} {msg.Type} {msg.Text}");
        page.PageError += (_, error) => AddEvent($"{nameof(IPage.PageError)} {error}");
        page.Request += (_, request) => AddEvent($"{nameof(IPage.Request)} {request.Method} {request.Url}");
        page.Response += (_, response) => AddEvent($"{nameof(IPage.Response)} {response.Status} {response.Url}");
        page.RequestFailed += (_, request) =>
            AddEvent($"{nameof(IPage.RequestFailed)} {request.Method} {request.Url} {request.Failure}");
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Artifact capture is diagnostics. Any failure to screenshot, trace or dispose is recorded in the browser log and must never fail or mask the test that was actually running.")]
    public async Task CompleteAsync(IBrowserContext context, IPage page)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(page);

        try
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(_tempDirectory, _settings.ScreenshotFileName),
                FullPage = true
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AddEvent($"{nameof(IPage.ScreenshotAsync)} {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            await context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(_tempDirectory, _settings.TraceFileName)
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AddEvent($"{nameof(ITracing.StopAsync)} {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AddEvent($"{nameof(IBrowserContext.DisposeAsync)} {ex.GetType().Name}: {ex.Message}");
        }

        string[] recordedEvents;
        lock (_events)
        {
            recordedEvents = _events.ToArray();
        }

        await File.WriteAllLinesAsync(Path.Combine(_tempDirectory, _settings.BrowserLogFileName), recordedEvents)
            .ConfigureAwait(false);
        await WriteMetadataAsync(Path.Combine(_tempDirectory, _settings.MetadataFileName)).ConfigureAwait(false);
        if (!_belongsToTest)
        {
            var parent = Path.GetDirectoryName(_tempDirectory);
            DeleteDirectory(_tempDirectory);
            DeleteDirectoryIfEmpty(parent);
            return;
        }

        var pendingArtifactId = Guid.NewGuid();
        PendingArtifacts.GetOrAdd(_testId, _ => new ConcurrentBag<PendingArtifact>()).Add(
            new PendingArtifact(_tempDirectory, _finalDirectory, pendingArtifactId, _settings.FailureFileName));
    }

    private static void WriteFailureMetadata(string path, TestResultState? state)
    {
        var payload = new
        {
            outcome = state?.Result.ToString(),
            executionTimeSeconds = state?.ExecutionTime,
            exceptionTypes = state?.ExceptionTypes,
            exceptionMessages = state?.ExceptionMessages,
            exceptionStackTraces = state?.ExceptionStackTraces,
            failureCause = state?.FailureCause?.ToString()
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions()));
    }

    private static JsonSerializerOptions JsonOptions() => new JsonSerializerOptions { WriteIndented = true };

    private static string Sanitize(string value, int maxLength)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars);
        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void DeleteDirectoryIfEmpty(string? directory)
    {
        if (directory is not null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }

    private void AddEvent(string message)
    {
        lock (_events)
        {
            _events.Add($"[{DateTimeOffset.UtcNow:O}] {message}");
        }
    }

    private async Task WriteMetadataAsync(string path)
    {
        var payload = new
        {
            app = _appName,
            suite = _suiteName,
            testId = _testId,
            testName = _testName,
            artifactName = _artifactName,
            startedAt = _startedAt,
            completedAt = DateTimeOffset.UtcNow,
            githubRunId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            githubRunAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT"),
            githubRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY"),
            githubSha = Environment.GetEnvironmentVariable("GITHUB_SHA"),
            githubRef = Environment.GetEnvironmentVariable("GITHUB_REF")
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions())).ConfigureAwait(false);
    }

    private sealed class PendingArtifact(string tempDirectory, string finalDirectory, Guid contextId, string failureFileName)
    {
        public string TempDirectory { get; } = tempDirectory;

        public string FinalDirectory { get; } = finalDirectory;

        public Guid ContextId { get; } = contextId;

        public string FailureFileName { get; } = failureFileName;
    }
}
