namespace Identity.Tests.E2E.Infrastructure;

using Microsoft.Playwright;

public sealed class Member(BrowserScenario scenario)
{
    private string? _email;
    private string? _password;
    private string? _previousEmail;
    private string? _previousPassword;

    public string Email => _email ?? throw new InvalidOperationException("No member was created for this scenario.");

    public string Password => _password ?? throw new InvalidOperationException("No member was created for this scenario.");

    public string PreviousEmail => _previousEmail ?? throw new InvalidOperationException("The member's email was never changed in this scenario.");

    public string PreviousPassword => _previousPassword ?? throw new InvalidOperationException("The member's password was never changed in this scenario.");

    public static async Task SubmitCredentialsAsync(IPage page, string email, string password)
    {
        ArgumentNullException.ThrowIfNull(page);
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("#login-submit");
    }

    public async Task CreateAsync()
    {
        (_email, _password) = await scenario.Fixture.CreateConfirmedUserAsync();
    }

    public async Task CreateAdministratorAsync()
    {
        (_email, _password) = await scenario.Fixture.CreateAdminUserAsync();
    }

    public void Remember(string email, string password)
    {
        _email = email;
        _password = password;
    }

    public void ChangePassword(string password)
    {
        _previousPassword = Password;
        _password = password;
    }

    public void ChangeEmail(string email)
    {
        _previousEmail = Email;
        _email = email;
    }

    public Task<Guid> GetIdAsync() => scenario.Fixture.GetUserIdAsync(Email);

    public Task SignInAsync() => SignInAsync(scenario.Page);

    public async Task SignInAsync(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        await page.GotoAsync(PageRoutes.Login);
        await SubmitCredentialsAsync(page, Password);
        await Assertions.Expect(page).Not.ToHaveURLAsync(UrlPatterns.Login());
    }

    public Task SubmitCredentialsAsync(IPage page, string password) => SubmitCredentialsAsync(page, Email, password);
}
