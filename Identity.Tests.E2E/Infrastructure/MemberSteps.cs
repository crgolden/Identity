namespace Identity.Tests.E2E.Infrastructure;

using Reqnroll;

[Binding]
public sealed class MemberSteps(Member member)
{
    [Given("a member with a confirmed account")]
    public Task GivenAMemberWithAConfirmedAccount() => member.CreateAsync();

    [Given("a signed-in member")]
    public async Task GivenASignedInMember()
    {
        await member.CreateAsync();
        await member.SignInAsync();
    }

    [Given("a signed-in administrator")]
    public async Task GivenASignedInAdministrator()
    {
        await member.CreateAdministratorAsync();
        await member.SignInAsync();
    }
}
