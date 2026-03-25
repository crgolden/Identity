namespace Identity.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;

/// <summary>
/// Measures the SHA-256 hashing used by GravatarService to derive avatar URLs.
/// Run with: dotnet run -c Release -- --filter *GravatarBenchmarks*
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[ExcludeFromCodeCoverage]
public class GravatarBenchmarks
{
    [Params("user@example.com", "  USER@Example.COM  ", "a.very.long.email.address+tag@subdomain.example.org")]
    public string Email { get; set; } = string.Empty;

    [Benchmark(Baseline = true)]
    public string ComputeHash_AllocatingConvert()
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Email.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [Benchmark]
    public string ComputeHash_StackAllocated()
    {
        var normalized = Email.Trim().ToLowerInvariant();
        var inputBytes = Encoding.UTF8.GetBytes(normalized);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(inputBytes, hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
