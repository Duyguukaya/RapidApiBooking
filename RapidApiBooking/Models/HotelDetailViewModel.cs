namespace RapidApiBooking.Models
{
    public class HotelDetailViewModel
    {
        public string Description { get; set; }
        public List<string> Photos { get; set; } = new List<string>();

        // Yeni eklediğimiz Oda Listesi özelliği
        public List<RoomItemViewModel> Rooms { get; set; } = new List<RoomItemViewModel>();
    }
}
