namespace Identity.Extensions;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static ArgumentNullException;
using static String;
using static System.Net.Mime.MediaTypeNames.Application;

/// <summary>Provides additional Identity endpoint registrations for passkey creation and request options.</summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>Maps the <c>/Account/PasskeyCreationOptions</c> and <c>/Account/PasskeyRequestOptions</c> POST endpoints.</summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to register endpoints on.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for the mapped account group.</returns>
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ThrowIfNull(endpoints);
        var accountGroup = endpoints.MapGroup("/Account");
        accountGroup.MapPost("/PasskeyCreationOptions", async (
            HttpContext context,
            [FromServices] UserManager<IdentityUser<Guid>> userManager,
            [FromServices] SignInManager<IdentityUser<Guid>> signInManager,
            [FromServices] IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var user = await userManager.GetUserAsync(context.User);
            if (user is null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var userId = await userManager.GetUserIdAsync(user);
            var userName = await userManager.GetUserNameAsync(user) ?? "User";
            var userEntity = new PasskeyUserEntity
            {
                Id = userId,
                Name = userName,
                DisplayName = userName
            };
            var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(userEntity);
            return TypedResults.Content(optionsJson, contentType: Json);
        });

        accountGroup.MapPost("/PasskeyRequestOptions", async (
            [FromServices] UserManager<IdentityUser<Guid>> userManager,
            [FromServices] SignInManager<IdentityUser<Guid>> signInManager,
            [FromQuery] string? username) =>
        {
            var user = IsNullOrWhiteSpace(username) ? null : await userManager.FindByNameAsync(username);
            var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
            return TypedResults.Content(optionsJson, contentType: Json);
        });

        return accountGroup;
    }
}