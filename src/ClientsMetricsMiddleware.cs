using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;

namespace Airbag
{
    public static class ClientsMetricsMiddleware
    {
        public static void AddClientIdMetric(this IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                var httpContextUser = ctx.Request.HttpContext.User;
                var adAppId = httpContextUser.Claims.FirstOrDefault(x => x.Type == "appid")?.Value;

                if (adAppId != null)
                {
                }

                await next();
            });
        }
    }
}