using Chance.MvvmCross.Plugins.UserInteraction;
using Cirrious.MvvmCross.ViewModels;
using MyShuttle.Client.Core.DocumentResponse;
using MyShuttle.Client.Core.Infrastructure.Abstractions.Services;
using MyShuttle.Client.Core.Model;
using MyShuttle.Client.Core.ServiceAgents.Interfaces;
using MyShuttle.Client.Core.Settings;
using MyShuttle.Client.Core.ViewModels;
using MyShuttle.Client.Core.ViewModels.InterfacesForDependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyShuttle.Client.W10.UniversalApp.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        public string MapToken => _applicationSettingService.UniversalAppBingMapsToken;
        public IVehiclesByDistanceViewModel VehiclesByDistanceViewModel { get; private set; }
        public IVehiclesByPriceViewModel VehiclesByPriceViewModel { get; private set; }
        public IMyRidesViewModel MyRidesViewModel { get; private set; }
        public ICommand NavigateToVehicleDetailsCommand { get; set; }
        public ICommand NavigateToRideDetailsCommand { get; set; }
        private readonly IMyShuttleClient _myShuttleClient;
        private readonly IUserInteraction _userInteraction;
        private readonly IApplicationSettingServiceSingleton _applicationSettingService;
        private readonly ILocationServiceSingleton _locationService;
        private ICommand _requestVehicleCommand;
        private Location _currentLocation;
        public ICommand RequestVehicleCommand
        {
            get { return this._requestVehicleCommand; }
        }

        public MainViewModel(
            IVehiclesByDistanceViewModel vehiclesByDistanceViewModel,
            IVehiclesByPriceViewModel vehiclesByPriceViewModel,
            IMyRidesViewModel myRidesViewModel,
            ILocationServiceSingleton locationService,
            IApplicationSettingServiceSingleton applicationSettingService,
            IMyShuttleClient myShuttleClient,
            IUserInteraction userInteraction)
        {
            VehiclesByDistanceViewModel = vehiclesByDistanceViewModel;
            VehiclesByPriceViewModel = vehiclesByPriceViewModel;
            MyRidesViewModel = myRidesViewModel;
            _myShuttleClient = myShuttleClient;
            _userInteraction = userInteraction;
            _locationService = locationService;
            _applicationSettingService = applicationSettingService;
            VehiclesByDistanceViewModel.PropertyChanged += VehiclesByDistanceViewModel_PropertyChanged;
            VehiclesByPriceViewModel.PropertyChanged += VehiclesByPriceViewModel_PropertyChanged;
            MyRidesViewModel.PropertyChanged += MyRidesViewModel_PropertyChanged;

            InitializeCommands();
        }

        public async void Init()
        {
            _currentLocation = await _locationService.CalculatePositionAsync();
        }

        private void MyRidesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsLoadingMyLastRides")
                RaisePropertyChanged(() => MyRides);
        }

        private void VehiclesByDistanceViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsLoadingFilteredVehicles")
            {
                RaisePropertyChanged(() => VehiclesByDistance);
                RaisePropertyChanged(() => Vehicles);
                RaisePropertyChanged(() => FirstVehicle);
            }
        }

        private void VehiclesByPriceViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsLoadingFilteredVehicles")
                RaisePropertyChanged(() => VehiclesByPrice);
        }

        public ObservableCollection<Vehicle> VehiclesByDistance
        {
            get
            {
                var skip = CurrentViewState == 0 ? 1 : 0;

                return VehiclesByDistanceViewModel.FilteredVehicles != null ?
                    new ObservableCollection<Vehicle>(VehiclesByDistanceViewModel.FilteredVehicles.Take(GetNumberVehicles() + skip).Skip(skip)) :
                    null;
            }
        }

        public Vehicle FirstVehicle { get { return VehiclesByDistanceViewModel.FilteredVehicles != null ? VehiclesByDistanceViewModel.FilteredVehicles[0] : null; } }


        public ObservableCollection<Vehicle> Vehicles
        {
            get
            {
                return Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons") ? null : VehiclesByDistance;
            }
        }

        public ObservableCollection<Vehicle> VehiclesByPrice
        {
            get
            {
                return VehiclesByPriceViewModel.FilteredVehicles != null ?
                    new ObservableCollection<Vehicle>(VehiclesByPriceViewModel.FilteredVehicles.Take(GetNumberVehicles())) :
                null;
            }
        }

        public ObservableCollection<Ride> MyRides
        {
            get
            {
                return MyRidesViewModel.MyLastRides != null ?
                    new ObservableCollection<Ride>(MyRidesViewModel.MyLastRides.Take(GetNumberRides())) :
                null;
            }
        }

        private int _currentViewState;

        public int CurrentViewState
        {
            get
            {
                return _currentViewState;
            }
            set
            {
                _currentViewState = value;
                RaisePropertyChanged(() => VehiclesByPrice);
                RaisePropertyChanged(() => VehiclesByDistance);
                RaisePropertyChanged(() => MyRides);
            }
        }
        private bool _isLoadingVehicle;

        public bool IsLoadingVehicle
        {
            get
            {
                return _isLoadingVehicle;
            }

            set
            {
                _isLoadingVehicle = value;
                RaisePropertyChanged(() => IsLoadingVehicle);
            }
        }

        private int GetNumberVehicles()
        {
            return new[] { 4, 2, 3, 3, 4, 5 }[CurrentViewState];
        }

        private int GetNumberRides()
        {
            return new[] { 4, 2, 3, 4, 4, 4 }[CurrentViewState];
        }

        private void InitializeCommands()
        {
            NavigateToVehicleDetailsCommand = new MvxCommand<int>(NavigateToVehicleDetails);
            NavigateToRideDetailsCommand = new MvxCommand<int>(NavigateToRideDetails);
            _requestVehicleCommand = new MvxCommand(this.RequestVehicle);

        }

        private async void RequestVehicle()
        {
            await this.NotificateRequest();
            await _userInteraction.AlertAsync("Vehicle requested. Thanks!");
        }

        private async Task NotificateRequest()
        {
            this.IsLoadingVehicle = true;

            await _myShuttleClient.NotificationsService.RequestVehicleAsync(CommonAppSettings.FixedEmployeeId,
                CommonAppSettings.FixedDriverId,
                this._currentLocation.Latitude,
                this._currentLocation.Longitude);

            this.IsLoadingVehicle = false;
        }

        private void NavigateToVehicleDetails(int currentVehicleId)
        {
            ShowViewModel<VehicleDetailViewModel>(new { currentVehicleId = currentVehicleId });
        }

        private void NavigateToRideDetails(int currentRideId)
        {
            ShowViewModel<RideDetailViewModel>(new { currentRideId = currentRideId });
        }
    }
}
