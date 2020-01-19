using System;
using System.Linq;
using App.Metrics;
using App.Metrics.Counter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Airbag
{
    public static class ClientsMetricsMiddleware
    {
        public static void AddClientIdMetric(this IApplicationBuilder app)
        {
            var metrics = app.ApplicationServices.GetService<IMetrics>();

            app.Use(async (ctx, next) =>
            {
                var httpContextUser = ctx.Request.HttpContext.User;
                var adAppId = httpContextUser.Claims.FirstOrDefault(x => x.Type == "appid")?.Value;
                var audience = httpContextUser.Claims.FirstOrDefault(x => x.Type == "aud")?.Value;

                if (audience != null)
                {
                    metrics.Measure.Counter.Increment(new CounterOptions()
                    {
                        Name = "client_authenticated_successfully",
                        Tags = new MetricTags(new[] {"ad_app_id", "audience"}, new[] {adAppId ?? "unknown", audience})
                    });
                }

                await next();
            });
        }
    }
}