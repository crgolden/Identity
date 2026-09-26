namespace Identity.Extensions;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames.Application;
using static System.String;

public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointConventionBuilder MapAdditionalIdentityEndpoints()
        {
            var accountGroup = endpoints
                .MapGroup(PasskeyEndpoints.AccountGroupPrefix)
                .RequireRateLimiting(PasskeyEndpoints.RateLimiterPolicyName);
            accountGroup.MapPost(PasskeyEndpoints.CreationOptionsRoute, async (
                HttpContext context,
                [FromServices] UserManager<IdentityUser<Guid>> userManager,
                [FromServices] SignInManager<IdentityUser<Guid>> signInManager,
                [FromServices] IAntiforgery antiforgery) =>
            {
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.BadRequest();
                }

                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound(UserMessages.UnableToLoadUser(userManager.GetUserId(context.User)));
                }

                var userId = await userManager.GetUserIdAsync(user);
                var userName = await userManager.GetUserNameAsync(user) ?? PasskeyEndpoints.FallbackUserName;
                var userEntity = new PasskeyUserEntity
                {
                    Id = userId,
                    Name = userName,
                    DisplayName = userName
                };
                var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(userEntity);
                return TypedResults.Content(optionsJson, contentType: Json);
            });

            accountGroup.MapPost(PasskeyEndpoints.RequestOptionsRoute, async (
                HttpContext context,
                [FromServices] UserManager<IdentityUser<Guid>> userManager,
                [FromServices] SignInManager<IdentityUser<Guid>> signInManager,
                [FromServices] IAntiforgery antiforgery,
                [FromQuery] string? username) =>
            {
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.BadRequest();
                }

                var user = IsNullOrWhiteSpace(username) ? null : await userManager.FindByNameAsync(username);
                var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
                return TypedResults.Content(optionsJson, contentType: Json);
            });

            return accountGroup;
        }
    }
}
