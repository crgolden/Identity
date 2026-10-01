namespace Identity.Tests.Load;

internal sealed record LoadProfile(string Path, int Requests, int Parallelism);
