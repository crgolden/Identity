using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Identity;

/// <summary>
/// Measures the cost of ASP.NET Core Identity's PBKDF2-based password hashing.
/// Run with: dotnet run -c Release -- --filter *AuthenticationBenchmarks*
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class AuthenticationBenchmarks
{
    private readonly PasswordHasher<IdentityUser<Guid>> _hasher = new();
    private readonly IdentityUser<Guid> _user = new();
    private string _hash = string.Empty;

    [Params("Short1!", "ALongerPassword@123456!", "AVeryl0ngP@ssw0rdWithManyCharacters!")]
    public string Password { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _hash = _hasher.HashPassword(_user, Password);
    }

    [Benchmark]
    public string HashPassword() => _hasher.HashPassword(_user, Password);

    [Benchmark]
    public PasswordVerificationResult VerifyHashedPassword() =>
        _hasher.VerifyHashedPassword(_user, _hash, Password);
}
