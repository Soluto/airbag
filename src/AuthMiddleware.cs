using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Airbag
{
    public static class AuthMiddleware
    {
        private static async Task<bool> IsAuthenticated(HttpContext ctx, IEnumerable<string> authSchemas)
        {
            var results = await Task.WhenAll(authSchemas.Select(async schema =>
            {
                try
                {
                   return await ctx.AuthenticateAsync(schema);
                }
                catch
                {
                    return null;
                }
            }));

            var user = results.FirstOrDefault(res => res != null && res.Succeeded)?.Principal;

            ctx.Request.HttpContext.User = user;
            
            return user != null;
        }

        public static void UseAuthenticatedRoutes(this IApplicationBuilder app, IEnumerable<string> authSchemas)
        {
            app.Use(async (ctx, next) =>
            {
                if (!await IsAuthenticated(ctx, authSchemas))
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