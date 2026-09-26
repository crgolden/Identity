namespace Identity.Tests.E2E.Load;

internal sealed record LoadProfile(string Path, int Requests, int Parallelism);
