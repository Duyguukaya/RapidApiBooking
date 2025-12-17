namespace RapidApiBooking.Models
{
    public class RoomItemViewModel
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public string Currency { get; set; }
        public List<string> Facilities { get; set; } = new List<string>();
    }
}
