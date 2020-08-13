﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using WB.Core.BoundedContexts.Headquarters;
using WB.UI.Headquarters.HealthChecks;

namespace WB.UI.Headquarters.Services.EmbeddedService
{
    public class ExportServiceEmbeddableHost : BackgroundService
    {
        private readonly IOptions<HeadquartersConfig> headquarterOptions;
        private readonly ILogger<ExportServiceEmbeddableHost> logger;
        private readonly EmbeddedExportServiceHealthCheck healthCheck;
        private readonly IConfiguration configuration;
        private readonly IServer server;

        public ExportServiceEmbeddableHost(
            IOptions<HeadquartersConfig> headquarterOptions,
            IConfiguration configuration,
            ILogger<ExportServiceEmbeddableHost> logger,
            EmbeddedExportServiceHealthCheck healthCheck,
            IServer server)
        {
            this.headquarterOptions = headquarterOptions;
            this.logger = logger;
            this.healthCheck = healthCheck;
            this.server = server;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var exportHostPath = configuration.GetPathToExportServiceHostDll();

            if (!System.IO.File.Exists(exportHostPath))
            {
                logger.LogInformation("WB.Services.Export.Host.exe is not found in {search}", exportHostPath);
                return;
            }

            var context = AssemblyLoadContext.Default;
            var resolver = new AssemblyDependencyResolver(exportHostPath);
            context.Resolving += (loadContext, name) =>
            {
                var assembly = resolver.ResolveAssemblyToPath(name);
                return assembly != null ? loadContext.LoadFromAssemblyPath(assembly) : null;
            };

            var exportHost = context.LoadFromAssemblyName(new AssemblyName("WB.Services.Export.Host"));

            var program = exportHost.GetType("WB.Services.Export.Host.Program");
            var createWebHostBuilder = program?.GetMethod("CreateWebHostBuilder");

            logger.LogInformation("Starting Export Service host");
            logger.LogDebug("Starting Export Service host at {exportHostPath}", exportHostPath);

            var exportArgs = new List<string>();

            if (!string.IsNullOrWhiteSpace(configuration["console"]))
            {
                exportArgs.Add("--console=" + configuration["console"]);
            }

            IHostBuilder exportHostBuilder = createWebHostBuilder?.Invoke(null, new object[] { exportArgs.ToArray(), false }) as IHostBuilder;

            if (exportHostBuilder == null)
            {
                logger.LogError("Unable to find IHostBuilder implementation at WB.Services.Export.Host.Program.CreateWebHostBuilder");
                return;
            }

            var serverUrl = server.Features.Get<IServerAddressesFeature>().Addresses.FirstOrDefault(ip => ip.Contains("127.0.0.1"));

            if (serverUrl == null)
            {
                serverUrl = server.Features.Get<IServerAddressesFeature>().Addresses.FirstOrDefault(ip => ip.Contains("localhost"));
            }

            logger.LogInformation("Configuring export service to use {serverUrl} as tenant url for {tenant}",
                serverUrl, this.headquarterOptions.Value.TenantName);

            exportHostBuilder.ConfigureAppConfiguration((ctx, builder) =>
            {
                ctx.Configuration["TenantUrlOverride:" + this.headquarterOptions.Value.TenantName] = serverUrl;
                ctx.Configuration.GetSection("ConnectionStrings")["DefaultConnection"] =
                    configuration.GetConnectionString("DefaultConnection");
            });

            exportHostBuilder.ConfigureWebHost(w =>
            {
                w.UseSetting("UseIISIntegration", true.ToString());
                w.UseKestrel(k => k.Listen(IPAddress.Loopback, 0));
                w.UseStartup(exportHost.GetType("WB.Services.Export.Host.Startup"));
            });

            var host = exportHostBuilder?.Build();

            var lifetime = host.Services.GetService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() =>
            {
                var exportServer = host.Services.GetRequiredService<IServer>();

                var addresses = exportServer.Features.Get<IServerAddressesFeature>().Addresses;
                configuration["DataExport:ExportServiceUrl"] = addresses.First();

                logger.LogInformation("Headquarters reconfigured to use {exportUrl} address for Export Service",
                    configuration["DataExport:ExportServiceUrl"]);
            });

            healthCheck.StartupTaskCompleted = true;

            try
            {
                await host.RunAsync(stoppingToken);
            }
            finally
            {
                healthCheck.StartupTaskCompleted = false;
            }
        }
    }
}