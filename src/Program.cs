using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private static IConfigurationRoot Configuration;

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddEnvironmentVariables();
            Configuration = builder.Build();
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
             .UseConfiguration(Configuration)
             .ConfigureMetricsWithDefaults(
                builder =>
                {
                    builder.Configuration.Configure(
                        options =>
                        {
                            options.Enabled = Configuration.GetValue<bool>("COLLECT_METRICS");
                        });
                })
                .UseMetrics(options =>
                {
                    options.EndpointOptions = endpointsOptions =>
                                {
                                    endpointsOptions.MetricsTextEndpointOutputFormatter = new MetricsPrometheusTextOutputFormatter();
                                    endpointsOptions.MetricsEndpointOutputFormatter = new MetricsPrometheusProtobufOutputFormatter();
                                };
                })
                .UseStartup<Startup>()
                .Build();
    }
}
