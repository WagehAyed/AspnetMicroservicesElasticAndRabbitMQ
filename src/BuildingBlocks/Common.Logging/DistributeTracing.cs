using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Logging
{
    public static class DistributeTracingService
    {
        public static void Configure(this IServiceCollection services, string sourceName, string tracerServiceName)
        {
            services.AddOpenTelemetryTracing((builder) =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(tracerServiceName))
                    .AddHttpClientInstrumentation()
                    .AddSource(sourceName)
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = "localhost";
                        options.AgentPort = 16686;
                        options.ExportProcessorType = ExportProcessorType.Simple;
                    })
                    .AddConsoleExporter(options =>
                    {
                        options.Targets = OpenTelemetry.Exporter.ConsoleExporterOutputTargets.Console;
                    });
            });
        }
    }
}
