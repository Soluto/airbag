using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Airbag
{
    public static class AuthMiddleware
    {
        private static async Task<bool> IsAuthenticated(HttpContext ctx, IEnumerable<string> authSchemas)
        {
            foreach (var shecma in authSchemas)
            {
                var res = await ctx.AuthenticateAsync(shecma);
                if (res != null && res.Succeeded)
                {
                    ctx.Request.HttpContext.User = res.Principal;
                    return true;
                }
            }
            return false;
        }

        public static void UseAuthenticatedRoutes(this IApplicationBuilder app)
        {
            var authSchemes = app.ApplicationServices.GetServices<Provider>().Select(provider => provider.Name);
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var validateRoutes = configuration.GetValue("AUTHORIZED_ROUTES_ENABLED", true);


            if (!validateRoutes) return;

            app.Use(async (ctx, next) =>
            {
                if (!await IsAuthenticated(ctx, authSchemes))
                {
                    ctx.Response.StatusCode = 403;
                    Console.WriteLine("Failed to authenticate");
                    return;
                }

                await next();
            });
        }
    }
}