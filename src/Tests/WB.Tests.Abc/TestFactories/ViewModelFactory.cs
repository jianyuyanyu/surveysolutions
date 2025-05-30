﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Documents;
using Moq;
using MvvmCross.Base;
using MvvmCross.Plugin.Messenger;
using Ncqrs;
using WB.Core.BoundedContexts.Supervisor.Services;
using WB.Core.BoundedContexts.Supervisor.ViewModel;
using WB.Core.BoundedContexts.Supervisor.ViewModel.Dashboard;
using WB.Core.BoundedContexts.Supervisor.ViewModel.Dashboard.Items;
using WB.Core.BoundedContexts.Supervisor.ViewModel.Dashboard.Services;
using WB.Core.BoundedContexts.Supervisor.Views;
using WB.Core.BoundedContexts.Tester.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.HttpServices.Services;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Services.Workspace;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.Core.SharedKernels.Enumerator.Views.Dashboard;

namespace WB.Tests.Abc.TestFactories
{
    internal class ViewModelFactory
    {
        public AttachmentViewModel AttachmentViewModel(
            IQuestionnaireStorage questionnaireRepository = null,
            IStatefulInterviewRepository interviewRepository = null,
            IAttachmentContentStorage attachmentContentStorage = null)
            => new AttachmentViewModel(questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(), 
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(), 
                Create.Service.LiteEventRegistry(),
                attachmentContentStorage, 
                Mock.Of<IInterviewPdfService>(),
                Mock.Of<IViewModelNavigationService>());

        public DynamicTextViewModel DynamicTextViewModel(
            IViewModelEventRegistry eventRegistry = null, 
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaireStorage questionnaireStorage = null)
            => new DynamicTextViewModel(
                eventRegistry ?? Create.Service.LiteEventRegistry(),
                interviewRepository: interviewRepository,
                substitutionService: Create.Service.SubstitutionService(),
                questionnaireStorage: questionnaireStorage ?? SetUp.QuestionnaireRepositoryWithOneQuestionnaire(Mock.Of<IQuestionnaire>()));

        public InterviewStateViewModel InterviewStateViewModel(
            IStatefulInterviewRepository interviewRepository = null,
            IInterviewStateCalculationStrategy interviewStateCalculationStrategy = null,
            IQuestionnaireStorage questionnaireRepository = null)
        => new InterviewStateViewModel( interviewRepository,
             interviewStateCalculationStrategy,
             questionnaireRepository: questionnaireRepository ?? SetUp.QuestionnaireRepositoryWithOneQuestionnaire(Mock.Of<IQuestionnaire>()));

        public ErrorMessageViewModel ErrorMessageViewModel(
            IViewModelEventRegistry eventRegistry = null,
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaireStorage questionnaireStorage = null)
            => new ErrorMessageViewModel(
                eventRegistry ?? Create.Service.LiteEventRegistry(),
                interviewRepository: interviewRepository,
                substitutionService: Create.Service.SubstitutionService(),
                questionnaireStorage: questionnaireStorage ??
                    Mock.Of<IQuestionnaireStorage>(x => x.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) == Mock.Of<IQuestionnaire>()));

        public EnumerationStageViewModel EnumerationStageViewModel(
            IInterviewViewModelFactory interviewViewModelFactory = null,
            IQuestionnaireStorage questionnaireRepository = null,
            IStatefulInterviewRepository interviewRepository = null,
            ISubstitutionService substitutionService = null,
            IViewModelEventRegistry eventRegistry = null,
            IMvxMessenger messenger = null,
            IUserInterfaceStateService userInterfaceStateService = null,
            IMvxMainThreadAsyncDispatcher mvxMainThreadDispatcher = null,
            ICompositeCollectionInflationService compositeCollectionInflationService = null,
            IVibrationService vibrationService = null)
            => new EnumerationStageViewModel(
                interviewViewModelFactory ?? Mock.Of<IInterviewViewModelFactory>(),
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                userInterfaceStateService ?? Mock.Of<IUserInterfaceStateService>(),
                Create.ViewModel.DynamicTextViewModel(
                    eventRegistry: eventRegistry,
                    interviewRepository: interviewRepository),
                compositeCollectionInflationService ?? Mock.Of<ICompositeCollectionInflationService>(),
                Mock.Of<IViewModelEventRegistry>(),
                Mock.Of<ICommandService>(),
                Create.Fake.MvxMainThreadDispatcher());

        public ErrorMessagesViewModel ErrorMessagesViewModel(
            IQuestionnaireStorage questionnaireRepository = null,
            IStatefulInterviewRepository interviewRepository = null)
        {
            var dynamicTextViewModelFactory = Mock.Of<IDynamicTextViewModelFactory>();

            Mock.Get(dynamicTextViewModelFactory)
                .Setup(factory => factory.CreateDynamicTextViewModel())
                .Returns(() => Create.ViewModel.DynamicTextViewModel(
                    interviewRepository: interviewRepository));
            Mock.Get(dynamicTextViewModelFactory)
                .Setup(factory => factory.CreateErrorMessage())
                .Returns(() => Create.ViewModel.ErrorMessageViewModel(
                    interviewRepository: interviewRepository));

            return new ErrorMessagesViewModel(dynamicTextViewModelFactory);
        }

        public SingleOptionLinkedToListQuestionViewModel SingleOptionLinkedToListQuestionViewModel(
            IQuestionnaire questionnaire = null,
            IStatefulInterview interview = null,
            IViewModelEventRegistry eventRegistry = null,
            QuestionStateViewModel<SingleOptionQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null,
            IUserInteractionService userInteraction = null)
        {
            userInteraction = userInteraction ?? Mock.Of<IUserInteractionService>();
            var mockOfViewModelFactory = new Mock<IInterviewViewModelFactory>();
            mockOfViewModelFactory.Setup(x => x.GetNew<SingleOptionQuestionOptionViewModel>()).Returns(() =>
                new SingleOptionQuestionOptionViewModel(Create.ViewModel.AttachmentViewModel()));
            
           return new SingleOptionLinkedToListQuestionViewModel(
                Mock.Of<IPrincipal>(_ =>
                    _.CurrentUserIdentity == Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid())),
                Create.Storage.QuestionnaireStorage(questionnaire ?? Mock.Of<IQuestionnaire>()),
                Create.Storage.InterviewRepository(interview ?? Mock.Of<IStatefulInterview>()),
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                questionState ?? Stub<QuestionStateViewModel<SingleOptionQuestionAnswered>>.WithNotEmptyValues,
                Abc.SetUp.FilteredOptionsViewModel(),
                Mock.Of<QuestionInstructionViewModel>(),
                answering ?? Mock.Of<AnsweringViewModel>(),
                Create.ViewModel.ThrottlingViewModel(),
                mockOfViewModelFactory.Object);
        }

        public CategoricalMultiLinkedToListViewModel MultiOptionLinkedToListQuestionQuestionViewModel(
            IQuestionnaire questionnaire = null,
            IStatefulInterview interview = null,
            IViewModelEventRegistry eventRegistry = null,
            QuestionStateViewModel<MultipleOptionsQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null,
            IUserInteractionService userInteraction = null)
        {
            var questionnaireRepository = SetUp.QuestionnaireRepositoryWithOneQuestionnaire(questionnaire ?? Mock.Of<IQuestionnaire>());

            var statefulInterviewRepository = SetUp.StatefulInterviewRepository(interview ?? Mock.Of<IStatefulInterview>());

            userInteraction = userInteraction ?? Mock.Of<IUserInteractionService>();
            var mockOfViewModelFactory = new Mock<IInterviewViewModelFactory>();
            mockOfViewModelFactory.Setup(x => x.GetNew<CategoricalMultiOptionViewModel<int>>()).Returns(() =>
                new CategoricalMultiOptionViewModel<int>(userInteraction, Create.ViewModel.AttachmentViewModel()));
            
            return new CategoricalMultiLinkedToListViewModel(
                questionState ?? Create.ViewModel.QuestionState<MultipleOptionsQuestionAnswered>(eventRegistry, statefulInterviewRepository, questionnaireRepository),
                questionnaireRepository,
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                statefulInterviewRepository,
                Mock.Of<IPrincipal>(_ => _.CurrentUserIdentity == Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid())),
                answering ?? Mock.Of<AnsweringViewModel>(),
                Mock.Of<QuestionInstructionViewModel>(),
                Create.ViewModel.ThrottlingViewModel(),
                mockOfViewModelFactory.Object,
                Create.Fake.MvxMainThreadAsyncDispatcher());
        }

        public SingleOptionLinkedQuestionViewModel SingleOptionLinkedQuestionViewModel(
            IQuestionnaire questionnaire = null,
            IStatefulInterview interview = null,
            IViewModelEventRegistry eventRegistry = null,
            QuestionStateViewModel<SingleOptionLinkedQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null) => new SingleOptionLinkedQuestionViewModel(
            Mock.Of<IPrincipal>(_ =>
                _.CurrentUserIdentity == Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid())),
            Create.Storage.QuestionnaireStorage(questionnaire ?? Mock.Of<IQuestionnaire>()),
            Create.Storage.InterviewRepository(interview ?? Mock.Of<IStatefulInterview>()),
            eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
            questionState ?? Stub<QuestionStateViewModel<SingleOptionLinkedQuestionAnswered>>.WithNotEmptyValues,
            Mock.Of<QuestionInstructionViewModel>(),
            answering ?? Mock.Of<AnsweringViewModel>(),
            Create.ViewModel.ThrottlingViewModel(),
            Create.Fake.MvxMainThreadDispatcher1());

        public TextQuestionViewModel TextQuestionViewModel(
            IViewModelEventRegistry eventRegistry = null,
            IQuestionnaireStorage questionnaireStorage = null,
            IStatefulInterviewRepository interviewRepository = null,
            QuestionStateViewModel<TextQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null)
        {
            var statefulInterviewRepository = interviewRepository ?? Stub<IStatefulInterviewRepository>.WithNotEmptyValues;
            var questionnaireRepository = questionnaireStorage ?? Stub<IQuestionnaireStorage>.WithNotEmptyValues;
            var principal = Mock.Of<IPrincipal>(_ => _.CurrentUserIdentity == Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid()));
            var liteEventRegistry = eventRegistry ?? Create.Service.LiteEventRegistry();

            var questionStateViewModel = questionState ??
                                         this.QuestionState<TextQuestionAnswered>(liteEventRegistry: eventRegistry,
                                             interviewRepository: statefulInterviewRepository, questionnaireStorage: questionnaireRepository);

            return new TextQuestionViewModel(
                liteEventRegistry,
                principal,
                questionnaireRepository,
                statefulInterviewRepository,
                questionStateViewModel,
                new QuestionInstructionViewModel(questionnaireRepository, statefulInterviewRepository, 
                    new DynamicTextViewModel(eventRegistry ?? Stub<IViewModelEventRegistry>.WithNotEmptyValues, statefulInterviewRepository, new SubstitutionService(), questionnaireStorage)),
                answering ?? this.AnsweringViewModel());
        }

        public AnsweringViewModel AnsweringViewModel(ICommandService commandService = null,
            IUserInterfaceStateService userInterfaceStateService = null)
            => new AnsweringViewModel(
                commandService ?? Stub<ICommandService>.WithNotEmptyValues,
                userInterfaceStateService ?? Stub<IUserInterfaceStateService>.WithNotEmptyValues,
                Mock.Of<ILogger>());

        public ValidityViewModel ValidityViewModel(
            IViewModelEventRegistry eventRegistry = null,
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaire questionnaire = null,
            Identity entityIdentity = null)
        {
            var questionnaireRepository = Mock.Of<IQuestionnaireStorage>(
                x => x.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) == questionnaire);

            return new ValidityViewModel(
                eventRegistry ?? Create.Service.LiteEventRegistry(),
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                Create.ViewModel.ErrorMessagesViewModel(
                    questionnaireRepository: questionnaireRepository,
                    interviewRepository: interviewRepository));
        }

        public VariableViewModel VariableViewModel(
            IViewModelEventRegistry eventRegistry = null,
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaire questionnaire = null,
            Identity entityIdentity = null)
        {
            var questionnaireRepository = Mock.Of<IQuestionnaireStorage>(
                x => x.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) == questionnaire
                && x.GetQuestionnaireOrThrow(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) == questionnaire);

            return new VariableViewModel(
                questionnaireRepository,
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                eventRegistry ?? Create.Service.LiteEventRegistry());
        }

        public QuestionInstructionViewModel QuestionInstructionViewModel()
        {
            return Mock.Of<QuestionInstructionViewModel>();
        }

        public QuestionStateViewModel<T> QuestionState<T>(
            IViewModelEventRegistry liteEventRegistry = null,
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaireStorage questionnaireStorage = null) where T : QuestionAnswered
        {
            var questionnaireRepository = questionnaireStorage ?? Stub<IQuestionnaireStorage>.WithNotEmptyValues;
            liteEventRegistry = liteEventRegistry ?? Create.Service.LiteEventRegistry();
            interviewRepository = interviewRepository ?? Stub<IStatefulInterviewRepository>.WithNotEmptyValues;

            var headerViewModel = new QuestionHeaderViewModel(
                dynamicTextViewModel: Create.ViewModel.DynamicTextViewModel(eventRegistry: liteEventRegistry,
                    interviewRepository: interviewRepository,
                    questionnaireStorage: questionnaireRepository));

            var validityViewModel = new ValidityViewModel(
                liteEventRegistry: liteEventRegistry,
                interviewRepository: interviewRepository,
                errorMessagesViewModel: ErrorMessagesViewModel(questionnaireRepository, interviewRepository));
            
            var warningsViewModel = new WarningsViewModel(
                liteEventRegistry: liteEventRegistry,
                interviewRepository: interviewRepository,
                errorMessagesViewModel: ErrorMessagesViewModel(questionnaireRepository, interviewRepository));

            var commentsViewModel = new CommentsViewModel(interviewRepository: interviewRepository,
                                    commandService: Stub<ICommandService>.WithNotEmptyValues,
                                    principal: Stub<IPrincipal>.WithNotEmptyValues,
                                    eventRegistry: liteEventRegistry);

            var answersRemovedNotifier = new AnswersRemovedNotifier(liteEventRegistry);

            return new QuestionStateViewModel<T>(
                liteEventRegistry: liteEventRegistry,
                interviewRepository: interviewRepository,
                validityViewModel: validityViewModel,
                questionHeaderViewModel: headerViewModel,
                enablementViewModel: Create.ViewModel.EnablementViewModel(
                    interviewRepository: interviewRepository,
                    eventRegistry: liteEventRegistry,
                    questionnaireRepository: questionnaireRepository),
                commentsViewModel: commentsViewModel,
                answersRemovedNotifier: answersRemovedNotifier, 
                warningsViewModel: warningsViewModel);
        }

        public EnablementViewModel EnablementViewModel(IStatefulInterviewRepository interviewRepository = null,
            IViewModelEventRegistry eventRegistry = null, IQuestionnaireStorage questionnaireRepository = null)
            => new EnablementViewModel(interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                eventRegistry ?? Create.Service.LiteEventRegistry(), 
                questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                Mock.Of<IViewModelNavigationService>(),
                Mock.Of<ILogger>());

        public FilteredOptionsViewModel FilteredOptionsViewModel(
            Identity questionId,
            QuestionnaireDocument questionnaire, 
            IStatefulInterview statefulInterview)
        {
            var questionnaireRepository = Create.Fake.QuestionnaireRepositoryWithOneQuestionnaire(questionnaire);
            var interviewRepository = Create.Fake.StatefulInterviewRepositoryWith(statefulInterview);

            var result = new FilteredOptionsViewModel(questionnaireRepository, interviewRepository, new AnswerNotifier(Create.Service.LiteEventRegistry()),
                Mock.Of<ILogger>());
            result.Init(statefulInterview.Id.FormatGuid(), questionId, 30);
            return result;
        }

        public SideBarSectionsViewModel SidebarSectionsViewModel(
            QuestionnaireDocument questionnaireDocument, 
            IStatefulInterview interview, 
            ViewModelEventRegistry liteEventRegistry,
            NavigationState navigationState = null)
        {
            var questionnaire = Create.Entity.PlainQuestionnaire(questionnaireDocument, 1);
            var questionnaireRepository = SetUp.QuestionnaireRepositoryWithOneQuestionnaire(questionnaire);

            var interviewsRepository = SetUp.StatefulInterviewRepository(interview);

            var sideBarSectionViewModelsFactory = new SideBarSectionViewModelFactory(ServiceLocator.Current);
            
            navigationState = navigationState ?? Create.Other.NavigationState();

            SideBarSectionViewModel SideBarSectionViewModel()
            {
                return new SideBarSectionViewModel(interviewsRepository, questionnaireRepository, liteEventRegistry,
                    Create.ViewModel.DynamicTextViewModel(liteEventRegistry, interviewsRepository, questionnaireRepository),
                    Create.Entity.AnswerNotifier(liteEventRegistry))
                {
                    NavigationState = navigationState,
                };
            }

            var interviewStateViewModel = new InterviewStateViewModel(
                interviewsRepository,
                Mock.Of<IInterviewStateCalculationStrategy>(_ => 
                    _.GetInterviewSimpleStatus(It.IsAny<IStatefulInterview>()) == new InterviewSimpleStatus() ),
                questionnaireRepository);
           
            var coverStateViewModel = new CoverStateViewModel(
                interviewsRepository,
                Mock.Of<IGroupStateCalculationStrategy>(),
                questionnaireRepository);
            
            var groupStateViewModel = new GroupStateViewModel(
                interviewsRepository,
                Mock.Of<IGroupStateCalculationStrategy>(),
                questionnaireRepository);
            
            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<CoverStateViewModel>())
                .Returns(() => coverStateViewModel);
            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<GroupStateViewModel>())
                .Returns(() => groupStateViewModel);
            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<SideBarSectionViewModel>())
                .Returns((Func<SideBarSectionViewModel>) SideBarSectionViewModel);

            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<SideBarVirtualCoverSectionViewModel>())
                .Returns(() => new SideBarVirtualCoverSectionViewModel( Create.ViewModel.DynamicTextViewModel(
                        liteEventRegistry,
                        interviewRepository: interviewsRepository,
                        questionnaireRepository), coverStateViewModel));

            
            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<SideBarOverviewViewModel>())
                .Returns(() => new SideBarOverviewViewModel(Create.ViewModel.DynamicTextViewModel(
                    liteEventRegistry,
                    interviewRepository: interviewsRepository,
                    questionnaireRepository), interviewStateViewModel,
                    Mock.Of<AnswerNotifier>()));

            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<SideBarCompleteSectionViewModel>())
                .Returns(() => new SideBarCompleteSectionViewModel(Create.ViewModel.DynamicTextViewModel(
                        liteEventRegistry,
                        interviewRepository: interviewsRepository,
                        questionnaireRepository), interviewStateViewModel,
                        Create.Entity.AnswerNotifier(liteEventRegistry)));

            Mock.Get(ServiceLocator.Current)
                .Setup(locator => locator.GetInstance<SideBarSectionViewModel>())
                .Returns((Func<SideBarSectionViewModel>) SideBarSectionViewModel);
            SetUp.InstanceToMockedServiceLocator(interviewStateViewModel);


            var sidebarViewModel = new SideBarSectionsViewModel(
                statefulInterviewRepository: interviewsRepository,
                questionnaireRepository: questionnaireRepository,
                modelsFactory: sideBarSectionViewModelsFactory,
                eventRegistry: liteEventRegistry,
                Create.Fake.MvxMainThreadDispatcher());

            sidebarViewModel.Init("", navigationState);

            return sidebarViewModel;
        }

        public CategoricalMultiLinkedToQuestionViewModel MultiOptionLinkedToRosterQuestionViewModel(
            IQuestionnaire questionnaire = null,
            IStatefulInterview interview = null,
            IViewModelEventRegistry eventRegistry = null,
            QuestionStateViewModel<MultipleOptionsLinkedQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null)
        {
            var statefulInterviewRepository = Mock.Of<IStatefulInterviewRepository>(_ =>
                _.Get(It.IsAny<string>()) == (interview ?? Mock.Of<IStatefulInterview>()));

            var questionnaireRepository = Mock.Of<IQuestionnaireStorage>(_ =>
                _.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>(), It.IsAny<string>()) ==
                (questionnaire ?? Mock.Of<IQuestionnaire>()));

            return new CategoricalMultiLinkedToQuestionViewModel(
                questionState ?? Create.ViewModel.QuestionState<MultipleOptionsLinkedQuestionAnswered>(eventRegistry, statefulInterviewRepository, questionnaireRepository),
                questionnaireRepository,
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                statefulInterviewRepository,
                Mock.Of<IPrincipal>(_ =>
                    _.CurrentUserIdentity == Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid())),
                answering ?? Mock.Of<AnsweringViewModel>(),
                Mock.Of<QuestionInstructionViewModel>(),
                Create.ViewModel.ThrottlingViewModel(),
                Create.Service.InterviewViewModelFactory(),
                Create.Fake.MvxMainThreadAsyncDispatcher());
        }

        public CategoricalMultiLinkedToRosterTitleViewModel MultiOptionLinkedToRosterTitleViewModel(
            IQuestionnaire questionnaire = null,
            IStatefulInterview interview = null,
            IViewModelEventRegistry eventRegistry = null,
            QuestionStateViewModel<MultipleOptionsLinkedQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null,
            IUserInteractionService userInteraction = null)
        {
            var statefulInterviewRepository = SetUp.StatefulInterviewRepository(interview ?? Mock.Of<IStatefulInterview>());

            var questionnaireRepository = SetUp.QuestionnaireRepositoryWithOneQuestionnaire(questionnaire ?? Mock.Of<IQuestionnaire>());

            userInteraction = userInteraction ?? Mock.Of<IUserInteractionService>();
            var mockOfViewModelFactory = new Mock<IInterviewViewModelFactory>();
            mockOfViewModelFactory.Setup(x => x.GetNew<CategoricalMultiOptionViewModel<RosterVector>>()).Returns(() =>
                new CategoricalMultiOptionViewModel<RosterVector>(userInteraction, Create.ViewModel.AttachmentViewModel()));
            
            return new CategoricalMultiLinkedToRosterTitleViewModel(
                questionState ?? Create.ViewModel.QuestionState<MultipleOptionsLinkedQuestionAnswered>(eventRegistry, statefulInterviewRepository, questionnaireRepository),
                questionnaireRepository,
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                statefulInterviewRepository,
                Mock.Of<IPrincipal>(_ =>
                    _.CurrentUserIdentity == Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid())),
                answering ?? Mock.Of<AnsweringViewModel>(),
                Mock.Of<QuestionInstructionViewModel>(),
                Create.ViewModel.ThrottlingViewModel(),
                mockOfViewModelFactory.Object,
                Create.Fake.MvxMainThreadAsyncDispatcher());
        }

        public VibrationViewModel VibrationViewModel(IViewModelEventRegistry eventRegistry = null,
            IEnumeratorSettings enumeratorSettings = null, IVibrationService vibrationService = null)
            => new VibrationViewModel(
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                enumeratorSettings ?? Mock.Of<IEnumeratorSettings>(), 
                vibrationService ?? Mock.Of<IVibrationService>());

        public SingleOptionQuestionOptionViewModel SingleOptionQuestionOptionViewModel(int? value = null)
        {
            return new SingleOptionQuestionOptionViewModel(Create.ViewModel.AttachmentViewModel())
            {
                Value = value ?? 0
            };
        }

        public SpecialValuesViewModel SpecialValues(
            FilteredOptionsViewModel optionsViewModel = null,
            IMvxMainThreadAsyncDispatcher mvxMainThreadDispatcher = null,
            IStatefulInterviewRepository interviewRepository = null)
        {
            var serviceLocator = new Mock<IServiceLocator>();
            serviceLocator.Setup(s => s.GetInstance<SingleOptionQuestionOptionViewModel>())
                .Returns(() => Create.ViewModel.SingleOptionQuestionOptionViewModel());

            return new SpecialValuesViewModel(
                optionsViewModel ?? Mock.Of<FilteredOptionsViewModel>(), 
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                Create.Service.InterviewViewModelFactory(serviceLocator: serviceLocator.Object));
        }

        public SideBarCompleteSectionViewModel SideBarCompleteSectionViewModel()
        {
            return new SideBarCompleteSectionViewModel(
                Create.ViewModel.DynamicTextViewModel(),
                Create.ViewModel.InterviewStateViewModel(),
                Create.Entity.AnswerNotifier(Create.Service.LiteEventRegistry()));
        }

        public WaitingForSupervisorActionViewModel WaitingForSupervisorActionViewModel(
            IDashboardItemsAccessor dashboardItemsAccessor = null,
            IInterviewViewModelFactory viewModelFactory = null,
            IMvxMessenger messenger = null)
            => new WaitingForSupervisorActionViewModel(dashboardItemsAccessor ?? Mock.Of<IDashboardItemsAccessor>(),
                viewModelFactory ?? Create.Service.SupervisorInterviewViewModelFactory());

        public SupervisorInterviewDashboardViewModel SupervisorDashboardInterviewViewModel(Guid? interviewId = null,
            IPrincipal principal = null,
            IPlainStorage<InterviewerDocument> interviewers = null)
        {
            var viewModel = new SupervisorInterviewDashboardViewModel(
                Mock.Of<IServiceLocator>(),
                Mock.Of<IAuditLogService>(),
                Mock.Of<IViewModelNavigationService>(),
                principal ?? Mock.Of<IPrincipal>(x => x.CurrentUserIdentity == Create.Other.SupervisorIdentity(null, null, null, null)),
                interviewers ?? new InMemoryPlainStorage<InterviewerDocument>(Mock.Of<ILogger>()),
                Mock.Of<IUserInteractionService>(),
                Mock.Of<IMapInteractionService>());

            if (interviewId.HasValue)
            {
                viewModel.Init(Create.Entity.InterviewView(interviewId: interviewId,
                    questionnaireId: Create.Entity.QuestionnaireIdentity().ToString()), new List<PrefilledQuestion>());
            }

            return viewModel;
        }

        public FinishInstallationViewModel FinishInstallationViewModel(
            IViewModelNavigationService viewModelNavigationService = null,
            IPrincipal principal = null,
            IPasswordHasher passwordHasher = null,
            IPlainStorage<SupervisorIdentity> interviewersPlainStorage = null,
            IDeviceSettings deviceSettings = null,
            ISupervisorSynchronizationService synchronizationService = null,
            ILogger logger = null,
            IQRBarcodeScanService qrBarcodeScanService = null,
            ISerializer serializer = null,
            IUserInteractionService userInteractionService = null,
            IAuditLogService auditLogService = null)
            => new FinishInstallationViewModel(viewModelNavigationService ?? Mock.Of<IViewModelNavigationService>(),
                principal ?? Mock.Of<IPrincipal>(x => x.CurrentUserIdentity == Create.Other.SupervisorIdentity(null, null, null, null)),
                passwordHasher?? Mock.Of<IPasswordHasher>(),
                interviewersPlainStorage ?? Mock.Of<IPlainStorage<SupervisorIdentity>>(),
                deviceSettings ?? Mock.Of <IDeviceSettings>(),
                synchronizationService ?? Mock.Of<ISupervisorSynchronizationService>(),
                logger ?? Mock.Of<ILogger>(),
                qrBarcodeScanService ?? Mock.Of<IQRBarcodeScanService>(),
                serializer?? Mock.Of <ISerializer>(),
                userInteractionService?? Mock.Of<IUserInteractionService>(),
                auditLogService ?? Mock.Of<IAuditLogService>(),
                Mock.Of<IDeviceInformationService>(),
                Mock.Of<IWorkspaceService>());

        public ConnectedDeviceSynchronizationViewModel ConnectedDeviceSynchronizationViewModel()
            => new ConnectedDeviceSynchronizationViewModel();

        public ThrottlingViewModel ThrottlingViewModel(IUserInterfaceStateService userInterfaceStateService = null)
        {
            return new ThrottlingViewModel(userInterfaceStateService ?? Mock.Of<IUserInterfaceStateService>())
            {
                ThrottlePeriod = 0
            };
        }
        
        public SearchViewModel SearchViewModel(
            IPrincipal principal = null,
            IViewModelNavigationService viewModelNavigationService = null,
            IInterviewViewModelFactory viewModelFactory = null,
            IPlainStorage<InterviewView> interviewViewRepository = null,
            IPlainStorage<PrefilledQuestionView> identifyingQuestionsRepo = null,
            IAssignmentDocumentsStorage assignmentsRepository = null,
            IMvxMessenger messenger = null)
            => new SearchViewModel(
                principal ?? Mock.Of<IPrincipal>(),
                viewModelNavigationService ?? Mock.Of<IViewModelNavigationService>(),
                viewModelFactory ?? Mock.Of<IInterviewViewModelFactory>(),
                interviewViewRepository ?? Mock.Of<IPlainStorage<InterviewView>>(m => m.LoadAll() == Enumerable.Empty<InterviewView>().ToReadOnlyCollection()),
                identifyingQuestionsRepo ?? Mock.Of<IPlainStorage<PrefilledQuestionView>>(m => m.LoadAll() == Enumerable.Empty<PrefilledQuestionView>().ToReadOnlyCollection()),
                assignmentsRepository ?? Mock.Of<IAssignmentDocumentsStorage>(),
                Create.Fake.MvxMainThreadDispatcher());

        public RosterViewModel RosterViewModel(IStatefulInterviewRepository interviewRepository = null,
            IInterviewViewModelFactory interviewViewModelFactory = null,
            IViewModelEventRegistry eventRegistry = null,
            IQuestionnaireStorage questionnaireRepository = null)
        {
            return new RosterViewModel(interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                interviewViewModelFactory ?? Mock.Of<IInterviewViewModelFactory>(),
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                Create.Fake.MvxMainThreadDispatcher());
        }

        public FlatRosterViewModel FlatRosterViewModel(IStatefulInterviewRepository interviewRepository = null,
            IInterviewViewModelFactory viewModelFactory = null,
            IViewModelEventRegistry eventRegistry = null,
            ICompositeCollectionInflationService compositeCollectionInflationService = null)
        {
            return new FlatRosterViewModel(
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                viewModelFactory?? Mock.Of<IInterviewViewModelFactory>(),
                eventRegistry ?? Mock.Of<IViewModelEventRegistry>(),
                compositeCollectionInflationService ?? Mock.Of<ICompositeCollectionInflationService>(),
                Create.Fake.MvxMainThreadDispatcher()
                );
        }

        public FlatRosterTitleViewModel FlatRosterTitleViewModel(IStatefulInterviewRepository statefulInterviewRepository,
            IQuestionnaireStorage questionnaireStorage)
        {
            return new FlatRosterTitleViewModel(Create.ViewModel.DynamicTextViewModel(interviewRepository: statefulInterviewRepository, questionnaireStorage: questionnaireStorage),
                Create.ViewModel.EnablementViewModel(statefulInterviewRepository, questionnaireRepository: questionnaireStorage));
        }

        public CategoricalYesNoOptionViewModel YesNoQuestionOptionViewModel(IUserInteractionService userInteractionService)
            => new CategoricalYesNoOptionViewModel(userInteractionService, Create.ViewModel.AttachmentViewModel());

        public CategoricalMultiOptionViewModel CategoricalMultiOptionViewModel(IUserInteractionService userInteractionService = null)
            => new CategoricalMultiOptionViewModel(userInteractionService ?? Mock.Of<IUserInteractionService>(), Create.ViewModel.AttachmentViewModel());

        public CategoricalComboboxAutocompleteViewModel CategoricalComboboxAutocompleteViewModel(
            FilteredOptionsViewModel filteredOptionsViewModel, IQuestionStateViewModel questionState = null) =>
            new CategoricalComboboxAutocompleteViewModel(
                questionState ?? Create.ViewModel.QuestionState<MultipleOptionsQuestionAnswered>(), filteredOptionsViewModel, 
                false);

        public FilteredSingleOptionQuestionViewModel FilteredSingleOptionQuestionViewModel(
            Identity questionId,
            QuestionnaireDocument questionnaire,
            IStatefulInterview interview,
            IViewModelEventRegistry eventRegistry = null,
            FilteredOptionsViewModel filteredOptionsViewModel = null,
            IPrincipal principal = null,
            QuestionStateViewModel<SingleOptionQuestionAnswered> questionStateViewModel = null,
            AnsweringViewModel answering = null,
            QuestionInstructionViewModel instructionViewModel = null)
        {
            var interviewRepository = Create.Fake.StatefulInterviewRepositoryWith(interview);
            return new FilteredSingleOptionQuestionViewModel(
                interviewRepository,
                eventRegistry ?? Create.Service.LiteEventRegistry(),
                filteredOptionsViewModel ?? Create.ViewModel.FilteredOptionsViewModel(questionId, questionnaire, interview),
                principal ?? Mock.Of<IPrincipal>(),
                questionStateViewModel ?? Create.ViewModel.QuestionState<SingleOptionQuestionAnswered>(interviewRepository: interviewRepository),
                answering ?? Create.ViewModel.AnsweringViewModel(),
                instructionViewModel ?? Create.ViewModel.QuestionInstructionViewModel(),
                Create.Service.InterviewViewModelFactory(),
                Create.ViewModel.AttachmentViewModel());
        }

        public TimestampQuestionViewModel TimestampQuestionViewModel(
            IStatefulInterview interview,
            AnsweringViewModel answering = null)
        {
            var answeringViewModel = answering ?? Create.ViewModel.AnsweringViewModel();
            var statefulInterviewRepository = SetUp.StatefulInterviewRepository(interview);


            var principal = Mock.Of<IPrincipal>(x => x.CurrentUserIdentity == Mock.Of<IUserIdentity>(u => u.Id == Id.gA.ToString()));
            return new TimestampQuestionViewModel(principal,
                statefulInterviewRepository,
                Create.ViewModel.QuestionState<DateTimeQuestionAnswered>(interviewRepository: statefulInterviewRepository),
                Create.ViewModel.QuestionInstructionViewModel(),
                answeringViewModel,
                Create.Service.LiteEventRegistry());
        }

        public DashboardNotificationsViewModel DashboardNotificationsViewModel(IViewModelNavigationService navigationService = null)
        {
            return new DashboardNotificationsViewModel(navigationService ?? Mock.Of<IViewModelNavigationService>(),
                Mock.Of<IEnumeratorSettings>(),
                Mock.Of<IClock>());
        }

        public LocalSynchronizationViewModel LocalSynchronizationViewModel()
        {
            return new LocalSynchronizationViewModel(
                Mock.Of<ISynchronizationCompleteSource>(),
                Mock.Of<ITabletDiagnosticService>(),
                Mock.Of<ILogger>());
        }
    }
}
