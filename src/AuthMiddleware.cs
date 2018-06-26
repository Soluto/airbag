using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Airbag
{
    public static class AuthMiddleware
    {
        private static bool IsWhitelisted(HttpContext ctx, IEnumerable<string> whitelistedRoutes)
        {
            return ctx.Request.Path.HasValue &&
                   whitelistedRoutes.Any(r => string.Equals(r, ctx.Request.Path.Value, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAuthenticated(HttpContext ctx)
        {
            var result = ctx.User?.Identity?.IsAuthenticated;
            return result.HasValue && result.Value;
        }

        public static void UseAuthenticatedRoutes(this IApplicationBuilder app, IEnumerable<string> whitelistedRoutes)
        {
            app.Use(async (ctx, next) =>
            {
                if (IsAuthenticated(ctx) || IsWhitelisted(ctx, whitelistedRoutes))
                {
                    await next();
                }
                else
                {
                    ctx.Response.StatusCode = 403;
                }
            });
        }
    }
}