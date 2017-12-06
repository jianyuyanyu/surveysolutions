using System;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross.Core.ViewModels;
using Plugin.Permissions.Abstractions;
using WB.Core.BoundedContexts.Interviewer.Properties;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.MapService;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels;

namespace WB.Core.BoundedContexts.Interviewer.Views
{
    public class MapsViewModel : BaseViewModel
    {
        private readonly IViewModelNavigationService viewModelNavigationService;
        private readonly ILogger logger;
        private readonly IMapService mapService;
        private readonly IPermissionsService permissions;
        private readonly IUserInteractionService userInteractionService;

        public MapSynchronizationViewModel Synchronization { set; get; }

        public MapsViewModel(
            IViewModelNavigationService viewModelNavigationService,
            IPrincipal principal,
            MapSynchronizationViewModel synchronization,
            IMapService mapService,
            IPermissionsService permissions,
            IUserInteractionService userInteractionService,
            ILogger logger)
            : base(principal, viewModelNavigationService)
        {
            this.viewModelNavigationService = viewModelNavigationService;
            this.logger = logger;
            this.Synchronization = synchronization;
            this.Synchronization.SyncCompleted += this.Refresh;
            this.mapService = mapService;
            this.permissions = permissions;
            this.userInteractionService = userInteractionService;

        }

        public IMvxCommand SignOutCommand => new MvxCommand(this.SignOut);

        private void SignOut()
        {
            this.Synchronization.CancelSynchronizationCommand.Execute();
            this.viewModelNavigationService.SignOutAndNavigateToLogin();
        }

        public IMvxCommand NavigateToDashboardCommand => new MvxAsyncCommand(async () => await this.viewModelNavigationService.NavigateToDashboard());

        private MvxObservableCollection<MapItem> uiItems = new MvxObservableCollection<MapItem>();
        public MvxObservableCollection<MapItem> Maps
        {
            get => this.uiItems;
            protected set => this.RaiseAndSetIfChanged(ref this.uiItems, value);
        }

        private IMvxAsyncCommand mapSynchronizationCommand;
        public IMvxAsyncCommand MapSynchronizationCommand
        {
            get
            {
                return mapSynchronizationCommand ??
                       (mapSynchronizationCommand = new MvxAsyncCommand(this.RunMapSyncAsync,
                           () => !this.Synchronization.IsSynchronizationInProgress));
            }
        }

        public string MapsTitle => InterviewerUIResources.Maps_Title;

        
        private async Task RunMapSyncAsync()
        {
            if (this.viewModelNavigationService.HasPendingOperations)
            {
                this.viewModelNavigationService.ShowWaitMessage();
                return;
            }

            try
            {
                await this.permissions.AssureHasPermission(Permission.Storage);
            }
            catch (MissingPermissionsException e) when (e.Permission == Permission.Storage)
            {
                this.userInteractionService.ShowToast(UIResources.MissingPermissions_Storage);
                return;
            }

            this.Synchronization.Synchronize();
        }

        private bool isInProgress;
        public bool IsInProgress
        {
            get => this.isInProgress;
            set
            {
                this.isInProgress = value;
                RaisePropertyChanged(); 
            }
        }

        private void Refresh(object sender, EventArgs e)
        {
            UpdateUiItems();
        }

        
        protected void UpdateUiItems() => Task.Run(() =>
        {
            this.IsInProgress = true;

            try
            {
                var newItems = this.mapService.GetAvailableMaps().Select(x => new MapItem()
                {
                    MapName = x.MapName,
                    CreationDate = x.CreationDate,
                    Size = x.Size
                }  
                ).ToList();
                this.Maps = new MvxObservableCollection<MapItem>(newItems);
            }
            finally
            {
                this.IsInProgress = false;
            }
            
        });

        public IMvxCommand NavigateToDiagnosticsPageCommand =>
            new MvxCommand(() => this.viewModelNavigationService.NavigateTo<DiagnosticsViewModel>());

        public override void Load()
        {
            this.Synchronization.Init();
            UpdateUiItems();
        }
    }
}