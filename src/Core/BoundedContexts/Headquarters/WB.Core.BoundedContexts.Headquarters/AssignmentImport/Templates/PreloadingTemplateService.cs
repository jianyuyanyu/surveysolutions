﻿using System;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Services.Export;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;

namespace WB.Core.BoundedContexts.Headquarters.AssignmentImport.Templates
{
    internal class AssignmentImportTemplateGenerator : IPreloadingTemplateService
    {
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly ITabularFormatExportService tabularFormatExportService;
        private readonly IArchiveUtils archiveUtils;
        private readonly IExportFileNameService exportFileNameService;
        private readonly ISampleUploadViewFactory sampleUploadViewFactory;


        private const string FolderName = "PreLoadingTemplates";
        private readonly string path;

        public AssignmentImportTemplateGenerator(
            IFileSystemAccessor fileSystemAccessor,
            string folderPath,
            ITabularFormatExportService tabularFormatExportService, 
            IArchiveUtils archiveUtils, 
            IExportFileNameService exportFileNameService,
            ISampleUploadViewFactory sampleUploadViewFactory)
        {
            this.fileSystemAccessor = fileSystemAccessor;
            this.tabularFormatExportService = tabularFormatExportService;
            this.archiveUtils = archiveUtils;
            this.exportFileNameService = exportFileNameService;
            this.sampleUploadViewFactory = sampleUploadViewFactory;
            this.path = fileSystemAccessor.CombinePath(folderPath, FolderName);
            if (!fileSystemAccessor.IsDirectoryExists(this.path))
                fileSystemAccessor.CreateDirectory(this.path);
        }

        public byte[] GetPrefilledPreloadingTemplateFile(Guid questionnaireId, long version)
        {
            var featuredQuestionItems = this.sampleUploadViewFactory.Load(new SampleUploadViewInputModel(questionnaireId, version)).ColumnListToPreload;

            byte[] templateFile = null;
            var csvConfiguration = new CsvConfiguration
            {
                HasHeaderRecord = true,
                TrimFields = true,
                IgnoreQuotes = false,
                Delimiter = "\t",
                WillThrowOnMissingField = false
            };

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                using (CsvWriter csvWriter = new CsvWriter(streamWriter, csvConfiguration))
                {
                    foreach (var questionItem in featuredQuestionItems)
                    {
                        csvWriter.WriteField(questionItem.Caption);
                    }
                    csvWriter.NextRecord();
                }

                templateFile = memoryStream.ToArray();
            }

            return templateFile;
        }

        public string GetFilePathToPreloadingTemplate(Guid questionnaireId, long version)
        {
            var dataDirectoryPath = this.GetFolderPathOfDataByQuestionnaire(questionnaireId, version);
            
            if (!this.fileSystemAccessor.IsDirectoryExists(dataDirectoryPath))
            {
                this.fileSystemAccessor.CreateDirectory(dataDirectoryPath);
            }

            var questionnaireIdentity = new QuestionnaireIdentity(questionnaireId, version);

            var archiveFilePath = this.fileSystemAccessor.CombinePath(this.path, this.exportFileNameService.GetFileNameForBatchUploadByQuestionnaire(questionnaireIdentity));

            if (this.fileSystemAccessor.IsFileExists(archiveFilePath))
                return archiveFilePath;

            this.tabularFormatExportService.CreateHeaderStructureForPreloadingForQuestionnaire(new QuestionnaireIdentity(questionnaireId, version), dataDirectoryPath);

            if (this.fileSystemAccessor.GetFilesInDirectory(dataDirectoryPath).Length == 0)
            {
                this.fileSystemAccessor.DeleteDirectory(dataDirectoryPath);
                return null;
            }

            this.archiveUtils.ZipDirectoryToFile(dataDirectoryPath, archiveFilePath);

            return archiveFilePath;
        }

        private string GetFolderPathOfDataByQuestionnaire(Guid questionnaireId, long version)
        {
            return this.fileSystemAccessor.CombinePath(this.path,
                this.fileSystemAccessor.MakeStataCompatibleFileName($"template_{questionnaireId.FormatGuid()}_v{version}"));
        }
    }
}