namespace RapidApiBooking.Models
{
    public class DashboardViewModel
    {
        public MarketData Market { get; set; } = new MarketData(); // MarketData sınıfını burada çağırdık

        public List<FuelItem> FuelPrices { get; set; } = new List<FuelItem>();
        public string Temperature { get; set; }
        public string WeatherCondition { get; set; }
    }
}
