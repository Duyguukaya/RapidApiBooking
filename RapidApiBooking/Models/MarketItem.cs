namespace RapidApiBooking.Models
{
    public class MarketItem
    {
        public string Symbol { get; set; }
        public string Price { get; set; }
        public string ChangeRate { get; set; }
        public bool IsUp { get; set; }
    }
}
