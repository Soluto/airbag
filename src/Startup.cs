using System;
using System.Collections.Generic;
using System.Linq;
using Airbag.OpenPolicyAgent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RestEase;
using static Airbag.OpenPolicyAgent.OpenPolicyAgentAuthorizationMiddlewareConfiguration;

namespace Airbag
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IEnumerable<Provider> GetProviders()
        {
            var defaultProvider = new Provider
            {
                Issuer = _configuration.GetValue<string>("ISSUER"),
                Audience = _configuration.GetValue<string>("AUDIENCE"),
                Authority = _configuration.GetValue<string>("AUTHORITY"),
                Name = "DEFAULT"
            };

            var providers = _configuration.AsEnumerable()
                .Select(pair => pair.Key)
                .Where(key => key.StartsWith("ISSUER_") || key.StartsWith("AUDIENCE_") || key.StartsWith("AUTHORITY_"))
                .GroupBy(key => string.Join("_", key.Split('_').Skip(1)))
                .Select(grouping => new Provider
                {
                    Name = grouping.Key,
                    Issuer = _configuration.GetValue<string>("ISSUER_" + grouping.Key),
                    Audience = _configuration.GetValue<string>("AUDIENCE_" + grouping.Key),
                    Authority = _configuration.GetValue<string>("AUTHORITY_" + grouping.Key)
                })
                .ToList();

            if (!defaultProvider.IsEmpty())
            {
                providers.Add(defaultProvider);
            }

            if (providers.Any(provider => provider.IsInvalid()))
            {
                throw new Exception("Invalid auth provider configuration");
            }

            return providers;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var isDevEnv = string.Equals(_configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT"), "Development",
                StringComparison.OrdinalIgnoreCase);

            services.AddCors();

            var authenticationBuilder = services.AddAuthentication();

            foreach (var provider in GetProviders())
            {
                authenticationBuilder.AddJwtBearer(provider.Name, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = provider.Issuer,
                        ValidAudience = provider.Audience,
                        ValidateLifetime = true
                    };

                    options.Authority = provider.Authority;

                    // for testing
                    if (isDevEnv)
                    {
                        options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
                        options.RequireHttpsMetadata = false;
                    }
                });

                services.AddSingleton(s =>
                    RestClient.For<IOpenPolicyAgent>(
                        _configuration.GetValue<string>("OPA_URL", "http://localhost:8181")));
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();

            var unauthenticatedConfig = _configuration.GetValue<string>("UNAUTHENTICATED_ROUTES");
            IEnumerable<string> unauthenticatedRoutes = new List<string>();

            var opaModeRaw = _configuration.GetValue("OPA_MODE", "Disabled");

            if (!Enum.TryParse(opaModeRaw, out AuthorizationMode opaMode))
            {
                throw new Exception($"Invalid opa mode provided {opaModeRaw}");
            }

            var opaQuery = _configuration.GetValue("OPA_QUERY_PATH", string.Empty);
            var validateRoutes = _configuration.GetValue("AUTHORIZED_ROUTES_ENABLED", true);

            if (unauthenticatedConfig != null)
            {
                unauthenticatedRoutes = unauthenticatedConfig.Split(',');
            }

            if (validateRoutes)
            {
                app.UseAuthenticatedRoutes(unauthenticatedRoutes, GetProviders().Select(provider => provider.Name));
            }

            app.UseMiddleware<OpenPolicyAgentAuthorizationMiddleware>(
                new OpenPolicyAgentAuthorizationMiddlewareConfiguration
                {
                    Mode = opaMode,
                    QueryPath = opaQuery
                });

            app.RunProxy(new ProxyOptions
            {
                Scheme = "http",
                Host = _configuration.GetValue("BACKEND_HOST_NAME", "localhost"),
                Port = _configuration.GetValue("BACKEND_SERVICE_PORT", "80")
            });
        }
    }
}