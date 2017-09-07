using System;
using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Headquarters.EventHandler.WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.Implementation.Aggregates;
using WB.Core.BoundedContexts.Headquarters.Questionnaires.Translations;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.Implementation;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Accessors;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;

namespace WB.Tests.Abc.TestFactories
{
    public class AggregateRootFactory
    {
        public Interview Interview(Guid? interviewId = null,
            IQuestionnaireStorage questionnaireRepository = null,
            IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider = null,
            QuestionnaireIdentity questionnaireId = null,
            ISubstitutionTextFactory textFactory = null,
            IQuestionOptionsRepository questionOptionsRepository = null)
        {
            var questionnaireDocument = Create.Entity.QuestionnaireDocumentWithOneChapter();
            questionnaireDocument.IsUsingExpressionStorage = true;

            var questionnaireDefaultRepository = Mock.Of<IQuestionnaireStorage>(repository =>
                repository.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) == Create.Entity.PlainQuestionnaire(questionnaireDocument, 1, null) &&
                repository.GetQuestionnaireDocument(It.IsAny<QuestionnaireIdentity>()) == questionnaireDocument);

            var textFactoryMock = new Mock<ISubstitutionTextFactory> {DefaultValue = DefaultValue.Mock};
            var interview = new Interview(questionnaireRepository ?? questionnaireDefaultRepository,
                expressionProcessorStatePrototypeProvider ?? Stub.InterviewExpressionStateProvider(),
                textFactory ?? textFactoryMock.Object,
                Create.Service.InterviewTreeBuilder());

            interview.SetId(interviewId ?? Guid.NewGuid());

            if (questionOptionsRepository != null)
            {
                Setup.InstanceToMockedServiceLocator<IQuestionOptionsRepository>(questionOptionsRepository);
            }

            interview.Apply(Create.Event.InterviewCreated(
                questionnaireId: questionnaireId?.QuestionnaireId ?? Guid.NewGuid(),
                questionnaireVersion: questionnaireId?.Version ?? 1));

            return interview;
        }

        public Questionnaire Questionnaire(
            IQuestionnaireStorage questionnaireStorage = null,
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemStorage = null,
            IFileSystemAccessor fileSystemAccessor = null,
            IPlainStorageAccessor<TranslationInstance> translationsStorage = null)
            => new Questionnaire(
                questionnaireStorage ?? Mock.Of<IQuestionnaireStorage>(),
                Mock.Of<IQuestionnaireAssemblyAccessor>(),
                questionnaireBrowseItemStorage ?? Mock.Of<IPlainStorageAccessor<QuestionnaireBrowseItem>>(),
                Mock.Of<IPlainKeyValueStorage<QuestionnaireQuestionsInfo>>(),
                fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(),
                translationsStorage ?? new InMemoryPlainStorageAccessor<TranslationInstance>());

        public StatefulInterview StatefulInterview(Guid interviewId, 
            Guid? questionnaireId = null,
            Guid? userId = null,
            QuestionnaireDocument questionnaire = null,
            bool shouldBeInitialized = true)
        {
            var interview = this.StatefulInterview(questionnaireId, userId, questionnaire, shouldBeInitialized);
            interview.SetId(interviewId);
            return interview;
        }

        public StatefulInterview StatefulInterview(Guid? questionnaireId = null,
            Guid? userId = null,
            QuestionnaireDocument questionnaire = null, 
            bool shouldBeInitialized = true,
            Action<Mock<IInterviewLevel>> setupLevel = null)
        {
            questionnaireId = questionnaireId ?? questionnaire?.PublicKey ?? Guid.NewGuid();
            if (questionnaire != null)
            {
                questionnaire.IsUsingExpressionStorage = true;
                questionnaire.ExpressionsPlayOrder = Create.Service.ExpressionsPlayOrderProvider().GetExpressionsPlayOrder(questionnaire.AsReadOnly());
            }

            var questionnaireRepository = Setup.QuestionnaireRepositoryWithOneQuestionnaire(questionnaire);

            var statefulInterview = new StatefulInterview(
                questionnaireRepository,
                CreateDefaultInterviewExpressionStateProvider(setupLevel),
                Create.Service.SubstitutionTextFactory(),
                Create.Service.InterviewTreeBuilder());

            if (shouldBeInitialized)
            {
                var command = Create.Command.CreateInterview(Guid.Empty, userId ?? Guid.NewGuid(), Create.Entity.QuestionnaireIdentity(questionnaireId.Value, 1), DateTime.Now,
                    Guid.NewGuid(), null, null, null);
                statefulInterview.CreateInterview(command);
            }

            return statefulInterview;
        }

        public StatefulInterview StatefulInterview(
            Guid? questionnaireId = null,
            long? questionnaireVersion = null,
            Guid? userId = null,
            IQuestionnaireStorage questionnaireRepository = null,
            bool shouldBeInitialized = true,
            Action<Mock<IInterviewLevel>> setupLevel = null)
        {
            questionnaireId = questionnaireId ?? Guid.NewGuid();

            var statefulInterview = new StatefulInterview(
                questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                CreateDefaultInterviewExpressionStateProvider(setupLevel),
                Create.Service.SubstitutionTextFactory(),
                Create.Service.InterviewTreeBuilder());

            if (shouldBeInitialized)
            {
                var command = Create.Command.CreateInterview(Guid.Empty, userId ?? Guid.NewGuid(), Create.Entity.QuestionnaireIdentity(questionnaireId.Value, 1), DateTime.Now,
                    Guid.NewGuid(), null, null, null);
                statefulInterview.CreateInterview(command);
            }

            return statefulInterview;
        }

        private static IInterviewExpressionStatePrototypeProvider CreateDefaultInterviewExpressionStateProvider(Action<Mock<IInterviewLevel>> setupLevel = null)
        {
            //Stub<IInterviewExpressionStatePrototypeProvider>.WithNotEmptyValues,
            var expressionStorage = new Mock<IInterviewExpressionStorage>();
            var levelMock = new Mock<IInterviewLevel>();
            setupLevel?.Invoke(levelMock);
            expressionStorage.Setup(x => x.GetLevel(It.IsAny<Identity>())).Returns(levelMock.Object);

            var expressionState = Stub<ILatestInterviewExpressionState>.WithNotEmptyValues;

            var defaultExpressionStatePrototypeProvider = Mock.Of<IInterviewExpressionStatePrototypeProvider>(_
                => _.GetExpressionStorage(It.IsAny<QuestionnaireIdentity>()) == expressionStorage.Object
                && _.GetExpressionState(It.IsAny<Guid>(), It.IsAny<long>()) == expressionState);

            return defaultExpressionStatePrototypeProvider;
        }
    }
}