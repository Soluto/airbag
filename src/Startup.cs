using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Airbag.OpenPolicyAgent;
using Airbag.Utils;
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
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            _configuration = builder.Build();
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
                .Where(key => key.StartsWith("ISSUER_") || key.StartsWith("AUDIENCE_") || key.StartsWith("AUTHORITY_") || key.StartsWith("VALIDATE_AUDIENCE_"))
                .GroupBy(key => string.Join("_", key.Split('_').Last()))
                .Select(grouping => new Provider
                {
                    Name = grouping.Key,
                    Issuer = _configuration.GetValue<string>("ISSUER_" + grouping.Key),
                    ValidateAudience = bool.Parse(_configuration.GetValue<string>("VALIDATE_AUDIENCE_" + grouping.Key, "true")),
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
                        ValidateAudience = provider.ValidateAudience,
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

                services.AddSingleton(provider);
            }

            services.AddSingleton(s =>
                RestClient.For<IOpenPolicyAgent>(
                    _configuration.GetValue("OPA_URL", "http://localhost:8181")));

            var whitelistedRoutes = _configuration.GetValue("UNAUTHENTICATED_ROUTES", string.Empty).Split(',');
            services.AddSingleton(new RouteWhitelistMatcher(whitelistedRoutes));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var proxyOptions = new ProxyOptions
            {
                Scheme = "http",
                Host = _configuration.GetValue("BACKEND_HOST_NAME", "localhost"),
                Port = _configuration.GetValue("BACKEND_SERVICE_PORT", "80")
            };

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();

            var routeWhitelistMatcher = app.ApplicationServices.GetRequiredService<RouteWhitelistMatcher>();
            app.MapWhen(context => !routeWhitelistMatcher.IsMatch(context.Request.Path), nonWhitelistedPath =>
            {
                nonWhitelistedPath.UseAuthenticatedRoutes();
                nonWhitelistedPath.UseOpenPolicyAgentAuthorizationMiddleware();
                nonWhitelistedPath.RunProxy(proxyOptions);
            });

            app.RunProxy(proxyOptions);
        }
    }
}