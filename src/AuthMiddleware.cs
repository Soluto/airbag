using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Airbag
{
    public static class AuthMiddleware
    {
        private static bool UrlMatches(string pattern, string url) => Regex.IsMatch(url, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");

        private static bool IsWhitelisted(HttpContext ctx, IEnumerable<string> whitelistedRoutes) => ctx.Request.Path.HasValue && whitelistedRoutes.Any(whitelistedRoute => UrlMatches(whitelistedRoute, ctx.Request.Path.Value));

        private static bool IsAuthenticated(HttpContext ctx)
        {
            var result = ctx.User?.Identity?.IsAuthenticated;
            return result.HasValue && result.Value;
        }

        public static void UseAuthenticatedRoutes(this IApplicationBuilder app, IEnumerable<string> whitelistedRoutes)
        {
            app.Use(async (ctx, next) =>
            {
                if (!IsAuthenticated(ctx) && !IsWhitelisted(ctx, whitelistedRoutes))
                {
                    ctx.Response.StatusCode = 403;
                    return;
                }

                await next();
            });
        }
    }
}