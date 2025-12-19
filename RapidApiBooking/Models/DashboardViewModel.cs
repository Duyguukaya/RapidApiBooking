using System.Collections.Generic;

namespace RapidApiBooking.Models
{
    public class DashboardViewModel
    {
        public MarketData Market { get; set; } = new MarketData();
        public List<FuelItem> FuelPrices { get; set; } = new List<FuelItem>();

        public string CityName { get; set; }      
        public string Temperature { get; set; }
        public string WeatherCondition { get; set; }
        public string WeatherIcon { get; set; }  
        public string Humidity { get; set; } 
        public string WindSpeed { get; set; }

        public string TravelRecommendations { get; set; }

        public List<MenuItem> DailyMenu { get; set; } = new List<MenuItem>();
    }
}