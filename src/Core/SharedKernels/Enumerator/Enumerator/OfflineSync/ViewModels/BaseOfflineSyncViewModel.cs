﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Commands;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Entities;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Services;
using WB.Core.SharedKernels.Enumerator.Properties;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using Xamarin.Essentials;

namespace WB.Core.SharedKernels.Enumerator.OfflineSync.ViewModels
{
    [ExcludeFromCodeCoverage()] // TODO: remove attribute when UI binding completed
    public abstract class BaseOfflineSyncViewModel<TInputArgs> : BaseViewModel<TInputArgs>
    {
        protected readonly IPermissionsService permissions;
        protected readonly INearbyConnection nearbyConnection;
        protected CancellationTokenSource cancellationTokenSource = null;

        private readonly IDisposable nearbyConnectionSubscription;

        public IMvxAsyncCommand StartDiscoveryAsyncCommand => new MvxAsyncCommand(StartDiscoveryAsync);

        protected BaseOfflineSyncViewModel(IPrincipal principal,
            IViewModelNavigationService viewModelNavigationService,
            IPermissionsService permissions,
            INearbyConnection nearbyConnection)
            : base(principal, viewModelNavigationService)
        {
            this.permissions = permissions;
            this.nearbyConnection = nearbyConnection;
            nearbyConnectionSubscription = this.nearbyConnection.Events.Subscribe(HandleConnectionEvents);
        }

        private bool shouldStartAdvertising = true;

        public bool ShouldStartAdvertising
        {
            get => this.shouldStartAdvertising;
            set => this.SetProperty(ref this.shouldStartAdvertising, value);
        }

        private bool doesStartDiscoveryExecuted = false;

        private async Task StartDiscoveryAsync()
        {
            if (doesStartDiscoveryExecuted)
                return;

            
            
            try
            {
                doesStartDiscoveryExecuted = true;

                if (!ShouldStartAdvertising)
                    return;
                
                var isAllowedBluetooth = await TryBluetoothPermission().ConfigureAwait(false);
                if (!isAllowedBluetooth)
                    return;

                var isAllowedNearby = await TryNearbyWifiDevicesPermission().ConfigureAwait(false);
                if (!isAllowedNearby)
                    return;
                
                var isAllowedGetLocation = await TryRequestLocationPermission().ConfigureAwait(false);
                if (!isAllowedGetLocation)
                    return;

                await this.OnStartDiscovery();
            }
            finally
            {
                doesStartDiscoveryExecuted = false;
            }
        }

        protected async Task<bool> TryBluetoothPermission()
        {
            try
            {
                await permissions.AssureHasBluetoothPermissionOrThrow().ConfigureAwait(false);
            }
            catch (MissingPermissionsException)
            {
                ShouldStartAdvertising = false;
                this.OnConnectionError(EnumeratorUIResources.BluetoothPermissionRequired,
                    ConnectionStatusCode.MissingPermissionBluetoothAdvertise);
                return false;
            }

            return true;
        }
        
        protected abstract Task<bool> TryNearbyWifiDevicesPermission();
        
        private async Task<bool> TryRequestLocationPermission()
        {
            try
            {
                await this.permissions.AssureHasPermissionOrThrow<Permissions.LocationWhenInUse>().ConfigureAwait(false);
            }
            catch (MissingPermissionsException)
            {
                ShouldStartAdvertising = false;
                this.OnConnectionError(EnumeratorUIResources.LocationPermissionRequired,
                    ConnectionStatusCode.MissingPermissionAccessCoarseLocation);
                return false;
            }

            return true;
        }

        protected async void HandleConnectionEvents(INearbyEvent @event)
        {
            switch (@event)
            {
                case NearbyEvent.EndpointFound endpointFound:
                    await this.RequestConnectionAsync(endpointFound.Endpoint, endpointFound.EndpointInfo.EndpointName)
                        .ConfigureAwait(false);
                    break;
                case NearbyEvent.InitiatedConnection iniConnection:
                    await this.InitializeConnectionAsync(iniConnection.Endpoint, iniConnection.Info.EndpointName)
                        .ConfigureAwait(false);
                    break;
                case NearbyEvent.Connected connected:
                    this.OnDeviceConnected(connected.Name);
                    break;
                case NearbyEvent.Disconnected disconnected:
                    this.OnDeviceDisconnected(disconnected.Name);
                    break;
                case NearbyEvent.EndpointLost endpointLost:
                    break;
            }
        }

        private async Task InitializeConnectionAsync(string endpoint, string name)
        {
            this.OnDeviceConnectionAccepting(name);
            var connectionStatus = await this.nearbyConnection.AcceptConnectionAsync(endpoint);

            if (connectionStatus.Status == ConnectionStatusCode.StatusAlreadyConnectedToEndpoint ||
                connectionStatus.Status == ConnectionStatusCode.StatusOutOfOrderApiCall)
            {
                this.OnDeviceConnectionAccepted(name);
                return;
            }

            if (!connectionStatus.IsSuccess)
                this.OnConnectionError(connectionStatus.StatusMessage, connectionStatus.Status);
            else
                this.OnDeviceConnectionAccepted(name);
        }

        private async Task RequestConnectionAsync(string endpoint, string name)
        {
            this.OnDeviceFound(name);

            var interviewerName = this.Principal.CurrentUserIdentity.Name;

            NearbyStatus connectionStatus = null;
            try
            {
                connectionStatus = await this.nearbyConnection
                     .RequestConnectionAsync(interviewerName, endpoint, cancellationTokenSource.Token);
            }
            catch (NullReferenceException)
            {
                //research the cause of NRE
                //occurred with second call
            }

            if (connectionStatus == null ||
               connectionStatus.Status == ConnectionStatusCode.StatusAlreadyConnectedToEndpoint ||
               connectionStatus.Status == ConnectionStatusCode.StatusOutOfOrderApiCall)
                return;

            if (!connectionStatus.IsSuccess)
                this.OnConnectionError(connectionStatus.StatusMessage, connectionStatus.Status);
            else
                this.OnDeviceConnectionRequested(name);
        }

        protected abstract Task OnStartDiscovery();

        protected virtual void OnDeviceFound(string name)
        {
        }

        protected virtual void OnDeviceConnectionRequested(string name)
        {
        }

        protected virtual void OnDeviceConnectionAccepting(string name)
        {
        }

        protected virtual void OnDeviceConnectionAccepted(string name)
        {
        }

        protected virtual void OnDeviceConnected(string name)
        {
        }

        protected virtual void OnDeviceDisconnected(string name)
        {
        }

        protected abstract void OnConnectionError(string errorMessage, ConnectionStatusCode errorCode);

        protected string GetServiceName()
        {
            //var normalizedEndpoint = new Uri(this.settings.Endpoint).ToString().TrimEnd('/').ToLower();

            return this.GetDeviceIdentification();
        }

        protected abstract string GetDeviceIdentification();

        public override void Dispose()
        {
            base.Dispose();
            cancellationTokenSource?.Dispose();
            nearbyConnectionSubscription?.Dispose();
        }
    }
}
