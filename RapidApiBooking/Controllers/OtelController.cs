using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RapidApiBooking.Models;

namespace RapidApiBooking.Controllers
{
    public class OtelController : Controller
    {
        private readonly string _apiKey = "6b015b54abmsh649b427a04efa89p1e53dcjsna449b8e8da80";
        private readonly string _apiHost = "booking-com15.p.rapidapi.com";

        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Rooms(string city, string checkInDate, string checkOutDate, int adultCount)
        {
            var client = new HttpClient();
            DateTime d1, d2;

            var trCulture = new System.Globalization.CultureInfo("tr-TR");
            var usCulture = new System.Globalization.CultureInfo("en-US");

            if (!DateTime.TryParse(checkInDate, usCulture, System.Globalization.DateTimeStyles.None, out d1))
            {
                DateTime.TryParse(checkInDate, trCulture, System.Globalization.DateTimeStyles.None, out d1);
            }

            if (!DateTime.TryParse(checkOutDate, usCulture, System.Globalization.DateTimeStyles.None, out d2))
            {
                DateTime.TryParse(checkOutDate, trCulture, System.Globalization.DateTimeStyles.None, out d2);
            }

            if (d1.Year < 2020) d1 = DateTime.Now.AddDays(7);
            if (d2.Year < 2020) d2 = DateTime.Now.AddDays(8);

            string apiCheckIn = d1.ToString("yyyy-MM-dd");
            string apiCheckOut = d2.ToString("yyyy-MM-dd");

            string encodedCity = Uri.EscapeDataString(city ?? "Istanbul");
            string destId = "";

            var destReq = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{_apiHost}/api/v1/hotels/searchDestination?query={encodedCity}"),
                Headers = { { "x-rapidapi-key", _apiKey }, { "x-rapidapi-host", _apiHost } }
            };

            using (var response = await client.SendAsync(destReq))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(body);
                destId = json["data"]?[0]?["dest_id"]?.ToString();
            }

            if (string.IsNullOrEmpty(destId)) return RedirectToAction("Index");

            var hotelReq = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://{_apiHost}/api/v1/hotels/searchHotels?dest_id={destId}&search_type=CITY&arrival_date={apiCheckIn}&departure_date={apiCheckOut}&adults={adultCount}&room_qty=1&page_number=1&currency_code=EUR"),
                Headers = { { "x-rapidapi-key", _apiKey }, { "x-rapidapi-host", _apiHost } }
            };

            var model = new HotelListViewModel();
            using (var response = await client.SendAsync(hotelReq))
            {
                response.EnsureSuccessStatusCode();
                var hotelData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

                if (hotelData.data != null && hotelData.data.hotels != null)
                {
                    foreach (var item in hotelData.data.hotels)
                    {
                       
                        string photo = "https://via.placeholder.com/300";
                        try
                        {
                            if (item.property.photoUrls != null && item.property.photoUrls.Count > 0)
                                photo = item.property.photoUrls[0].ToString();
                        }
                        catch { }

                        // Fiyat
                        string price = "Müsaitlik Sorunuz";
                        try
                        {
                            if (item.property.priceBreakdown?.grossPrice?.value != null)
                            {
                                double rawPrice = (double)item.property.priceBreakdown.grossPrice.value;
                                price = "€ " + rawPrice.ToString("N2");
                            }
                        }
                        catch { }

                        model.Hotels.Add(new HotelItem
                        {
                            HotelId = (int)item.property.id,
                            Name = item.property.name,
                            PhotoUrl = photo,
                            Price = price,
                            Score = (double?)item.property.reviewScore ?? 0,
                            CheckIn = apiCheckIn,
                            CheckOut = apiCheckOut
                        });
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Detail(int id, string checkInDate, string checkOutDate)
        {
            var client = new HttpClient();
            var model = new HotelDetailViewModel();

            string apiCheckIn = !string.IsNullOrEmpty(checkInDate) ? checkInDate : DateTime.Now.ToString("yyyy-MM-dd");
            string apiCheckOut = !string.IsNullOrEmpty(checkOutDate) ? checkOutDate : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            try
            {
                var photoReq = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{_apiHost}/api/v1/hotels/getHotelPhotos?hotel_id={id}"),
                    Headers = { { "x-rapidapi-key", _apiKey }, { "x-rapidapi-host", _apiHost } }
                };

                var photoRes = await client.SendAsync(photoReq);
                if (photoRes.IsSuccessStatusCode)
                {
                    var body = await photoRes.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(body);

                    if (json.data != null)
                    {
                        foreach (var item in json.data)
                        {
                            string url = (string)item.url;
                            if (string.IsNullOrEmpty(url))
                            {
                                url = (string)item.url_max ?? (string)item.url_original;
                            }

                            if (!string.IsNullOrEmpty(url))
                            {
                                model.Photos.Add(url);
                            }

                            if (model.Photos.Count >= 10) break;
                        }
                    }
                }
            }
            catch { }
            if (model.Photos.Count == 0)
            {
                model.Photos.Add("https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80"); 
                model.Photos.Add("https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1200&q=80"); 
                model.Photos.Add("https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1200&q=80"); 
                model.Photos.Add("https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?auto=format&fit=crop&w=1200&q=80"); 
            }

            try
            {
                var descReq = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{_apiHost}/api/v1/hotels/getDescriptionAndInfo?hotel_id={id}&languagecode=tr"),
                    Headers = { { "x-rapidapi-key", _apiKey }, { "x-rapidapi-host", _apiHost } }
                };

                var descRes = await client.SendAsync(descReq);
                if (descRes.IsSuccessStatusCode)
                {
                    var body = await descRes.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(body);
                    string desc = null;

                    try { desc = (string)json.data[0].description; } catch { }
                    if (desc == null) { try { desc = (string)json.data.description; } catch { } }

                    model.Description = desc;
                }
            }
            catch { }

          
            if (string.IsNullOrEmpty(model.Description))
            {
                model.Description = "Bu otel şehir merkezinde harika bir konuma sahiptir. Misafirlerine konforlu bir konaklama deneyimi sunan tesis, modern olanaklarla donatılmıştır. Resepsiyon 24 saat açıktır.";
            }

            try
            {
                var roomReq = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{_apiHost}/api/v1/hotels/getRoomListWithAvailability?hotel_id={id}&arrival_date={apiCheckIn}&departure_date={apiCheckOut}"),
                    Headers = { { "x-rapidapi-key", _apiKey }, { "x-rapidapi-host", _apiHost } }
                };

                var roomRes = await client.SendAsync(roomReq);
                if (roomRes.IsSuccessStatusCode)
                {
                    var body = await roomRes.Content.ReadAsStringAsync();

                    dynamic json = JsonConvert.DeserializeObject(body);

                    var allRawRooms = new List<dynamic>();

                    if (json.data != null && json.data.available != null)
                    {
                        foreach (var r in json.data.available)
                        {
                            allRawRooms.Add(r);
                        }
                    }
                    else if (json.available != null)
                    {
                        foreach (var r in json.available)
                        {
                            allRawRooms.Add(r);
                        }
                    }

                    if (json.data != null && json.data.unavailable != null)
                    {
                        foreach (var r in json.data.unavailable)
                        {
                            allRawRooms.Add(r);
                        }
                    }
                    else if (json.unavailable != null)
                    {
                        foreach (var r in json.unavailable)
                        {
                            allRawRooms.Add(r);
                        }
                    }

                    foreach (var r in allRawRooms)
                    {
                        var roomModel = new RoomItemViewModel();

                        roomModel.Name = (string)r.room_name ?? (string)r.name_without_policy ?? "Oda Tipi Belirtilmemiş";

                        string rawPrice = "Tükendi";
                        string rawCurrency = "EUR";

                        try
                        {
                            if (r.composite_price_breakdown != null && r.composite_price_breakdown.gross_amount != null)
                            {
                                rawPrice = r.composite_price_breakdown.gross_amount.value.ToString();
                                rawCurrency = r.composite_price_breakdown.gross_amount.currency ?? "EUR";
                            }
                            else if (r.price_breakdown != null && r.price_breakdown.gross_price != null)
                            {
                                rawPrice = r.price_breakdown.gross_price.value.ToString();
                                rawCurrency = r.price_breakdown.gross_price.currency ?? "EUR";
                            }
                            else if (r.product_price_breakdown != null && r.product_price_breakdown.gross_amount != null)
                            {
                                rawPrice = r.product_price_breakdown.gross_amount.value.ToString();
                                rawCurrency = r.product_price_breakdown.gross_amount.currency ?? "EUR";
                            }
                        }
                        catch { }

                        if (double.TryParse(rawPrice, out double parsedPrice))
                        {
                            roomModel.Price = parsedPrice.ToString("N2");
                        }
                        else
                        {
                            roomModel.Price = rawPrice;
                        }

                        roomModel.Currency = rawCurrency;

                        if (r.room_surface_in_m2 != null)
                        {
                            roomModel.Facilities.Add($"{r.room_surface_in_m2} m²");
                        }

                        try
                        {
                            if (r.bed_configurations != null)
                            {
                                foreach (var config in r.bed_configurations)
                                {
                                    if (config.bed_types != null)
                                    {
                                        foreach (var bed in config.bed_types)
                                        {
                                            string bedInfo = (string)bed.name_with_count;
                                            if (!string.IsNullOrEmpty(bedInfo))
                                                roomModel.Facilities.Add(bedInfo);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            if (r.facilities != null)
                            {
                                foreach (var f in r.facilities)
                                {
                                    roomModel.Facilities.Add(f.ToString());
                                }
                            }
                        }
                        catch { }

                        if (roomModel.Facilities.Count == 0)
                        {
                            roomModel.Facilities.Add("Wifi");
                            roomModel.Facilities.Add("Klima");
                            roomModel.Facilities.Add("TV");
                        }

                        model.Rooms.Add(roomModel);
                    }
                }
            }
            catch (Exception ex)
            {
               
            }

            if (model.Rooms.Count == 0)
            {
                model.Rooms.Add(new RoomItemViewModel
                {
                    Name = "Standart Çift Kişilik Oda (Temsili)",
                    Price = "120",
                    Currency = "EUR",
                    Facilities = new List<string> { "Ücretsiz Wifi", "Klima", "LCD TV", "Minibar" }
                });
            }

            return View(model);
        }
    }
}