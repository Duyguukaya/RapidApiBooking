namespace RapidApiBooking.Models
{
    public class HotelItem
    {
        public int HotelId { get; set; }
        public string Name { get; set; }
        public string PhotoUrl { get; set; }
        public string Price { get; set; }
        public double Score { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }
}
