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
        private readonly string _apiKey = "a7262bfbe1msh650352f9eedc7a1p1ad5dejsn8fec4454c5aa"; // Senin Key'in

        public IActionResult Index()
        {
            return View();
        }


        // Form buraya düşecek
        public async Task<IActionResult> Rooms(string city, string checkInDate, string checkOutDate, int adultCount)
        {
            var client = new HttpClient();

            // --- DÜZELTME 1: Tarih Formatlama ---
            // Gelen tarihi güvenli bir şekilde API formatına çeviriyoruz.
            // Eğer parse edemezse bugünün tarihini atayacak güvenli bir yapı kuruyoruz.
            string apiCheckIn = DateTime.TryParse(checkInDate, out DateTime d1) ? d1.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");
            string apiCheckOut = DateTime.TryParse(checkOutDate, out DateTime d2) ? d2.ToString("yyyy-MM-dd") : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            // --- DÜZELTME 2: URL Encoding (Şehir ismindeki boşluklar için) ---
            string encodedCity = Uri.EscapeDataString(city);

            string destId = "";

            // ADIM 1: ŞEHİR BULMA
            var requestDest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={encodedCity}"),
                Headers = {
            { "x-rapidapi-key", _apiKey },
            { "x-rapidapi-host", "booking-com15.p.rapidapi.com" },
        },
            };

            using (var response = await client.SendAsync(requestDest))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(body);

                // Verinin dolu olup olmadığını kontrol et
                if (json["data"] != null && json["data"].HasValues)
                {
                    destId = json["data"][0]["dest_id"]?.ToString();
                }
            }

            if (string.IsNullOrEmpty(destId))
            {
                // Şehir bulunamazsa tekrar anasayfaya atabilirsin
                return RedirectToAction("Index");
            }

            // ADIM 2: OTELLERİ BULMA
            var requestHotels = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels?dest_id={destId}&search_type=CITY&arrival_date={apiCheckIn}&departure_date={apiCheckOut}&adults={adultCount}&room_qty=1&page_number=1&currency_code=TRY"),
                Headers = {
            { "x-rapidapi-key", _apiKey },
            { "x-rapidapi-host", "booking-com15.p.rapidapi.com" },
        },
            };

            HotelListViewModel model = new HotelListViewModel();
            model.Hotels = new List<HotelItem>();

            using (var response = await client.SendAsync(requestHotels))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                // Dinamik dönüşüm
                var data = JsonConvert.DeserializeObject<dynamic>(body);

                // API yapısına göre null check
                if (data.data != null && data.data.hotels != null)
                {
                    foreach (var item in data.data.hotels)
                    {
                        // --- DÜZELTME 3: Veri Güvenliği (Null Check) ---
                        string photo = "https://via.placeholder.com/300"; // Varsayılan resim
                        if (item.property.photoUrls != null && item.property.photoUrls.Count > 0)
                        {
                            photo = item.property.photoUrls[0];
                        }

                        double score = 0;
                        if (item.property.reviewScore != null)
                        {
                            score = (double)item.property.reviewScore;
                        }

                        model.Hotels.Add(new HotelItem
                        {
                            Name = item.property.name,
                            PhotoUrl = photo,
                            Price = item.property.priceBreakdown.grossPrice.value + " " + item.property.priceBreakdown.grossPrice.currency,
                            Score = score,
                            CheckIn = apiCheckIn,
                            CheckOut = apiCheckOut
                        });
                    }
                }
            }

            return View(model);
        }
    }
}