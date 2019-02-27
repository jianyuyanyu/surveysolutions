﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WB.Services.Export.Models;
using WB.Services.Export.Services.Processing;
using WB.Services.Infrastructure.Logging;
using WB.Services.Infrastructure.Tenant;
using WB.Services.Scheduler.Model;
using WB.Services.Scheduler.Services;

namespace WB.Services.Export.Host.Jobs
{
    class PostgresDataExportProcessesService : IDataExportProcessesService
    {
        private readonly IJobService jobService;
        private readonly IJobProgressReporter jobProgressReporter;
        private readonly ILogger<PostgresDataExportProcessesService> logger;

        public PostgresDataExportProcessesService(
            IJobService jobService,
            IJobProgressReporter jobProgressReporter,
            ILogger<PostgresDataExportProcessesService> logger)
        {
            this.jobService = jobService;
            this.jobProgressReporter = jobProgressReporter;
            this.logger = logger;
        }

        public async Task<long> AddDataExport(DataExportProcessArgs args)
        {
            using (LoggingHelpers.LogContext(("tenantName", args.ExportSettings.Tenant.Name)))
            {
                var job = await this.jobService.AddNewJobAsync(new JobItem
                {
                    Tenant = args.ExportSettings.Tenant.ToString(),
                    TenantName = args.ExportSettings.Tenant.Name,
                    Args = JsonConvert.SerializeObject(args),
                    Tag = args.NaturalId,
                    Type = ExportJobRunner.Name,
                    Data =
                    {
                        [StatusField] = DataExportStatus.Queued.ToString()
                    }
                });

                return job.Id;
            }
        }

        private DataExportProcessArgs AsDataExportProcessArgs(JobItem job)
        {
            var args = JsonConvert.DeserializeObject<DataExportProcessArgs>(job.Args);

            var eta = job.GetData<string>(EtaField);

            args.Status = new DataExportProcessStatus
            {
                ProgressInPercents = Int32.Parse(job.GetData<string>(ProgressField) ?? "0"),
                TimeEstimation = eta == null ? (TimeSpan?) null : TimeSpan.Parse(eta),
                BeginDate = job.StartAt,
                IsRunning = job.Status == JobStatus.Running || job.Status == JobStatus.Created,
                Status = Enum.Parse<DataExportStatus>(job.GetData<string>(StatusField))
            };

            return args;
        }

        public async Task<DataExportProcessArgs[]> GetAllProcesses(TenantInfo tenant)
        {
            var jobs = (await this.jobService.GetAllJobsAsync(tenant, JobStatus.Created, JobStatus.Running))
                .Select(AsDataExportProcessArgs).ToArray();

            return jobs;
        }
        
        public void UpdateDataExportProgress(long processId, int progressInPercents, TimeSpan estimatedTime = default)
        {
            logger.LogTrace("Update progress: {progressInPercents}%", progressInPercents);
            jobProgressReporter.UpdateJobData(processId, ProgressField, progressInPercents.ToString());

            if (estimatedTime != default)
            {
                jobProgressReporter.UpdateJobData(processId, EtaField, estimatedTime);
            }
        }

        public void DeleteDataExport(long processId, string reason)
        {
            jobProgressReporter.CancelJob(processId, reason);
        }

        public void ChangeStatusType(long processId, DataExportStatus status)
        {
            jobProgressReporter.UpdateJobData(processId, StatusField, status.ToString());
        }

        public const string StatusField = "exportStatus";
        public const string ProgressField = "progress";
        public const string EtaField = "eta";
    }
}