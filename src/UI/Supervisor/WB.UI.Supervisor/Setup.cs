using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Android.Widget;
using AndroidX.DrawerLayout.Widget;
using Autofac;
using Autofac.Features.ResolveAnything;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Converters;
using MvvmCross.IoC;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using MvvmCross.Views;
using WB.Core.BoundedContexts.Supervisor;
using WB.Core.BoundedContexts.Supervisor.Services;
using WB.Core.BoundedContexts.Supervisor.ViewModel;
using WB.Core.BoundedContexts.Supervisor.ViewModel.Dashboard;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure;
using WB.Core.Infrastructure.Modularity;
using WB.Core.Infrastructure.Modularity.Autofac;
using WB.Core.Infrastructure.Ncqrs;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.Enumerator;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.UI.Supervisor.ServiceLocation;
using WB.UI.Shared.Enumerator;
using WB.UI.Shared.Enumerator.Activities;
using WB.UI.Shared.Enumerator.Converters;
using WB.UI.Shared.Enumerator.CustomBindings;
using WB.UI.Shared.Enumerator.Services;
using WB.UI.Shared.Enumerator.Services.Autofac.MvvmCross;
using WB.UI.Shared.Enumerator.Services.Logging;
using WB.UI.Shared.Enumerator.Utils;
using WB.UI.Supervisor.Activities;
using WB.UI.Supervisor.Activities.Interview;
using WB.UI.Supervisor.MvvmBindings;
using WB.UI.Supervisor.Services.Implementation;

namespace WB.UI.Supervisor
{
    public class Setup : EnumeratorSetup<SupervisorMvxApplication>
    {
        IModule[] modules;

        public Setup()
        {
        }

        protected override IMvxViewsContainer InitializeViewLookup(IDictionary<Type, Type> viewModelViewLookup, IMvxIoCProvider iocProvider)
        {
            var lookup = base.InitializeViewLookup(viewModelViewLookup, iocProvider);

            var our = new Dictionary<Type, Type>()
            {
                {typeof(LoginViewModel), typeof(LoginActivity)},
                {typeof(FinishInstallationViewModel), typeof(FinishInstallationActivity)},
                {typeof(DiagnosticsViewModel),typeof(DiagnosticsActivity) },
                {typeof(DashboardViewModel),typeof(DashboardActivity) },
                {typeof(SupervisorInterviewViewModel),typeof(InterviewActivity) },
                {typeof(SupervisorOfflineSyncViewModel), typeof(OfflineSupervisorSyncActitivy) },
                {typeof(SupervisorResolveInterviewViewModel), typeof (SupervisorCompleteFragment)},
                {typeof(MapsViewModel), typeof (MapsActivity)},
                {typeof(PhotoViewViewModel), typeof(PhotoViewActivity) },
                {typeof(SearchViewModel), typeof(SupervisorSearchActivity)}
#if !EXCLUDEEXTENSIONS
                ,{typeof (Shared.Extensions.ViewModels.GeographyEditorViewModel), typeof (Shared.Extensions.Activities.GeographyEditorActivity)}
                ,{typeof (Shared.Extensions.ViewModels.SupervisorMapDashboardViewModel), typeof (Shared.Extensions.Activities.SupervisorMapDashboardActivity)}
                ,{typeof (Shared.Extensions.ViewModels.AssignmentMapViewModel), typeof (Shared.Extensions.Activities.AssignmentMapActivity)}
#endif
            };

            lookup.AddAll(our);
            return lookup;
        }

        protected override void FillValueConverters(IMvxValueConverterRegistry registry)
        {
            base.FillValueConverters(registry);

            registry.AddOrOverwrite("Localization", new SupervisorLocalizationValueConverter());
            registry.AddOrOverwrite("ValidationStyleBackground", new TextEditValidationStyleBackgroundConverter());
            registry.AddOrOverwrite("SelectedBg", new SelectedBackgroundConverter());
        }

        protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
        {
            registry.RegisterCustomBindingFactory<ImageView>("CompanyLogo", view => new ImageCompanyLogoBinding(view));
            registry.RegisterCustomBindingFactory<ProgressBar>("ShowProgress", (view) => new ProgressBarIndeterminateBinding(view));
            registry.RegisterCustomBindingFactory<DrawerLayout>("Lock", (view) => new DrawerLockModeBinding(view));
            base.FillTargetFactories(registry);
        }

        protected override IMvxIoCProvider CreateIocProvider()
        {
            return new MvxIoCProviderWithParent(CreateIocOptions(),this.CreateAndInitializeIoc());
        }

        protected override void InitializeApp(IMvxApplication app)
        {
            base.InitializeApp(app);
            
            string arcgisruntimeKey = ApplicationContext.Resources.GetString(Resource.String.arcgisruntime_key);
            if (!string.IsNullOrEmpty(arcgisruntimeKey))
            {
                ServiceLocator.Current.GetInstance<IMapInteractionService>().SetLicenseKey(arcgisruntimeKey);
            }
            
            string arcgisruntimeApiKey = ApplicationContext.Resources.GetString(Resource.String.arcgisruntime_api_key);
            if (!string.IsNullOrEmpty(arcgisruntimeApiKey))
            {
                ServiceLocator.Current.GetInstance<IMapInteractionService>().SetApiKey(arcgisruntimeApiKey);
            }

            var status = new UnderConstructionInfo();
            status.Run();

            foreach (var module in modules.OfType<IInitModule>())
            {
                module.Init(ServiceLocator.Current, status).Wait();
            }

            status.Finish();
            //base.InitializeFirstChance();
        }

        private IContainer CreateAndInitializeIoc()
        {            
            modules = new IModule[] {
                new NcqrsModule(),
                new InfrastructureModuleMobile(),
                new DataCollectionSharedKernelModule(),
                new EnumeratorIocRegistrationModule(),
                new EnumeratorUIModule(),
                new EnumeratorSharedKernelModule(),
                new SupervisorInfrastructureModule(),
                new SupervisorBoundedContextModule(), 
                new SupervisorUiModule(),
#if !EXCLUDEEXTENSIONS                
            new WB.UI.Shared.Extensions.MapExtensionsModule(),
            new WB.UI.Shared.Extensions.SupervisorMapExtensionsModule(),
#endif                
            };

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
            foreach (var module in modules)
            {
                builder.RegisterModule(module.AsAutofac());
            }

            builder.RegisterType<NLogLogger>().As<ILogger>();

            builder.RegisterType<SupervisorSettings>()
                .As<IEnumeratorSettings>()
                .As<IRestServiceSettings>()
                .As<IDeviceSettings>()
                .As<ISupervisorSettings>()
                .WithParameter("backupFolder", AndroidPathUtils.GetPathToSupervisorSubfolderInExternalDirectory("Backup"))
                .WithParameter("restoreFolder", AndroidPathUtils.GetPathToSupervisorSubfolderInExternalDirectory("Restore"));
            
            var container = builder.Build();
            ServiceLocator.SetLocatorProvider(() => new AutofacServiceLocatorAdapter(container));
            return container;
        }

        protected override IEnumerable<Assembly> AndroidViewAssemblies
        {
            get
            {
                var toReturn = base.AndroidViewAssemblies;

                // Add assemblies with other views we use.  When the XML is inflated
                // MvvmCross knows about the types and won't complain about them.  This
                // speeds up inflation noticeably.
                return toReturn;
            }
        }

        public override IEnumerable<Assembly> GetViewModelAssemblies()
        {
            return base.GetViewModelAssemblies().Union(new[]
            {
                typeof(Setup).Assembly,
                typeof(DashboardViewModel).Assembly,
#if !EXCLUDEEXTENSIONS
                typeof(WB.UI.Shared.Extensions.ViewModels.GeographyEditorViewModel).Assembly
#endif                
            });
        }
    }
}
