using System;
using System.Collections.Generic;
using System.Text;

namespace food_market_narrator.Services
{
    public interface ILocationService
    {
        event EventHandler<Location> LocationChanged;
        event EventHandler<Location?> LocationSampled;
        Location? LastKnownLocation { get; }
        Task<Location?> GetCurrentLocationAsync();
        Task StartTrackingAsync();
        Task<bool> RequestBackgroundLocationPermissionAsync();
        Task<bool> HasBackgroundLocationPermissionAsync();
        void StopTracking();
    }
}
