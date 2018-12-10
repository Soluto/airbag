using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Airbag
{
    public static class AuthMiddleware
    {
        private static bool UrlMatches(string pattern, string url) => Regex.IsMatch(url, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");

        private static bool IsWhitelisted(HttpContext ctx, IEnumerable<string> whitelistedRoutes) => ctx.Request.Path.HasValue && whitelistedRoutes.Any(whitelistedRoute => UrlMatches(whitelistedRoute, ctx.Request.Path.Value));

        private static async Task<bool> IsAuthenticated(HttpContext ctx, IEnumerable<string> authSchemas)
        {
            var results = await Task.WhenAll(authSchemas.Select(ctx.AuthenticateAsync));

            var user = results.FirstOrDefault(res => res.Succeeded)?.Principal;
            
            return user != null;
        }

        public static void UseAuthenticatedRoutes(this IApplicationBuilder app, IEnumerable<string> whitelistedRoutes, IEnumerable<string> authSchemas)
        {
            app.Use(async (ctx, next) =>
            {
                if (!await IsAuthenticated(ctx, authSchemas) && !IsWhitelisted(ctx, whitelistedRoutes))
                {
                    ctx.Response.StatusCode = 403;
                    return;
                }

                await next();
            });
        }
    }
}