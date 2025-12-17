using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RapidApiBooking.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RapidApiBooking.Controllers
{
    public class OtelController : Controller
    {
        private readonly string _apiKey = "a7262bfbe1msh650352f9eedc7a1p1ad5dejsn8fec4454c5aa";
        private readonly string _apiHost = "booking-com15.p.rapidapi.com";

        public IActionResult Index()
        {
            return View();
        }

        // =====================================================
        // LİSTELEME (ROOMS)
        // =====================================================
        public async Task<IActionResult> Rooms(string city, string checkInDate, string checkOutDate, int adultCount)
        {
            var client = new HttpClient();

            string apiCheckIn = DateTime.TryParse(checkInDate, out var d1) ? d1.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");
            string apiCheckOut = DateTime.TryParse(checkOutDate, out var d2) ? d2.ToString("yyyy-MM-dd") : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            string encodedCity = Uri.EscapeDataString(city ?? "");
            string destId = "";

            // 1. Şehir ID Bulma
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

            // 2. Otelleri Bulma
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
                var body = await response.Content.ReadAsStringAsync();
                var hotelData = JsonConvert.DeserializeObject<dynamic>(body);

                if (hotelData.data != null && hotelData.data.hotels != null)
                {
                    foreach (var item in hotelData.data.hotels)
                    {
                        string photo = "https://via.placeholder.com/300";
                        if (item.property.photoUrls != null && item.property.photoUrls.Count > 0)
                        {
                            photo = item.property.photoUrls[0].ToString();
                        }

                        string price = "Fiyat Yok";
                        if (item.property.priceBreakdown?.grossPrice?.value != null)
                        {
                            double raw = (double)item.property.priceBreakdown.grossPrice.value;
                            price = "€ " + raw.ToString("N2");
                        }

                        model.Hotels.Add(new HotelItem
                        {
                            // --- KRİTİK DÜZELTME BURADA ---
                            // item.hotel_id yerine item.property.id kullanılmalı!
                            HotelId = (int)item.property.id,
                            Name = item.property.name,
                            PhotoUrl = photo,
                            Price = price,
                            Score = item.property.reviewScore ?? 0,
                            CheckIn = apiCheckIn,
                            CheckOut = apiCheckOut
                        });
                    }
                }
            }

            return View(model);
        }

        // =====================================================
        // DETAY (DETAIL)
        // =====================================================
        public async Task<IActionResult> Detail(int id)
        {
            var client = new HttpClient();
            var model = new HotelDetailViewModel();

            // -------------------------------------------------
            // 1️⃣ FOTOĞRAFLAR (JToken ile Güvenli Okuma)
            // -------------------------------------------------
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
                var token = JToken.Parse(body);
                JArray photosArray = null;

                // API bazen liste [], bazen nesne { data: [] } döner. İkisini de kontrol ediyoruz.
                if (token is JArray) photosArray = (JArray)token;
                else if (token["data"] is JArray) photosArray = (JArray)token["data"];

                if (photosArray != null)
                {
                    foreach (var p in photosArray)
                    {
                        // Resim url'lerini sırayla dene
                        var url = p["url_max"]?.ToString() ?? p["url_original"]?.ToString() ?? p["url_square60"]?.ToString();

                        if (!string.IsNullOrEmpty(url)) model.Photos.Add(url);
                        if (model.Photos.Count >= 10) break;
                    }
                }
            }

            // -------------------------------------------------
            // 2️⃣ AÇIKLAMA
            // -------------------------------------------------
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
                var json = JObject.Parse(body);

                // Açıklama bazen liste içinde gelir
                string description = null;
                if (json["data"] is JArray arr && arr.Count > 0) description = arr[0]["description"]?.ToString();
                else if (json["data"] is JObject obj) description = obj["description"]?.ToString();

                model.Description = description ?? "Açıklama bulunamadı.";
            }

            // -------------------------------------------------
            // 3️⃣ ODALAR (Hata olursa sayfa patlamasın diye try-catch)
            // -------------------------------------------------
            try
            {
                var roomReq = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{_apiHost}/api/v1/hotels/getRoomListWithAvailability?hotel_id={id}"),
                    Headers = { { "x-rapidapi-key", _apiKey }, { "x-rapidapi-host", _apiHost } }
                };

                var roomRes = await client.SendAsync(roomReq);
                if (roomRes.IsSuccessStatusCode)
                {
                    var body = await roomRes.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);
                    JArray roomsArr = null;

                    if (json["data"] is JArray) roomsArr = (JArray)json["data"];
                    else if (json["data"]?["rooms"] is JArray) roomsArr = (JArray)json["data"]["rooms"];

                    if (roomsArr != null)
                    {
                        foreach (var r in roomsArr)
                        {
                            model.Rooms.Add(new RoomItemViewModel
                            {
                                Name = r["name"]?.ToString(),
                                Price = r["price"]?["value"]?.ToString(),
                                Currency = r["price"]?["currency"]?.ToString(),
                                Facilities = r["facilities"]?.Select(f => f.ToString()).ToList() ?? new List<string>()
                            });
                        }
                    }
                }
            }
            catch
            {
                // Oda bilgisi çekilemezse sayfa hata vermesin, boş kalsın.
            }

            return View(model);
        }
    }
}