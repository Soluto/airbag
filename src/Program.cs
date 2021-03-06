﻿using System.IO;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Prometheus;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Airbag
{
    public class Program
    {
        private static IConfigurationRoot _configuration;

        public static void Main(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            _configuration = builder.Build();
            BuildWebHost(args).Run();
        }

        private static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(_configuration)
                .ConfigureMetricsWithDefaults(
                    builder =>
                    {
                        builder.Configuration.Configure(
                            options => { options.Enabled = _configuration.GetValue<bool>("COLLECT_METRICS"); });
                    })
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddConsole();
                    builder.AddFilter("Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler",
                        LogLevel.Warning);
                })
                .UseMetrics(options =>
                {
                    options.EndpointOptions = endpointsOptions =>
                    {
                        endpointsOptions.MetricsTextEndpointOutputFormatter =
                            new MetricsPrometheusTextOutputFormatter();
                        endpointsOptions.MetricsEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();
                    };
                })
                .UseStartup<Startup>()
                .UseKestrel(options => options.AllowSynchronousIO = true)
                .Build();
    }
}