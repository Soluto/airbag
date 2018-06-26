using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Prometheus;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Airbag
{
    public class Program
    {
        private static IConfigurationRoot _configuration;

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddEnvironmentVariables();
            _configuration = builder.Build();
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
             .UseConfiguration(_configuration)
             .ConfigureMetricsWithDefaults(
                builder =>
                {
                    builder.Configuration.Configure(
                        options =>
                        {
                            options.Enabled = _configuration.GetValue<bool>("COLLECT_METRICS");
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
