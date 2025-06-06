using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Ncqrs;
using WB.Core.BoundedContexts.Designer.Classifications;
using WB.Core.BoundedContexts.Designer.CodeGenerationV2;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Attachments;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Categories;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Group;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.LookupTables;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Macros;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.StaticText;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Translations;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Variable;
using WB.Core.BoundedContexts.Designer.DataAccess;
using WB.Core.BoundedContexts.Designer.Implementation.Repositories;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.AttachmentService;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Implementation.Services.LookupTableService;
using WB.Core.BoundedContexts.Designer.Implementation.Services.QuestionnairePostProcessors;
using WB.Core.BoundedContexts.Designer.Implementation.Services.Revisions;
using WB.Core.BoundedContexts.Designer.MembershipProvider;
using WB.Core.BoundedContexts.Designer.QuestionnaireCompilationForOldVersions;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Translations;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Verifier;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionnaireInfo;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireList;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.SharedPersons;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.Implementation;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.TopologicalSorter;
using WB.Core.SharedKernels.NonConficltingNamespace;
using WB.Core.SharedKernels.Questionnaire.Translations;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Infrastructure.Native.Files.Implementation.FileSystem;
using WB.Infrastructure.Native.Questionnaire;
using WB.Infrastructure.Native.Storage;
using WB.UI.Designer.Models;
using Questionnaire = WB.Core.BoundedContexts.Designer.Aggregates.Questionnaire;
using QuestionnaireVerifier = WB.Core.BoundedContexts.Designer.Verifier.QuestionnaireVerifier;
using QuestionnaireView = WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionnaireView;
using Translation = WB.Core.SharedKernels.SurveySolutions.Documents.Translation;
using TranslationInstance = WB.Core.BoundedContexts.Designer.Translations.TranslationInstance;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.HttpServices.Services;
using WB.Core.SharedKernels.Questionnaire.ReusableCategories;
using WB.Core.SharedKernels.SurveySolutions.ReusableCategories;

namespace WB.Tests.Unit.Designer
{
    internal static partial class Create
    {
        public static DesignerIdentityUser AccountDocument(string userName = "", Guid? userId = null)
            => new DesignerIdentityUser
            {
                Id = userId ?? Guid.NewGuid(),
                UserName = userName,
            };

        public static Answer Answer(string answer = "answer option", decimal? value = null, string stringValue = null, decimal? parentValue = null)
        {
            return new Answer()
            {
                AnswerText = answer,
                AnswerValue = stringValue ?? value.ToString(),
                ParentValue = parentValue.HasValue ? parentValue.ToString() : null
            };
        }

        public static Attachment Attachment(Guid? attachmentId = null, string name = "attachment", string contentId = "content id")
        {
            return new Attachment
            {
                AttachmentId = attachmentId ?? Guid.NewGuid(),
                Name = name,
                ContentId = contentId
            };
        }

        public static AttachmentContent AttachmentContent(byte[] content = null, string contentType = null, string contentId = null,  long? size = null, AttachmentDetails details = null)
        {
            return new AttachmentContent
            {
                ContentId = contentId,
                Content = content ?? new byte[0],
                ContentType = contentType ?? "whatever",
                Size = size ?? 10,
                Details = details
            };
        }

        public static AttachmentMeta AttachmentMeta(
            Guid attachmentId,
            string contentHash,
            Guid questionnaireId,
            string fileName = null,
            DateTime? lastUpdateDate = null)
        {
            return new AttachmentMeta
            {
                AttachmentId = attachmentId,
                ContentId = contentHash,
                QuestionnaireId = questionnaireId,
                FileName = fileName ?? "fileName.txt",
                LastUpdateDate = lastUpdateDate ?? DateTime.UtcNow
            };
        }

        public static AttachmentSize AttachmentSize(long? size = null)
        {
            return new AttachmentSize
            {
                Size = size ?? 10
            };
        }

        public static AttachmentView AttachmentView(Guid? id = null, long? size = null)
        {
            return new AttachmentView
            (
                attachmentId : (id ?? Guid.NewGuid()).FormatGuid(),
                name: "Test",
                meta : new AttachmentMeta { AttachmentId = id ?? Guid.NewGuid() },
                content : new AttachmentContent
                {
                    Size = size ?? 10
                }
            );
        }

        public static Group Chapter(string title = "Chapter X", Guid? chapterId = null, bool hideIfDisabled = false, IEnumerable<IComposite> children = null)
        {
            return Create.Group(groupId: chapterId, title: title, hideIfDisabled: hideIfDisabled, children: children);
        }

        public static Group Section(string title = "Section X", Guid? sectionId = null, IEnumerable<IComposite> children = null)
            => Create.Group(
                title: title,
                groupId: sectionId,
                children: children);

        public static Group Subsection(string title = "Subsection X", Guid? sectionId = null, IEnumerable<IComposite> children = null)
            => Create.Group(
                title: title,
                groupId: sectionId,
                children: children);

        public static CodeGeneratorV2 CodeGeneratorV2()
        {
            return new CodeGeneratorV2(new CodeGenerationModelsFactory(
                DefaultMacrosSubstitutionService(),
                new QuestionTypeToCSharpTypeMapper()));
        }

        public static QuestionProperties QuestionProperties(bool? isCritical = null)
        {
            return new QuestionProperties(false, false)
            {
                IsCritical = isCritical ?? false
            };
        }

        public static DateTimeQuestion DateTimeQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string variable = null, string validationMessage = null, string text = null, QuestionScope scope = QuestionScope.Interviewer,
            bool preFilled = false, bool hideIfDisabled = false, bool isCurrentTime = false)
        {
            return new DateTimeQuestion()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                QuestionText = text,
                StataExportCaption = variable,
                QuestionScope = scope,
                Featured = preFilled,
                IsTimestamp = isCurrentTime,
            };
        }

        public static IMacrosSubstitutionService DefaultMacrosSubstitutionService()
        {
            var macrosSubstitutionServiceMock = new Mock<IMacrosSubstitutionService>();
            macrosSubstitutionServiceMock.Setup(
                x => x.InlineMacros(It.IsAny<string>(), It.IsAny<IEnumerable<Macro>>()))
                .Returns((string e, IEnumerable<Macro> macros) =>
                {
                    return e;
                });

            return macrosSubstitutionServiceMock.Object;
        }

        public static IDesignerEngineVersionService DesignerEngineVersionService()
        {
            return new DesignerEngineVersionService(Mock.Of<IAttachmentService>(), 
                Mock.Of<IDesignerTranslationService>(), 
                Mock.Of<IReusableCategoriesService>());
        }

        public static FixedRosterTitle FixedRosterTitle(decimal value, string title)
        {
            return new FixedRosterTitle(value, title);
        }

        public static GenerationResult GenerationResult(bool success = false)
        {
            return new GenerationResult( success : success , new Diagnostic[0]);
        }


        private static Guid GetQuestionnaireItemId(string questionnaireItemId)
        {
            return string.IsNullOrEmpty(questionnaireItemId) ? Guid.NewGuid() : Guid.Parse(questionnaireItemId);
        }

        private static Guid? GetQuestionnaireItemParentId(string questionnaireItemParentId)
        {
            return string.IsNullOrEmpty(questionnaireItemParentId)
                ? (Guid?)null
                : Guid.Parse(questionnaireItemParentId);
        }

        public static GpsCoordinateQuestion GpsCoordinateQuestion(Guid? questionId = null, string variable = "var1", bool isPrefilled = false, string title = "test test test",
            string enablementCondition = null, string validationExpression = null, bool hideIfDisabled = false)
        {
            return new GpsCoordinateQuestion()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = variable,
                Featured = isPrefilled,
                QuestionText = title,
                ValidationExpression = validationExpression,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
            };
        }

        public static Group Group(
            Guid? groupId = null,
            string title = "Group X",
            string variable = null,
            string enablementCondition = null,
            bool hideIfDisabled = false,
            IEnumerable<IComposite> children = null,
            Guid? rosterSizeQuestionId = null)
        {
            return new Group(title)
            {
                PublicKey = groupId ?? Guid.NewGuid(),
                VariableName = variable,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                Children = children?.ToReadOnlyCollection() ?? new ReadOnlyCollection<IComposite>(new List<IComposite>()),
                RosterSizeQuestionId = rosterSizeQuestionId,
            };
        }

        public static KeywordsProvider KeywordsProvider()
        {
            return new KeywordsProvider(Create.SubstitutionService());
        }

        public static LookupTable LookupTable(string tableName, string fileName = null)
        {
            return new LookupTable
            {
                TableName = tableName,
                FileName = fileName ?? "lookup.tab"
            };
        }

        public static LookupTableContent LookupTableContent(string[] variableNames, params LookupTableRow[] rows)
        {
            return new LookupTableContent
            (
                variableNames : variableNames,
                rows : rows
            );
        }

        public static LookupTableRow LookupTableRow(long rowcode, decimal?[] values)
        {
            return new LookupTableRow
            {
                RowCode = rowcode,
                Variables = values
            };
        }

        public static LookupTableService LookupTableService(
            IPlainKeyValueStorage<LookupTableContent> lookupTableContentStorage = null,
            IDesignerQuestionnaireStorage documentStorage = null)
        {
            return new LookupTableService(
                lookupTableContentStorage ?? Mock.Of<IPlainKeyValueStorage<LookupTableContent>>(),
                documentStorage ?? Mock.Of<IDesignerQuestionnaireStorage>());
        }

        public static Macro Macro(string name, string content = null, string description = null)
        {
            return new Macro
            {
                Name = name,
                Content = content,
                Description = description
            };
        }

        public static MacrosSubstitutionService MacrosSubstitutionService()
            => new MacrosSubstitutionService();


        public static MultimediaQuestion MultimediaQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string variable = null, string validationMessage = null, string title = "test", QuestionScope scope = QuestionScope.Interviewer
            , bool hideIfDisabled = false, bool isSignature = false)
        {
            return new MultimediaQuestion()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = variable,
                QuestionScope = scope,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                QuestionText = title,
                IsSignature = isSignature
            };
        }

        public static IMultyOptionsQuestion MultipleOptionsQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            bool areAnswersOrdered = false, int? maxAllowedAnswers = null, Guid? linkedToQuestionId = null, bool isYesNo = false, bool hideIfDisabled = false, List<Answer> answersList = null,
            string title = "test",
            bool isCombobox = false,
            string optionsFilterExpression = null,
            params decimal[] answers)
        {
            var publicKey = questionId ?? Guid.NewGuid();
            var multipleOptionsQuestion = new MultyOptionsQuestion("Question MO")
            {
                PublicKey = publicKey,
                StataExportCaption = GetNameForEntity("multi_option", publicKey),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                AreAnswersOrdered = areAnswersOrdered,
                MaxAllowedAnswers = maxAllowedAnswers,
                LinkedToQuestionId = linkedToQuestionId,
                YesNoView = isYesNo,
                Answers = answersList ?? answers.Select(a => Create.Answer(a.ToString(), a)).ToList(),
                QuestionText = title,
                IsFilteredCombobox = isCombobox,
            };
            multipleOptionsQuestion.Properties.OptionsFilterExpression = optionsFilterExpression;
            return multipleOptionsQuestion;
        }

        public static MultyOptionsQuestion MultyOptionsQuestion(Guid? id = null,
            IEnumerable<Answer> options = null, Guid? linkedToQuestionId = null, 
            string variable = null, 
            bool yesNoView = false,
            string enablementCondition = null, string validationExpression = null, Guid? linkedToRosterId = null, string optionsFilterExpression = null,
            int? maxAllowedAnswers = null, string title = "test", bool featured = false,
            bool? filteredCombobox = null,
            string linkedFilterExpression = null,
            Guid? categoriesId = null)
        {
            return new MultyOptionsQuestion
            {
                PublicKey = id ?? Guid.NewGuid(),
                Answers = linkedToQuestionId.HasValue ? null : new List<Answer>(options ?? new Answer[] { }),
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                StataExportCaption = variable,
                YesNoView = yesNoView,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                QuestionText = title,
                MaxAllowedAnswers = maxAllowedAnswers,
                Featured = featured,
                IsFilteredCombobox = filteredCombobox,
                LinkedFilterExpression = linkedFilterExpression,
                CategoriesId = categoriesId,
                Properties = new QuestionProperties(false, true)
                {
                    OptionsFilterExpression = optionsFilterExpression
                }
            };
        }

        public static NumericQuestion NumericIntegerQuestion(Guid? id = null, string variable = null, string enablementCondition = null,
            string validationExpression = null, QuestionScope scope = QuestionScope.Interviewer, bool isPrefilled = false,
            bool hideIfDisabled = false, IEnumerable<ValidationCondition> validationConditions = null, Guid? linkedToRosterId = null,
            string title = "test", string variableLabel = null, Option[] options = null, bool? isCritical = null)
        {
            var publicKey = id ?? Guid.NewGuid();
            var stataExportCaption = variable ?? "numeric_question"+publicKey;
            return new NumericQuestion
            {
                PublicKey = publicKey,
                StataExportCaption = stataExportCaption,
                IsInteger = true,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                QuestionScope = scope,
                Featured = isPrefilled,
                ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                LinkedToRosterId = linkedToRosterId,
                QuestionText = title,
                VariableLabel = variableLabel,
                Answers = options != null
                    ? options.Select(x => new Answer { AnswerValue = x.Value, 
                        AnswerText = x.Title, 
                        ParentValue = x.ParentValue, 
                        AttachmentName = x.AttachmentName}).ToList()
                    : Enumerable.Empty<Answer>().ToList(),
                Properties = Create.QuestionProperties(isCritical)
            };
        }

        public static NumericQuestion NumericRealQuestion(Guid? id = null, string variable = null, string enablementCondition = null, string validationExpression = null, IEnumerable<ValidationCondition> validationConditions = null,
            string title = "test test", int? decimalPlaces = null)
        {
            return new NumericQuestion
            {
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                IsInteger = false,
                ConditionExpression = enablementCondition,
                ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                ValidationExpression = validationExpression,
                QuestionText = title,
                CountOfDecimalPlaces = decimalPlaces
            };
        }

        public static Option[] Options(params Option[] options)
        {
            return options.ToArray();
        }

        public static Option[] Options(params Answer[] options)
        {
            return options.Select(x => new Option(x.GetParsedValue().ToString(CultureInfo.InvariantCulture), x.AnswerText)).ToArray();
        }

        public static QuestionnaireCategoricalOption QuestionnaireCategoricalOption(int code, string text = null, 
            int? parentValue = null, string attachmentName = null) =>
            new QuestionnaireCategoricalOption
            {
                Title = text ?? "text",
                ParentValue = parentValue,
                Value = code,
                AttachmentName = attachmentName
            };

        public static Answer Option(int code, string text = null, string parentValue = null, string attachmentName = null)
        {
            return new Answer
            {
                AnswerText = text ?? "text",
                ParentValue = parentValue,
                AnswerCode = code,
                AttachmentName = attachmentName
            };
        }

        public static Answer Option(string value = null, string text = null, 
            string parentValue = null, string attachmentName = null)
        {
            return new Answer
            {
                AnswerText = text ?? "text",
                AnswerValue = value ?? "1",
                ParentValue = parentValue,
                AttachmentName = attachmentName
            };
        }

        public static QRBarcodeQuestion QRBarcodeQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string variable = null, string validationMessage = null, string text = "test", QuestionScope scope = QuestionScope.Interviewer, bool preFilled = false,
            bool hideIfDisabled = false)
        {
            var publicKey = questionId ?? Guid.NewGuid();
            return new QRBarcodeQuestion
            {
                PublicKey = publicKey,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                QuestionText = text,
                StataExportCaption = variable,
                QuestionScope = scope,
                Featured = preFilled
            };
        }

        public static IQuestion Question(
            Guid? questionId = null,
            string variable = null,
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            QuestionType questionType = QuestionType.Text,
            IEnumerable<ValidationCondition> validationConditions = null,
            string variableLabel =null,
            string title= "Question X test",
            string instructions = null,
            bool isPrefilled = false,
            QuestionScope scope = QuestionScope.Interviewer,
            Guid? linkedToQuestion = null,
            Guid? linkedToRoster = null,
            QuestionProperties properties = null,
            params Answer[] answers)
        {
            var publicKey = questionId ?? Guid.NewGuid();
            var stataExportCaption = variable ?? GetNameForEntity("question", publicKey);

            switch (questionType)
            {
                case QuestionType.Area:
                    return new AreaQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.Audio:
                    return new AudioQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.Multimedia:
                    return new MultimediaQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.Numeric:
                    return new NumericQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.DateTime:
                    return new DateTimeQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.GpsCoordinates:
                    return new GpsCoordinateQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.MultyOption:
                    return new MultyOptionsQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.SingleOption:
                    return new SingleQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.TextList:
                    return new TextListQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.QRBarcode:
                    return new QRBarcodeQuestion()
                    {
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
                case QuestionType.Text:
                    default:
                    return new TextQuestion()
                    {
                        QuestionText = title,
                        PublicKey = publicKey,
                        StataExportCaption = stataExportCaption,
                        ConditionExpression = enablementCondition,
                        ValidationExpression = validationExpression,
                        ValidationMessage = validationMessage,
                        VariableLabel = variableLabel,
                        Instructions = instructions,
                        Answers = answers.ToList(),
                        ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                        Featured = isPrefilled,
                        QuestionScope = scope,
                        LinkedToRosterId = linkedToRoster,
                        LinkedToQuestionId = linkedToQuestion,
                        Properties = properties
                    };
            }
            
            
            
        }

        public static Questionnaire Questionnaire(IExpressionProcessor expressionProcessor = null, 
            IQuestionnaireHistoryVersionsService historyVersionsService = null,
            IFindReplaceService findReplaceService = null, 
            IQuestionnaireTranslator questionnaireTranslator = null,
            ITranslationsService translationsService = null,
            IDesignerTranslationService designerTranslationService = null)
        {
            return new Questionnaire(
                Mock.Of<IClock>(),
                Mock.Of<ILookupTableService>(),
                Mock.Of<IAttachmentService>(),
                designerTranslationService ?? Mock.Of<IDesignerTranslationService>(),
                historyVersionsService ?? Mock.Of<IQuestionnaireHistoryVersionsService>(),
                Mock.Of<IReusableCategoriesService>(),
                findReplaceService ?? Mock.Of<IFindReplaceService>(),
                questionnaireTranslator ?? Mock.Of<IQuestionnaireTranslator>(),
                translationsService ?? Mock.Of<ITranslationsService>());
        }


        public static Questionnaire Questionnaire(Guid responsible, QuestionnaireDocument document)
        {
            var questionnaire = Questionnaire();
            questionnaire.Initialize(document.PublicKey, document, new List<SharedPerson> {Create.SharedPerson(responsible)});
            return questionnaire;
        }

        static int changeRecordSequence = 0;
        public static QuestionnaireChangeRecord QuestionnaireChangeRecord(
            string questionnaireChangeRecordId = null,
            string questionnaireId = null,
            QuestionnaireActionType? action = null,
            Guid? targetId = null,
            QuestionnaireItemType? targetType = null,
            string resultingQuestionnaireDocument = null,
            int? sequence = null,
            string diffWithPreviousVersion = null,
            Guid? userId = null,
            params QuestionnaireChangeReference[] reference)
        {
            return new QuestionnaireChangeRecord()
            {
                UserId = userId ?? Guid.NewGuid(),
                QuestionnaireChangeRecordId = questionnaireChangeRecordId ?? Guid.NewGuid().FormatGuid(),
                QuestionnaireId = questionnaireId,
                ActionType = action ?? QuestionnaireActionType.Add,
                TargetItemId = targetId ?? Guid.NewGuid(),
                TargetItemType = targetType ?? QuestionnaireItemType.Section,
                References = reference.ToImmutableHashSet(),
                Sequence = sequence ?? changeRecordSequence++,
                ResultingQuestionnaireDocument = resultingQuestionnaireDocument,
                Patch = diffWithPreviousVersion
            };
        }

        public static QuestionnaireChangeReference QuestionnaireChangeReference(
            Guid? referenceId = null,
            QuestionnaireItemType? referenceType = null)
        {
            return new QuestionnaireChangeReference()
            {
                ReferenceId = referenceId ?? Guid.NewGuid(),
                ReferenceType = referenceType ?? QuestionnaireItemType.Section
            };
        }

        public static QuestionnaireDocument QuestionnaireDocument(Guid? id = null, params IComposite[] children)
            => Create.QuestionnaireDocument(id: id, children: children, title: "Questionnaire X", variable: "questionnaire_doc");

        public static QuestionnaireDocument QuestionnaireDocumentWithCoverPage(Guid? id, params IComposite[] children)
            => Create.QuestionnaireDocumentWithCoverPage(id, null, children: children);

        public static QuestionnaireDocument QuestionnaireDocumentWithEmptyCoverPage(Guid? id, params IComposite[] children)
        {
            var coverId = Guid.NewGuid();
            var cover = Create.Chapter("Cover", coverId);
            var allChildren = Enumerable.Concat((cover as IComposite).ToEnumerable(), children).ToArray();
            var questionnaireDocument = Create.QuestionnaireDocumentWithoutChildren(id, "Questionnaire with empty cover", children: allChildren);
            questionnaireDocument.CoverPageSectionId = coverId;
            return questionnaireDocument;
        }

        public static Variable Variable(Guid? id = null, VariableType type = VariableType.LongInteger, string variableName = null, string expression = "2*2", 
            string label = null, bool doNotExport = false)
        {
            var publicKey = id ?? Guid.NewGuid();
            var name = variableName ?? GetNameForEntity("var", publicKey);
            return new Variable(publicKey: publicKey, 
                variableData: new VariableData(type: type, name: name, expression: expression, label: label, doNotExport: doNotExport));
        }

        public static QuestionnaireDocument QuestionnaireDocument(
            Guid? id = null, string title = "qqq", IEnumerable<IComposite> children = null, Guid? userId = null)
        {
            return QuestionnaireDocument("questionnaire", id, title, children, userId);
        }

        public static QuestionnaireDocument QuestionnaireDocument(
            string variable, Guid? id = null, string title = null, IEnumerable<IComposite> children = null, Guid? userId = null, Categories[] categories = null)
        {
            var publicKey = id ?? Guid.NewGuid();
            var coverId = Guid.NewGuid();

            var questionnaireDocument = new QuestionnaireDocument
            {
                PublicKey = publicKey,
                CoverPageSectionId = coverId,
                Children = new IComposite[]
                {
                    Section(sectionId: coverId, title: "cover"),
                    Section(children: children?.ToReadOnlyCollection() ?? new ReadOnlyCollection<IComposite>(new List<IComposite>())),
                }.ToReadOnlyCollection(),
                Title = title,
                VariableName = variable,
                CreatedBy = userId ?? Guid.NewGuid(),
                Categories = categories?.ToList() ?? new List<Categories>()
            };
            return questionnaireDocument;
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithoutChildren(
            Guid? id = null, string title = "qqq", IEnumerable<IComposite> children = null, Guid? userId = null)
        {
            return QuestionnaireDocumentWithoutChildren("questionnaire", id, title, children, userId);
        }
        public static QuestionnaireDocument QuestionnaireDocumentWithoutChildren(
            string variable, Guid? id = null, string title = null, IEnumerable<IComposite> children = null, Guid? userId = null, Categories[] categories = null)
        {
            var publicKey = id ?? Guid.NewGuid();

            var questionnaireDocument = new QuestionnaireDocument
            {
                PublicKey = publicKey,
                Children = children?.ToReadOnlyCollection() ?? new ReadOnlyCollection<IComposite>(new List<IComposite>()),
                Title = title,
                VariableName = variable,
                CreatedBy = userId ?? Guid.NewGuid(),
                Categories = categories?.ToList() ?? new List<Categories>()
            };
            return questionnaireDocument;
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(IEnumerable<Macro> macros = null, IEnumerable<IComposite> children = null)
            => QuestionnaireDocumentWithOneChapter(null, null, null, null, macros?.ToArray(), children?.ToArray() ?? new IComposite[] {});

        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(params IComposite[] children)
            => QuestionnaireDocumentWithOneChapter(null, null, null, null, null, children);

        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(Guid chapterId, params IComposite[] children)
        {
            return QuestionnaireDocumentWithOneChapter(null, chapterId, null, null, null, children);
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(Attachment[] attachments = null, params IComposite[] children)
        {
            return QuestionnaireDocumentWithOneChapter(null, null, attachments, null, null, children);
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(Translation[] translations = null, params IComposite[] children)
        {
            return QuestionnaireDocumentWithOneChapter(null, null, null, translations, null, children);
        }
        
        public static QuestionnaireDocument QuestionnaireDocumentWithOneChapter(Guid? questionnaireId = null, Guid? chapterId = null, Attachment[] attachments = null, 
            Translation[] translations = null, IEnumerable<Macro> macros = null, params IComposite[] children)
        {
            var firstChapterId = chapterId.GetValueOrDefault();
            var childrenWithStruct = new IComposite[]
            {
                new Group("Cover")
                {
                    PublicKey = Guid.Parse("C46EE895-0E6E-4063-8136-31E6BFA7C3F8"),
                },
                new Group("Chapter")
                {
                    PublicKey = firstChapterId,
                    Children = children.ToReadOnlyCollection()
                }
            };

            return QuestionnaireDocumentWithoutChildren(questionnaireId, attachments, translations, macros, childrenWithStruct);
        }

        public static QuestionnaireDocument OldQuestionnaireDocumentWithOneChapter(Guid? questionnaireId = null, Guid? chapterId = null, Attachment[] attachments = null,
            Translation[] translations = null, IEnumerable<Macro> macros = null, params IComposite[] children)
        {
            var firstChapterId = chapterId.GetValueOrDefault();
            var childrenWithStruct = new IComposite[]
            {
                new Group("Chapter")
                {
                    PublicKey = firstChapterId,
                    Children = children.ToReadOnlyCollection()
                }
            };

            return QuestionnaireDocumentWithoutChildren(questionnaireId, attachments, translations, macros, childrenWithStruct);
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithoutChildren(Guid? questionnaireId = null, Attachment[] attachments = null,
            Translation[] translations = null, IEnumerable<Macro> macros = null, params IComposite[] children)
        {
            var publicKey = questionnaireId ?? Guid.NewGuid();
            var result = new QuestionnaireDocument
            {
                Title = "Q",
                VariableName = "Q",
                PublicKey = publicKey,
                Children = children.ToReadOnlyCollection(),
            };
            result.Attachments.AddRange(attachments ?? new Attachment[0]);
            result.Translations.AddRange(translations ?? new Translation[0]);

            foreach (var macro in macros ?? Enumerable.Empty<Macro>())
            {
                result.Macros[Guid.NewGuid()] = macro;
            }

            result.ConnectChildrenWithParent();

            return result;
        }

        public static QuestionnaireDocument QuestionnaireDocumentWithCoverPage(Guid? id = null, Attachment[] attachments = null, 
            Translation[] translations = null, IEnumerable<Macro> macros = null, Guid? coverId = null, params IComposite[] children)
        {
            var cover = coverId ?? Guid.NewGuid();
            var childrenWithStruct = new IComposite[]
            {
                new Group("Cover")
                {
                    PublicKey = cover,
                    Children = children.ToReadOnlyCollection()
                }
            };
            var document = QuestionnaireDocumentWithoutChildren(id,  attachments, translations, macros, childrenWithStruct);
            document.CoverPageSectionId = cover;
            return document;
        }
        
        public static QuestionnaireStateTracker QuestionnaireStateTacker()
        {
            return new QuestionnaireStateTracker();
        }

        public static QuestionnaireView QuestionnaireView(Guid? createdBy)
            => Create.QuestionnaireView(new QuestionnaireDocument { CreatedBy = createdBy ?? Guid.NewGuid() });

        public static QuestionnaireView QuestionnaireView(QuestionnaireDocument questionnaireDocument = null, IEnumerable<SharedPersonView> sharedPersons = null)
            => new QuestionnaireView(questionnaireDocument ?? Create.QuestionnaireDocument(), sharedPersons ?? Enumerable.Empty<SharedPersonView>());

        public static RoslynExpressionProcessor RoslynExpressionProcessor() => new RoslynExpressionProcessor();

        public static Group FixedRoster(Guid? rosterId = null, IEnumerable<string> fixedTitles = null, IEnumerable<IComposite> children = null, 
            string variable = "roster_var", string title = "Roster X", FixedRosterTitle[] fixedRosterTitles = null, string enablementCondition = null, 
            RosterDisplayMode displayMode = RosterDisplayMode.SubSection)
            => Create.Roster(
                rosterId: rosterId,
                children: children,
                variable: variable,
                title: title,
                fixedTitles: fixedTitles?.ToArray() ?? new[] { "Fixed Roster 1", "Fixed Roster 2", "Fixed Roster 3" },
                fixedRosterTitles: fixedRosterTitles,
                enablementCondition: enablementCondition,
                displayMode:displayMode);

        public static Group ListRoster(
            Guid? rosterId = null,
            string title = "Roster List",
            string variable = "roster_list",
            string enablementCondition = null,
            Guid? rosterSizeQuestionId = null,
            IEnumerable<IComposite> children = null)
        {
            Group roster = Create.Group(
                groupId: rosterId,
                title: title,
                variable: variable,
                enablementCondition: enablementCondition,
                children: children);

            roster.IsRoster = true;
            roster.RosterSizeSource = RosterSizeSourceType.Question;
            roster.RosterSizeQuestionId = rosterSizeQuestionId;

            return roster;
        }

        public static Group MultiRoster(
            Guid? rosterId = null,
            string title = "Roster Multi",
            string variable = "roster_mul",
            string enablementCondition = null,
            Guid? rosterSizeQuestionId = null,
            IEnumerable<IComposite> children = null)
        {
            Group roster = Create.Group(
                groupId: rosterId,
                title: title,
                variable: variable,
                enablementCondition: enablementCondition,
                children: children);

            roster.IsRoster = true;
            roster.RosterSizeSource = RosterSizeSourceType.Question;
            roster.RosterSizeQuestionId = rosterSizeQuestionId;

            return roster;
        }

        public static Group NumericRoster(
            Guid? rosterId = null,
            string title = "Roster Numeric",
            string variable = "roster_num",
            string enablementCondition = null,
            IEnumerable<IComposite> children = null,
            Guid? rosterSizeQuestionId = null,
            Guid? rosterTitleQuestionId = null,
            RosterDisplayMode displayMode = RosterDisplayMode.SubSection)
        {
            Group roster = Create.Group(
                groupId: rosterId,
                title: title,
                variable: variable,
                enablementCondition: enablementCondition,
                children: children);

            roster.IsRoster = true;
            roster.DisplayMode = displayMode;
            roster.RosterSizeSource = RosterSizeSourceType.Question;
            roster.RosterSizeQuestionId = rosterSizeQuestionId;
            roster.RosterTitleQuestionId = rosterTitleQuestionId;

            return roster;
        }

        public static Group Roster(
            Guid? rosterId = null,
            string title = "Roster X",
            string variable = null,
            string enablementCondition = null,
            string[] fixedTitles = null,
            IEnumerable<IComposite> children = null,
            RosterSizeSourceType rosterType = RosterSizeSourceType.FixedTitles,
            Guid? rosterSizeQuestionId = null,
            Guid? rosterTitleQuestionId = null,
            FixedRosterTitle[] fixedRosterTitles = null,
            RosterDisplayMode displayMode = RosterDisplayMode.SubSection,
            bool customRosterTitle = true)
        {
            var id = rosterId ?? Guid.NewGuid();
            Group group = Create.Group(
                groupId: id,
                title: title,
                variable: variable ?? GetNameForEntity("roster_var",  id),
                enablementCondition: enablementCondition,
                children: children);

            group.IsRoster = true;
            group.RosterSizeSource = rosterType;
            group.DisplayMode = displayMode;

            if (rosterType == RosterSizeSourceType.FixedTitles)
            {
                if (fixedRosterTitles == null)
                {
                    group.FixedRosterTitles =
                        (fixedTitles ?? new[] { "Roster X-1", "Roster X-2", "Roster X-3" }).Select(
                            (x, i) => Create.FixedRosterTitle(i, x)).ToArray();
                }
                else
                {
                    group.FixedRosterTitles = fixedRosterTitles;
                }
            }

            group.RosterSizeQuestionId = rosterSizeQuestionId;
            group.RosterTitleQuestionId = rosterTitleQuestionId;
            group.CustomRosterTitle = customRosterTitle;

            return group;
        }


        public static SingleQuestion SingleOptionQuestion(Guid? questionId = null, 
            QuestionScope scope = QuestionScope.Interviewer,
            string variable = null,
            string enablementCondition = null, string validationExpression = null,
            Guid? linkedToQuestionId = null, Guid? cascadeFromQuestionId = null,
            decimal[] answerCodes = null, string title = null, bool hideIfDisabled = false,
            string linkedFilterExpression = null, Guid? linkedToRosterId = null, List<Answer> answers = null,
            bool isPrefilled = false, bool isComboBox = false, bool showAsList = false, Guid? categoriesId = null,
            string optionsFilterExpression = null)
        {
            var publicKey = questionId ?? Guid.NewGuid();
            var singleOptionQuestion = new SingleQuestion
            {
                PublicKey = publicKey,
                StataExportCaption = variable ?? GetNameForEntity("single_option", publicKey),
                QuestionText = title ?? "SO Question",
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                CascadeFromQuestionId = cascadeFromQuestionId,
                Answers = answers ?? (answerCodes ?? new decimal[0] { }).Select(a => Create.Answer(a.ToString(), a)).ToList(),
                LinkedFilterExpression = linkedFilterExpression,
                Featured = isPrefilled,
                IsFilteredCombobox = isComboBox,
                ShowAsList = showAsList,
                QuestionScope = scope,
                CategoriesId = categoriesId
            };
            singleOptionQuestion.Properties.OptionsFilterExpression = optionsFilterExpression;
            return singleOptionQuestion;
        }

        public static SingleQuestion SingleQuestion(Guid? id = null, string variable = null, string enablementCondition = null, string validationExpression = null,
            Guid? cascadeFromQuestionId = null, List<Answer> options = null, Guid? linkedToQuestionId = null, QuestionScope scope = QuestionScope.Interviewer,
            bool isFilteredCombobox = false, Guid? linkedToRosterId = null, string optionsFilter = null, bool isPrefilled = false,
            string linkedFilter = null, string title = "test")
        {
            return new SingleQuestion
            {
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                Answers = options ?? new List<Answer>(),
                CascadeFromQuestionId = cascadeFromQuestionId,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                LinkedFilterExpression = linkedFilter,
                QuestionScope = scope,
                IsFilteredCombobox = isFilteredCombobox,
                Featured = isPrefilled,
                Properties = {OptionsFilterExpression = optionsFilter},
                QuestionText = title,
            };
        }


        public static StaticText StaticText(
            Guid? staticTextId = null,
            string text = "Static Text X",
            string attachmentName = null,
            string enablementCondition = null,
            bool hideIfDisabled = false,
            IEnumerable<ValidationCondition> validationConditions = null)
        {
            return new StaticText(
                staticTextId ?? Guid.NewGuid(), 
                text,
                enablementCondition,
                hideIfDisabled,
                validationConditions?.ToList() ?? new List<ValidationCondition>(),
                attachmentName);
        }

        public static ISubstitutionService SubstitutionService()
        {
            return new SubstitutionService();
        }

        public static ITextListQuestion TextListQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            int? maxAnswerCount = null, string variable = null, bool hideIfDisabled = false, string title = "test", QuestionScope scope = QuestionScope.Interviewer, bool featured = false)
        {
            return new TextListQuestion()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                MaxAnswerCount = maxAnswerCount,
                StataExportCaption = variable,
                QuestionText = title,
                QuestionScope = scope,
                Featured = featured
            };
        }

        public static TextQuestion TextQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string mask = null,
            string variable = null,
            string validationMessage = null,
            string text = "Question Text test",
            QuestionScope scope = QuestionScope.Interviewer,
            bool preFilled = false,
            string label = null,
            string instruction = null,
            IEnumerable<ValidationCondition> validationConditions = null,
            bool hideIfDisabled = false,
            bool? isCritical = null)

        {
            var publicKey = questionId ?? Guid.NewGuid();
            var stataExportCaption = variable ?? GetNameForEntity("text_question", publicKey);
            return new TextQuestion()
            {
                PublicKey = publicKey,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                Mask = mask,
                QuestionText = text,
                StataExportCaption = stataExportCaption,
                QuestionScope = scope,
                Featured = preFilled,
                VariableLabel = label,
                Instructions = instruction,
                ValidationConditions = validationConditions?.ToList().ConcatWithOldConditionIfNotEmpty(validationExpression, validationMessage),
                Properties = Create.QuestionProperties(isCritical)
            };
        }

        public static QuestionnaireVerificationMessage VerificationError(string code, string message, IEnumerable<string> compilationErrorMessages, params QuestionnaireEntityReference[] questionnaireEntityReferences)
        {
            return QuestionnaireVerificationMessage.Error(code, message, compilationErrorMessages, questionnaireEntityReferences);
        }

        public static QuestionnaireVerificationMessage VerificationError(string code, string message, params QuestionnaireEntityReference[] questionnaireEntityReferences)
        {
            return QuestionnaireVerificationMessage.Error(code, message, questionnaireEntityReferences);
        }

        public static QuestionnaireVerificationMessage VerificationWarning(string code, string message, params QuestionnaireEntityReference[] questionnaireEntityReferences)
        {
            return QuestionnaireVerificationMessage.Warning(code, message, questionnaireEntityReferences);
        }

        public static VerificationMessage VerificationMessage(string code, string message, params QuestionnaireEntityExtendedReference[] extendedReferences)
        {
            return new VerificationMessage
            (
                code : code,
                message : message,
                isGroupedMessage:false,
                errors : new List<VerificationMessageError>()
                {
                    new VerificationMessageError(
                        references : extendedReferences.ToList()
                    )
                }
            );
        }

        public static QuestionnaireEntityReference VerificationReference(Guid? id = null, QuestionnaireVerificationReferenceType type = QuestionnaireVerificationReferenceType.Question)
        {
            return new QuestionnaireEntityReference(type, id ?? Guid.NewGuid());
        }

        public static QuestionnaireEntityExtendedReference VerificationReferenceEnriched(QuestionnaireVerificationReferenceType type, Guid id, string title)
        {
            return new QuestionnaireEntityExtendedReference
            (
                type : type,
                itemId : id.FormatGuid(),
                title : title
            );
        }

        internal static class Command
        {
            public static AddLookupTable AddLookupTable(Guid questionnaireId, Guid lookupTableId, Guid responsibleId, string lookupTableName = "table")
            {
                return new AddLookupTable(questionnaireId, lookupTableName, null, lookupTableId, responsibleId);
            }

            public static AddMacro AddMacro(Guid questionnaire, Guid? macroId = null, Guid? userId = null)
            {
                return new AddMacro(questionnaire, macroId ?? Guid.NewGuid(), userId ?? Guid.NewGuid());
            }

            public static DeleteLookupTable DeleteLookupTable(Guid questionnaireId, Guid lookupTableId, Guid responsibleId)
            {
                return new DeleteLookupTable(questionnaireId, lookupTableId, responsibleId);
            }

            public static DeleteMacro DeleteMacro(Guid questionnaire, Guid? macroId = null, Guid? userId = null)
            {
                return new DeleteMacro(questionnaire, macroId ?? Guid.NewGuid(), userId ?? Guid.NewGuid());
            }

            public static UpdateLookupTable UpdateLookupTable(Guid questionnaireId, Guid lookupTableId, Guid responsibleId, 
                string lookupTableName = "table", Guid? oldLookupTableId = null)
            {
                return new UpdateLookupTable(questionnaireId, lookupTableId, responsibleId, lookupTableName, "file", oldLookupTableId);
            }

            internal static UpdateMacro UpdateMacro(Guid questionnaireId, Guid macroId, string name, string content, 
                string description, Guid? userId)
            {
                return new UpdateMacro(questionnaireId, macroId, name, content, description, userId ?? Guid.NewGuid());
            }

            public static UpdateStaticText UpdateStaticText(Guid questionnaireId, Guid entityId, string text, 
                string attachmentName, Guid responsibleId, string enablementCondition, bool hideIfDisabled = false, 
                List<ValidationCondition> validationConditions = null)
            {
                return new UpdateStaticText(questionnaireId, entityId, text, attachmentName, responsibleId, 
                    enablementCondition, hideIfDisabled, validationConditions);
            }

            public static AddOrUpdateAttachment AddOrUpdateAttachment(Guid questionnaireId, Guid attachmentId, 
                string attachmentContentId, Guid responsibleId, string attachmentName, Guid? oldAttachmentId = null)
            {
                return new AddOrUpdateAttachment(questionnaireId, attachmentId, responsibleId, attachmentName, 
                    attachmentContentId, oldAttachmentId);
            }

            public static DeleteAttachment DeleteAttachment(Guid questionnaireId, Guid attachmentId, Guid responsibleId)
            {
                return new DeleteAttachment(questionnaireId, attachmentId, responsibleId);
            }

            public static UpdateGroup UpdateGroup(Guid questionnaireId, Guid groupId, Guid? responsibleId = null,
                string title = null, string variableName = null, Guid? rosterSizeQuestionId = null,
                string condition = null, bool hideIfDisabled = false, bool isRoster = false,
                RosterSizeSourceType rosterSizeSource = RosterSizeSourceType.Question,
                FixedRosterTitleItem[] fixedRosterTitles = null, Guid? rosterTitleQuestionId = null, RosterDisplayMode displayMode = RosterDisplayMode.SubSection)
                => new UpdateGroup(questionnaireId, groupId, responsibleId ?? Guid.NewGuid(), title, variableName,
                    rosterSizeQuestionId, condition, hideIfDisabled, isRoster,
                    rosterSizeSource, fixedRosterTitles, rosterTitleQuestionId, displayMode);

            public static UpdateVariable UpdateVariable(Guid questionnaireId, Guid entityId, VariableType type, 
                string name, string expression, string label = null, Guid? userId = null, bool doNotExport = false)
            {
                return new UpdateVariable(questionnaireId, userId ?? Guid.NewGuid(), entityId, 
                    new VariableData(type, name, expression, label, doNotExport));
            }

            public static AddOrUpdateTranslation AddOrUpdateTranslation(Guid questionnaireId, Guid translationId, string name, 
                Guid responsibleId, Guid? oldTranslationId = null)
            {
                return new AddOrUpdateTranslation(questionnaireId, responsibleId, translationId, name, oldTranslationId);
            }

            public static DeleteTranslation DeleteTranslation(Guid questionnaireId, Guid translationId, Guid responsibleId)
            {
                return new DeleteTranslation(questionnaireId, responsibleId, translationId);
            }

            public static SetDefaultTranslation SetDefaultTranslation(Guid questionnaireId, Guid? translationId, Guid responsibleId)
            {
                return new SetDefaultTranslation(questionnaireId, responsibleId, translationId);
            }

            public static MoveGroup MoveGroup(Guid questionnaireId, Guid groupId, Guid responsibleId, Guid? targetGroupId = null, int? tagretIndex = null)
                => new MoveGroup(questionnaireId, groupId, targetGroupId, tagretIndex ?? 0, responsibleId);

            public static MoveStaticText MoveStaticText(Guid questionnaireId, Guid staticTextId, Guid responsibleId,
                Guid? targetGroupId = null, int? tagretIndex = null)
                => new MoveStaticText(questionnaireId, staticTextId, targetGroupId ?? Guid.NewGuid(), tagretIndex ?? 0,
                    responsibleId);

            public static MoveVariable MoveVariable(Guid questionnaireId, Guid variableId, Guid responsibleId,
                Guid? targetGroupId = null, int? tagretIndex = null)
                => new MoveVariable(questionnaireId, variableId, targetGroupId ?? Guid.NewGuid(), tagretIndex ?? 0,
                    responsibleId);

            public static MoveQuestion MoveQuestion(Guid questionnaireId, Guid questionId, Guid responsibleId,
                Guid? targetGroupId = null, int? targetIndex = null)
                => new MoveQuestion(questionnaireId, questionId, targetGroupId ?? Guid.NewGuid(), targetIndex ?? 0,
                    responsibleId);

            public static ImportQuestionnaire ImportQuestionnaire(QuestionnaireDocument questionnaireDocument, Guid? responsibleId = null)
                => new ImportQuestionnaire(responsibleId ?? Guid.NewGuid(), questionnaireDocument);

            public static PasteAfter PasteAfter(Guid questionnaireId, Guid entityId, Guid itemToPasteAfterId, 
                Guid sourceQuestionnaireId, Guid sourceItemId, Guid responsibleId) 
                => new PasteAfter(questionnaireId, entityId, itemToPasteAfterId, sourceQuestionnaireId, sourceItemId, responsibleId);

            public static PasteInto PasteInto(Guid questionnaireId, Guid entityId, 
                Guid sourceQuestionnaireId, Guid sourceItemId, Guid targetParentId, Guid responsibleId) 
                => new PasteInto(questionnaireId, entityId, sourceQuestionnaireId, sourceItemId, targetParentId, responsibleId);

            public static DeleteGroup DeleteGroup(Guid questionnaireId, Guid groupId)
                => new DeleteGroup(questionnaireId, groupId, Guid.NewGuid());

            public static ReplaceTextsCommand ReplaceTextsCommand(string searchFor, 
                string replaceWith, 
                bool matchWholeWord = false, 
                bool matchCase = false, 
                bool useRegex = false,
                Guid? userId = null)
            {
                return new ReplaceTextsCommand(Guid.Empty, userId ?? Guid.Empty, searchFor.ToLower(), replaceWith, matchCase, matchWholeWord, useRegex);
            }
            
            public static AddStaticText AddStaticText(Guid questionnaireId, Guid staticTextId, string text, Guid responsibleId, Guid parentId, int? index = null)
            {
                return new AddStaticText(questionnaireId, staticTextId, text, responsibleId, parentId, index);
            }
            
            public static AddDefaultTypeQuestion AddDefaultTypeQuestion(Guid questionnaireId, Guid questionId, string text, Guid responsibleId, Guid parentId, int? tagretIndex = null)
            {
                return new AddDefaultTypeQuestion(questionnaireId, questionId, parentId, text, responsibleId, tagretIndex);
            }

            public static UpdateNumericQuestion UpdateNumericQuestion(Guid questionnaireId, Guid questionId, Guid responsibleId, 
                string title, bool isPreFilled = false, QuestionScope scope = QuestionScope.Interviewer, bool isInteger = false, 
                bool useFormatting = false, int? countOfDecimalPlaces = null, List<ValidationCondition> validationConditions = null,
                Option[] options = null)
            {
                return new UpdateNumericQuestion(questionnaireId, questionId, responsibleId, new CommonQuestionParameters {Title = title}, isPreFilled, scope, 
                    isInteger, useFormatting, countOfDecimalPlaces, validationConditions ?? new List<ValidationCondition>(), options: options);
            }

            public static AddVariable AddVariable(Guid questionnaireId, Guid entityId, Guid parentId, 
                Guid responsibleId, string name = null, string expression = null, 
                VariableType variableType = VariableType.String, string label = null, int? index =null, bool doNotExport = false)
            {
                return new AddVariable(questionnaireId, entityId, 
                    new VariableData(variableType, name, expression, label, doNotExport), responsibleId, parentId, index);
            }

            public static UpdateQuestionnaire UpdateQuestionnaire(Guid questionnaireId, Guid responsibleId, string title = "title", string variable = "questionnaire", bool isPublic = false, bool isResponsibleAdmin = false, string defaultLanguageName = "Original")
            {
                return new UpdateQuestionnaire(questionnaireId, title, variable, false, isPublic, defaultLanguageName, responsibleId, isResponsibleAdmin);
            }
            public static DeleteQuestionnaire DeleteQuestionnaire(Guid questionnaireId, Guid responsibleId)
            {
                return new DeleteQuestionnaire(questionnaireId, responsibleId);
            }

            public static RevertVersionQuestionnaire RevertVersionQuestionnaire(Guid questionnaireId, Guid historyReferanceId, Guid responsibleId)
            {
                return new RevertVersionQuestionnaire(questionnaireId, historyReferanceId, responsibleId);
            }

            public static SwitchToTranslation SwitchToTranslation(Guid questionnaireId, Guid responsibleId, Guid? translationId)
            {
                return new SwitchToTranslation(questionnaireId, responsibleId, translationId);
            }

            public static CreateQuestionnaire CreateQuestionnaire(Guid questionnaireId, string title, Guid? createdBy, bool isPublic, string variable = null)
            {
                return new CreateQuestionnaire(questionnaireId, title, createdBy ?? Guid.NewGuid(), isPublic, variable);
            }

            public static UpdateMultimediaQuestion UpdateMultimediaQuestion(Guid questionId, string title, string variableName, string instructions, string enablementCondition, string variableLabel, bool hideIfDisabled, Guid responsibleId, QuestionScope scope, QuestionProperties properties, bool isSignature)
            {
                return new UpdateMultimediaQuestion(Guid.NewGuid(), questionId, responsibleId, new CommonQuestionParameters
                {
                    EnablementCondition = enablementCondition,
                    HideIfDisabled = hideIfDisabled,
                    Title = title,
                    Instructions = instructions,
                    VariableName = variableName,
                    VariableLabel = variableLabel,
                    HideInstructions = properties.HideInstructions
                }, scope)
                {
                    IsSignature = isSignature
                };
            }

            public static UpdateMultiOptionQuestion UpdateMultiOptionQuestion(Guid questionId, Guid responsibleId, string title,
                string variableName, string variableLabel = null, string enablementCondition = null, string instructions = null,
                string validationExpression = null, string validationMessage = null, QuestionScope scope = QuestionScope.Interviewer, Option[] options = null,
                Guid? linkedToQuestionId = null, bool areAnswersOrdered = false, int? maxAllowedAnswers = null, bool yesNoView = false,
                string linkedFilterExpression = null, bool isFilteredCombobox = false, bool hideIfDisabled = false, 
                List<ValidationCondition> validationConditions = null, Guid? categoriesId = null) => new UpdateMultiOptionQuestion(
                Guid.NewGuid(),
                questionId,
                responsibleId,
                commonQuestionParameters: new CommonQuestionParameters()
                {
                    Title = title,
                    VariableName = variableName,
                    VariableLabel = variableLabel,
                    EnablementCondition = enablementCondition,
                    Instructions = instructions,
                    HideIfDisabled = hideIfDisabled
                },
                validationExpression,
                validationMessage,
                scope,
                options ?? new[] {new Option(title : "1", value : "1"), new Option(title : "2", value : "2")},
                linkedToQuestionId,
                areAnswersOrdered,
                maxAllowedAnswers,
                yesNoView,
                validationConditions ?? new List<ValidationCondition>(),
                linkedFilterExpression,
                isFilteredCombobox,
                categoriesId);

            public static MigrateToNewVersion MigrateToNewVersion(Guid questionnaireId, Guid userId)
            {
                return new MigrateToNewVersion(questionnaireId, userId); 
            }
        }

        public static ValidationCondition ValidationCondition(string expression = "self != null", string message = "should be answered")
        {
            return new ValidationCondition(expression, message);
        }

        public static AttachmentService AttachmentService(
            DesignerDbContext dbContext = null)
        {
            return new AttachmentService(
                dbContext ?? Create.InMemoryDbContext(), videoConverter: Mock.Of<IVideoConverter>());
        }

        public static QuestionnaireHistoryVersionsService QuestionnireHistoryVersionsService(
            DesignerDbContext dbContext = null,
            IEntitySerializer<QuestionnaireDocument> entitySerializer = null,
            IPatchApplier patchApplier = null,
            IOptions<QuestionnaireHistorySettings> questionnaireHistorySettings = null,
            ICommandService commandService = null,
            IMemoryCache memoryCache = null)
        {
            return new QuestionnaireHistoryVersionsService(
                dbContext ?? Create.InMemoryDbContext(),
                entitySerializer ?? new EntitySerializer<QuestionnaireDocument>(),
                questionnaireHistorySettings ?? Mock.Of<IOptions<QuestionnaireHistorySettings>>(o => o.Value == new QuestionnaireHistorySettings
                {
                    QuestionnaireChangeHistoryLimit = 10
                }), 
                patchApplier ?? Create.PatchApplier(),
                Create.PatchGenerator(),
                commandService ?? Mock.Of<ICommandService>(),
                memoryCache ?? Mock.Of<IMemoryCache>());
        }

        private static IPatchApplier PatchApplier()
        {
            return new JsonPatchService(new ZipArchiveUtils());
        }

        public static ITopologicalSorter<T> TopologicalSorter<T>()
        {
            return new TopologicalSorter<T>();
        }

        public static SharedPerson SharedPerson(Guid? id = null, string email = null, bool isOwner = true, ShareType shareType = ShareType.Edit)
        {
            return new SharedPerson
            {
                UserId = id ?? Guid.NewGuid(),
                IsOwner = isOwner,
                Email = email ?? "user@e.mail",
                ShareType = shareType
            };
        }

        public static SharedPersonView SharedPersonView(Guid? id = null, string email = null, bool isOwner = true, ShareType shareType = ShareType.Edit)
        {
            return new SharedPersonView
            {
                UserId = id ?? Guid.NewGuid(),
                IsOwner = isOwner,
                Email = email ?? "user@e.mail",
                ShareType = shareType
            };
        }

        public static TranslationInstance TranslationInstance(Guid? questionnaireId = null,
            TranslationType type = TranslationType.Unknown,
            Guid? questionnaireEntityId = null,
            string translationIndex = null,
            Guid? translationId = null,
            string translation = null)
        {
            return new TranslationInstance
            {
                Id = Guid.NewGuid(),
                QuestionnaireId = questionnaireId ?? Guid.NewGuid(),
                QuestionnaireEntityId = questionnaireEntityId ?? Guid.NewGuid(),
                Type = type,
                Value = translation,
                TranslationIndex = translationIndex,
                TranslationId = translationId ?? Guid.NewGuid()
            };
        }

        public static TranslationDto TranslationDto(
            TranslationType type = TranslationType.Unknown,
            Guid? questionnaireEntityId = null,
            string translationIndex = null,
            Guid? translationId = null,
            string translation = null)
        {
            return new TranslationDto
            {
                QuestionnaireEntityId = questionnaireEntityId ?? Guid.NewGuid(),
                Type = type,
                Value = translation,
                TranslationIndex = translationIndex,
                TranslationId = translationId ?? Guid.NewGuid()
            };
        }

        public static Translation Translation(Guid? translationId = null, string name = null)
        {
            return new Translation() { Name = name, Id = translationId ?? Guid.NewGuid() };
        }

        public static TranslationsService TranslationsService(
            DesignerDbContext dbContext = null,
            IQuestionnaireViewFactory questionnaireStorage = null,
            IReusableCategoriesService reusableCategoriesService = null,
            ITranslationsExportService translationsExportService = null)
            => new TranslationsService(
                dbContext ?? Create.InMemoryDbContext(),
                questionnaireStorage ?? Stub<IQuestionnaireViewFactory>.Returning(Create.QuestionnaireView()),
                translationsExportService ?? new TranslationsExportService(),
                reusableCategoriesService ?? Mock.Of<IReusableCategoriesService>()
            );


        public static UpdateQuestionnaire UpdateQuestionnaire(string title, bool isPublic, Guid responsibleId, bool isResponsibleAdmin = false, string variable = "questionnaire", string defaultLanguageName = "Original")
            => new UpdateQuestionnaire(Guid.NewGuid(), title, variable, false, isPublic, defaultLanguageName, responsibleId, isResponsibleAdmin);

        public static QuestionnaireListViewItem QuestionnaireListViewItem(Guid? id = null, bool isPublic = false, SharedPerson[] sharedPersons = null)
            => QuestionnaireListViewItem(id ?? Guid.Empty, isPublic, null, null, sharedPersons);

        public static QuestionnaireListViewItem QuestionnaireListViewItem(Guid id, 
            bool isPublic = false, 
            string title = null,
            Guid? createdBy = null,
            SharedPerson[] sharedPersons = null)
        {
            return new QuestionnaireListViewItem() {
                OwnerId = createdBy.GetValueOrDefault(),
                IsPublic = isPublic,
                PublicId = id,
                Title = title,
                SharedPersons = new HashSet<SharedPerson>(sharedPersons ?? Enumerable.Empty<SharedPerson>())
            };
        }

        public static QuestionnaireListView QuestionnaireListView(params QuestionnaireListViewItem[] items)
            => new QuestionnaireListView(1, 10, items.Length, items, string.Empty);

        public static HistoryPostProcessor HistoryPostProcessor(
            DesignerDbContext dbContext = null,
            IQuestionnaireHistoryVersionsService questionnaireHistoryVersionsService = null,
            IPlainKeyValueStorage<QuestionnaireStateTracker> questionnaireStateTrackerStorage = null) =>
            new HistoryPostProcessor(
                dbContext ?? Create.InMemoryDbContext(),
                questionnaireHistoryVersionsService ?? Mock.Of<IQuestionnaireHistoryVersionsService>(),
                questionnaireStateTrackerStorage ?? Mock.Of<IPlainKeyValueStorage<QuestionnaireStateTracker>>()
                );



        public static DynamicCompilerSettingsProvider DynamicCompilerSettingsProvider()
        {
            return new DynamicCompilerSettingsProvider();
        }

        public static QuestionnaireVerifier QuestionnaireVerifier(
            IExpressionProcessor expressionProcessor = null,
            ISubstitutionService substitutionService = null,
            IKeywordsProvider keywordsProvider = null,
            IExpressionProcessorGenerator expressionProcessorGenerator = null,
            IMacrosSubstitutionService macrosSubstitutionService = null,
            ILookupTableService lookupTableService = null,
            IAttachmentService attachmentService = null,
            ITopologicalSorter<Guid> topologicalSorter = null,
            IQuestionnaireTranslator questionnaireTranslator = null,
            IReusableCategoriesService reusableCategoriesService = null)
        {
            var fileSystemAccessorMock = new Mock<IFileSystemAccessor>();
            fileSystemAccessorMock.Setup(x => x.MakeStataCompatibleFileName(Moq.It.IsAny<string>())).Returns<string>(s => s);

            var questionnireExpressionProcessorGeneratorMock = new Mock<IExpressionProcessorGenerator>();
            string generationResult;
            questionnireExpressionProcessorGeneratorMock.Setup(
                _ => _.GenerateProcessorStateAssembly(Moq.It.IsAny<QuestionnaireCodeGenerationPackage>(), Moq.It.IsAny<int>(), out generationResult))
                .Returns(new GenerationResult( success : true, diagnostics : new List<Diagnostic>() ));

            var substitutionServiceInstance = new SubstitutionService();

            var lookupTableServiceMock = new Mock<ILookupTableService>(MockBehavior.Default)
            {
                DefaultValue = DefaultValue.Mock
            };

            var attachmentServiceMock = Stub<IAttachmentService>.WithNotEmptyValues;

            var expressionProcessorImp = expressionProcessor ?? Create.RoslynExpressionProcessor();
            var macrosSubstitutionServiceImp = macrosSubstitutionService ?? Create.MacrosSubstitutionService();
            var expressionsPlayOrderProvider = new ExpressionsPlayOrderProvider(
                new ExpressionsGraphProvider(expressionProcessorImp, macrosSubstitutionServiceImp)
            );

            return new QuestionnaireVerifier(expressionProcessorImp,
                fileSystemAccessorMock.Object,
                substitutionService ?? substitutionServiceInstance,
                keywordsProvider ?? new KeywordsProvider(substitutionServiceInstance),
                expressionProcessorGenerator ?? questionnireExpressionProcessorGeneratorMock.Object,
                new DesignerEngineVersionService(
                    Mock.Of<IAttachmentService>(a => a.GetContent(It.IsAny<string>()) == new AttachmentContent(){ContentType = "image/png"})
                    , Mock.Of<IDesignerTranslationService>(),
                    Mock.Of<IReusableCategoriesService>()),
                macrosSubstitutionServiceImp,
                lookupTableService ?? lookupTableServiceMock.Object,
                attachmentService ?? attachmentServiceMock,
                topologicalSorter ?? Create.TopologicalSorter<Guid>(),
                Mock.Of<ITranslationsService>(),
                questionnaireTranslator ?? Mock.Of<IQuestionnaireTranslator>(),
                Mock.Of<IQuestionnaireCompilationVersionService>(), 
                Mock.Of<IDynamicCompilerSettingsProvider>(x => x.GetAssembliesToReference() == DynamicCompilerSettingsProvider().GetAssembliesToReference()),
                expressionsPlayOrderProvider,
                reusableCategoriesService ?? Mock.Of<IReusableCategoriesService>(),
                new QuestionnaireCodeGenerationPackageFactory(lookupTableService ?? lookupTableServiceMock.Object));
        }

        public static IQuestionTypeToCSharpTypeMapper QuestionTypeToCSharpTypeMapper()
        {
            return new QuestionTypeToCSharpTypeMapper();
        }

        private static string GetNameForEntity(string prefix, Guid entityId)
        {
            var name = prefix + "_" + entityId.ToString("N");
            if (name.Length > 32)
                return name.Substring(0, 32);

            return name;
        }

        public static IPatchGenerator PatchGenerator()
        {
            return new JsonPatchService(new ZipArchiveUtils());
        }

        public static ICategoricalOptionsImportService CategoricalOptionsImportService(QuestionnaireDocument document, IReusableCategoriesService reusableCategoriesService = null)
            => new CategoricalOptionsImportService(
                new InMemoryKeyValueStorage<QuestionnaireDocument>(
                    new Dictionary<string, QuestionnaireDocument>()
                    {
                        {
                            document.PublicKey.FormatGuid(),
                            document
                        }
                    }), categoriesExtractFactory: CategoriesExtractFactory());

        public static ClassificationsStorage ClassificationStorage(
            DesignerDbContext dbContext)
        {
            return new ClassificationsStorage(
                dbContext ?? Create.InMemoryDbContext());
        }

        public static DesignerDbContext ClassificationsAccessor(params ClassificationEntity[] entities)
        {
            var storage = Create.InMemoryDbContext();
            foreach (var classificationEntity in entities)
            {
                storage.Add(classificationEntity);
            }
            storage.SaveChanges();
            return storage;
        }

        public static PublicFoldersStorage PublicFoldersStorage(DesignerDbContext dbContext = null)
        {
            return new PublicFoldersStorage(
                dbContext ?? Create.InMemoryDbContext()
                );
        }

        public static QuestionnaireListViewFolder QuestionnaireListViewFolder(Guid? id=null, string title = null, Guid? parent = null)
        {
            return new QuestionnaireListViewFolder
            {
                Parent = parent,
                PublicId = id ?? Guid.NewGuid(),
                Title = title
            };
        }

        public static Fixture AutoFixture()
        {
            var autoFixture = new Fixture();
            autoFixture.Customize(new AutoMoqCustomization());
            return autoFixture;
        }

        public static DesignerDbContext InMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<DesignerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            var dbContext = new DesignerDbContext(options);

            return dbContext;
        }

        public static ExpressionsPlayOrderProvider ExpressionsPlayOrderProvider(
            IExpressionProcessor expressionProcessor = null,
            IMacrosSubstitutionService macrosSubstitutionService = null)
        {
            return new ExpressionsPlayOrderProvider(
                new ExpressionsGraphProvider(
                    expressionProcessor ?? ServiceLocator.Current.GetInstance<IExpressionProcessor>(),
                    macrosSubstitutionService ?? new MacrosSubstitutionService()));
        }

        public static ListViewPostProcessor ListViewPostProcessor(DesignerDbContext dbContext = null,
            IRecipientNotifier emailNotifier = null)
        {
            return new ListViewPostProcessor(
                dbContext ?? Create.InMemoryDbContext(),
                emailNotifier ?? Mock.Of<IRecipientNotifier>());
        }

        public static IPlainKeyValueStorage<T> MockedKeyValueStorage<T>() where T : class
        {
            var result = new Mock<IPlainKeyValueStorage<T>>();
            result.DefaultValue = DefaultValue.Mock;

            return result.Object;
        }

        public static QuestionnaireRevision QuestionnaireRevision(string questionnaireId)
            => new QuestionnaireRevision(Guid.Parse(questionnaireId));

        public static QuestionnaireRevision QuestionnaireRevision(Guid questionnaireId)
            => new QuestionnaireRevision(questionnaireId);

        public static QuestionnaireRevision QuestionnaireRevision(Guid questionnaireId, Guid rev)
            => new QuestionnaireRevision(questionnaireId, rev);

        public static Categories Categories(Guid? id = null, string name = null) =>
            new Categories {Id = id ?? Guid.NewGuid(), Name = name};

        public static AddOrUpdateCategories AddOrUpdateCategories(Guid questionnaireId, Guid responsibleId,
            Guid categoriesId, string text = null, Guid? oldCategoriesId = null) =>
            new AddOrUpdateCategories(questionnaireId, responsibleId, categoriesId, text ?? "new categories",
                oldCategoriesId);

        public static DeleteCategories DeleteCategories(Guid questionnaireId, Guid responsibleId, Guid categoriesId) =>
            new DeleteCategories(questionnaireId, responsibleId, categoriesId);

        public static CopyPastePreProcessor CopyPastePreProcessor(IReusableCategoriesService reusableCategoriesService) =>
            new CopyPastePreProcessor(reusableCategoriesService);

        public static IReusableCategoriesService CategoriesService(DesignerDbContext dbContext = null,
            IQuestionnaireViewFactory questionnaireStorage = null,
            ICategoriesExtractFactory categoriesExtractFactory = null)
            => new ReusableCategoriesService(dbContext ?? Mock.Of<DesignerDbContext>(),
                questionnaireStorage ?? Mock.Of<IQuestionnaireViewFactory>(),
                categoriesExtractFactory ?? Mock.Of<ICategoriesExtractFactory>());

        public static CategoriesRow CategoriesRow(string id, string text, string parentId, int rowId) => new CategoriesRow
        {
            Id = id,
            ParentId = parentId,
            Text = text,
            RowId = rowId
        };

        public static CategoriesInstance CategoriesInstance(Guid questionnaireId, Guid categoriesId, int value,
            int sortIndex = 0, string text = "") =>
            new CategoriesInstance
            {
                QuestionnaireId = questionnaireId,
                CategoriesId = categoriesId,
                Value = value,
                Text = text,
                SortIndex = sortIndex
            };

        public static ICategoriesExtractFactory CategoriesExtractFactory(
            ICategoriesExportService categoriesExportService = null)
        {
            return new CategoriesExtractFactory(
                new ExcelCategoriesExtractService(new CategoriesVerifier(), categoriesExportService ?? new CategoriesExportService()),
                new TsvCategoriesExtractService(new CategoriesVerifier()));
        }
    }
}
