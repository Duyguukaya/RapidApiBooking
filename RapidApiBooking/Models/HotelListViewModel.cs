
namespace RapidApiBooking.Models
{

    public class HotelListViewModel
    {
        public List<HotelItem> Hotels { get; set; }
    }

    public class HotelItem
    {
        public string Name { get; set; }
        public string PhotoUrl { get; set; }
        public string Price { get; set; }
        public double Score { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }
}