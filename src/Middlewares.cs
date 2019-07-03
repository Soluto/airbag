using System.Linq;
using Airbag.OpenPolicyAgent;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Airbag.OpenPolicyAgent.OpenPolicyAgentAuthorizationMiddlewareConfiguration;

namespace Airbag
{
    public static class Middlewares
    {
        public static void UseAirbag(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();

            var validateRoutes = configuration.GetValue("AUTHORIZED_ROUTES_ENABLED", true);
            var authSchemes = app.ApplicationServices.GetServices<Provider>().Select(provider => provider.Name);

            if (validateRoutes)
            {
                app.UseAuthenticatedRoutes(authSchemes);
            }

            app.UseMiddleware<OpenPolicyAgentAuthorizationMiddleware>(GetOpaMiddlewareConfig(configuration));

            var proxyOptions = app.ApplicationServices.GetRequiredService<ProxyOptions>();
            app.RunProxy(proxyOptions);
        }

        private static OpenPolicyAgentAuthorizationMiddlewareConfiguration GetOpaMiddlewareConfig(IConfiguration configuration)
        {
            var opaMode = configuration.GetValue<AuthorizationMode>("OPA_MODE");
            var opaQuery = configuration.GetValue("OPA_QUERY_PATH", string.Empty);

            return new OpenPolicyAgentAuthorizationMiddlewareConfiguration
            {
                Mode = opaMode, 
                QueryPath = opaQuery
            };
        }
    }
}