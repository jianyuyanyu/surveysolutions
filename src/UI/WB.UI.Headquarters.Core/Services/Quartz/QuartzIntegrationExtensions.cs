﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Listener;
using Quartz.Logging;
using Quartz.Spi;
using Serilog;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.DataExport.Jobs;
using WB.Core.BoundedContexts.Headquarters.Invitations;
using WB.Core.BoundedContexts.Headquarters.QuartzIntegration;
using WB.Core.BoundedContexts.Headquarters.Synchronization.Schedulers.InterviewDetailsDataScheduler;
using WB.Core.BoundedContexts.Headquarters.Users.UserPreloading.Tasks;
using WB.Core.GenericSubdomains.Portable;
using WB.Infrastructure.Native;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Storage.Postgre.DbMigrations;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using WB.Infrastructure.Native.Utils;

namespace WB.UI.Headquarters.Services.Quartz
{
    public static class QuartzIntegrationExtensions
    {
        public static void AddQuartzIntegration(this IServiceCollection services,
            IConfiguration configuration,
            DbUpgradeSettings dbUpgradeSettings)
        {
            services.AddHostedService<QuartzMigrator>();

            services.Configure<QuartzMigratorConfig>(c =>
            {
                c.DbUpgradeSetting = dbUpgradeSettings;
            });

            services.AddQuartz(q =>
            {
                q.SchedulerId = Environment.MachineName + "-" + Process.GetCurrentProcess().Id;
                // q.SchedulerId = "AUTO"; - disabled because fail in prod environment on resolve DNS name

                q.SchedulerName = "Headquarters Background Services";
                
                q.UseJobFactory<AsyncScopedJobFactory>();
                q.UseDefaultThreadPool();

                q.UsePersistentStore(c =>
                {
                    c.UsePostgres(a =>
                    {
                        var connection = configuration.GetConnectionString("DefaultConnection");
                        var connectionBuilder = new NpgsqlConnectionStringBuilder(connection);
                        connectionBuilder.SetApplicationPostfix("quartz");

                        a.ConnectionString = connectionBuilder.ConnectionString;
                        a.TablePrefix = "quartz.";
                    });

                    c.UseProperties = true;
                    c.UseClustering();
                    c.UseJsonSerializer();
                });
            });

            global::Quartz.Logging.LogProvider.IsDisabled = false;//.SetCurrentLogProvider(new QuartzLogProvider());
          
            if (configuration["no-quartz"].ToBool(false) == false)
            {
                services.AddQuartzHostedService(q => { q.WaitForJobsToComplete = false; });
            }
        }

        public static IServiceCollection AddQuartzHostedService(
            this IServiceCollection services,
            Action<QuartzHostedServiceOptions> configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }

            return services.AddSingleton<IHostedService, HqQuartzHostedService>();
        }

        public static void RunQuartzMigrations(this IServiceProvider services, DbUpgradeSettings dbUpgradeSettings)
        {
            var migrationSettings = services.GetRequiredService<UnitOfWorkConnectionSettings>();

            DatabaseManagement.InitDatabase(migrationSettings.ConnectionString, "quartz");
            DbMigrationsRunner.MigrateToLatest(migrationSettings.ConnectionString, "quartz", dbUpgradeSettings,
                services.GetRequiredService<ILoggerProvider>());
        }

        public static async Task InitQuartzJobs(this IServiceProvider services)
        {
            var jobSetting = services.GetRequiredService<SyncPackagesProcessorBackgroundJobSetting>();
            
            await services.GetRequiredService<InterviewDetailsBackgroundSchedulerTask>()
                .Schedule(repeatIntervalInSeconds: jobSetting.SynchronizationInterval);
            await services.GetRequiredService<AssignmentsImportTask>().Schedule(repeatIntervalInSeconds: 300);
            await services.GetRequiredService<AssignmentsVerificationTask>().Schedule(repeatIntervalInSeconds: 300);
            await services.GetRequiredService<SendInvitationsTask>().ScheduleRunAsync();
            await services.GetRequiredService<SendRemindersTask>().Schedule(repeatIntervalInSeconds: 60 * 60);
            await services.GetRequiredService<SendInterviewCompletedTask>().Schedule(repeatIntervalInSeconds: 60);
            
            await services.GetRequiredService<ExportDataRetentionTask>().Schedule(repeatIntervalInSeconds: 60 * 60 * 6);
            
            var scheduler = await services.GetRequiredService<ISchedulerFactory>().GetScheduler();
            
            await scheduler.UnscheduleJob(new TriggerKey("Delete questionnaire trigger", "Delete questionnaire"));
            await scheduler.UnscheduleJob(new TriggerKey("Import trigger", "Import"));

            foreach (var schedule in services.GetServices<IScheduledJob>())
            {
                await schedule.RegisterJob();
            }

            var tiggers = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            foreach (var triggerKey in tiggers)
            {
                var triggerState = await scheduler.GetTriggerState(triggerKey);
                if (triggerState == TriggerState.Error)
                {
                    Log.Information($"Reseting Error state for trigger {triggerKey}.");
                    await scheduler.ResetTriggerFromErrorState(triggerKey);
                }
            }
        }

        private class QuartzMigratorConfig
        {
            public DbUpgradeSettings DbUpgradeSetting { get; set; }
        }

        private class QuartzMigrator : IHostedService
        {
            private readonly IServiceProvider serviceProvider;
            private readonly IOptions<QuartzMigratorConfig> schedulerConfig;

            public QuartzMigrator(IServiceProvider serviceProvider, IOptions<QuartzMigratorConfig> schedulerConfig)
            {
                this.serviceProvider = serviceProvider;
                this.schedulerConfig = schedulerConfig;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                this.serviceProvider.RunQuartzMigrations(schedulerConfig.Value.DbUpgradeSetting);
                await this.serviceProvider.InitQuartzJobs();
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
        
        private class QuartzLogProvider : ILogProvider
        {
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    Log.Error(exception, "QuartzLogProvider exception");
                    //var messageTemplate = "[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func();
                    //Log.Error(exception, messageTemplate, parameters);
                    return true;
                };
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
            {
                throw new NotImplementedException();
            }
        }
    }
}
