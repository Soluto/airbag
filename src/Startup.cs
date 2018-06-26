using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Airbag
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var isDevEnv = string.Equals(_configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

            services.AddCors();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = _configuration.GetValue<string>("ISSUER"),
                        ValidAudience = _configuration.GetValue<string>("AUDIENCE"),
                        ValidateLifetime = true
                    };

                    options.Authority = _configuration.GetValue<string>("AUTHORITY");

                    // for testing
                    if (isDevEnv)
                    {
                        options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
                        options.RequireHttpsMetadata = false;
                    }
                });
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

            if (unauthenticatedConfig != null)
            {
                unauthenticatedRoutes = unauthenticatedConfig.Split(',');
            }

            app.UseAuthenticatedRoutes(unauthenticatedRoutes);

            app.RunProxy(new ProxyOptions
            {
                Scheme = "http",
                Host = _configuration.GetValue("BACKEND_HOST_NAME", "localhost"),
                Port = _configuration.GetValue("BACKEND_SERVICE_PORT", "80")
            });
        }
    }
}
