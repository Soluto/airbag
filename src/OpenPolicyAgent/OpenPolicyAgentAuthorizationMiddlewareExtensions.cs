using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Airbag.OpenPolicyAgent.OpenPolicyAgentAuthorizationMiddlewareConfiguration;

namespace Airbag.OpenPolicyAgent
{
    static class OpenPolicyAgentAuthorizationMiddlewareExtensions
    {
        public static void UseOpenPolicyAgentAuthorizationMiddleware(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var middlewareConfig = GetOpaMiddlewareConfig(configuration);

            if (middlewareConfig.Mode != AuthorizationMode.Disabled)
            {
                app.UseMiddleware<OpenPolicyAgentAuthorizationMiddleware>(middlewareConfig);
            }
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