using System;
using System.Collections.Generic;
using System.Text;

namespace food_market_narrator.Services
{
    public interface ILocationService
    {
        event EventHandler<Location> LocationChanged;
        Task<Location?> GetCurrentLocationAsync();
        Task StartTrackingAsync();
        void StopTracking();
    }
}
