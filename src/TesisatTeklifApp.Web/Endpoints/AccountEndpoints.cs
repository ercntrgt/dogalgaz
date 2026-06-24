using Microsoft.AspNetCore.Identity;
using TesisatTeklifApp.Infrastructure.Identity;

namespace TesisatTeklifApp.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/Account");

        group.MapPost("/Logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/Account/Login");
        });

        return endpoints;
    }
}
