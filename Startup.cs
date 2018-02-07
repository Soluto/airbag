using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace airbag
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                {

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidIssuer = configuration.GetValue<string>("ISSUER"),
                        ValidateAudience = false
                    };
                    options.Authority = configuration.GetValue<string>("AUTHORITY");
                    options.RequireHttpsMetadata = true;
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.Use(async (ctx, next) =>
            {
                var result = ctx.User?.Identity?.IsAuthenticated;
                if (result.HasValue && result.Value)
                {
                    await next();
                }
                else
                {
                    ctx.Response.StatusCode = 403;
                }
            });

            app.RunProxy(new ProxyOptions
            {
                Scheme = "http",
                Host = configuration.GetValue<string>("BACKEND_HOST_NAME"),
                Port = configuration.GetValue<string>("BACKEND_SERVICE_PORT")
            });
        }
    }
}
