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
        private readonly string _apiKey = "6b015b54abmsh649b427a04efa89p1e53dcjsna449b8e8da80";
        private readonly string _apiHost = "booking-com15.p.rapidapi.com";

        public IActionResult Index()
        {
            return View();
        }

        // =====================================================
        // LİSTELEME (ROOMS)
        // =====================================================
        // =====================================================
        // LİSTELEME (ROOMS) - AMERİKAN FORMATI DESTEKLİ
        // =====================================================
        public async Task<IActionResult> Rooms(string city, string checkInDate, string checkOutDate, int adultCount)
        {
            var client = new HttpClient();

            // 1️⃣ TARİH SORUNUNU ÇÖZEN BLOK
            // -----------------------------------------------------------------------
            DateTime d1, d2;

            // Hem Türkçe (tr-TR) hem Amerikan (en-US) kültürlerini tanımlıyoruz
            var trCulture = new System.Globalization.CultureInfo("tr-TR");
            var usCulture = new System.Globalization.CultureInfo("en-US");

            // GİRİŞ TARİHİ İÇİN KONTROL:
            // Önce URL'den gelen Amerikan formatını (12/29/2025) dene.
            if (!DateTime.TryParse(checkInDate, usCulture, System.Globalization.DateTimeStyles.None, out d1))
            {
                // Eğer Amerikan değilse, Türkçe formatı (29.12.2025) dene.
                DateTime.TryParse(checkInDate, trCulture, System.Globalization.DateTimeStyles.None, out d1);
            }

            // ÇIKIŞ TARİHİ İÇİN KONTROL:
            if (!DateTime.TryParse(checkOutDate, usCulture, System.Globalization.DateTimeStyles.None, out d2))
            {
                DateTime.TryParse(checkOutDate, trCulture, System.Globalization.DateTimeStyles.None, out d2);
            }

            // Eğer tarihler hala 0001 yılındaysa (okunamadıysa), mecburen 7 gün sonrasını ver (Hata önleyici)
            if (d1.Year < 2020) d1 = DateTime.Now.AddDays(7);
            if (d2.Year < 2020) d2 = DateTime.Now.AddDays(8);

            // API'nin istediği formata (yyyy-MM-dd) çeviriyoruz
            string apiCheckIn = d1.ToString("yyyy-MM-dd");
            string apiCheckOut = d2.ToString("yyyy-MM-dd");
            // -----------------------------------------------------------------------

            string encodedCity = Uri.EscapeDataString(city ?? "Istanbul");
            string destId = "";

            // 2️⃣ ŞEHİR ID BULMA
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

            // 3️⃣ OTELLERİ BULMA
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
                        // Fotoğraf
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
                            // Artık düzeltilmiş ve API formatına (yyyy-MM-dd) dönmüş tarihleri yolluyoruz
                            CheckIn = apiCheckIn,
                            CheckOut = apiCheckOut
                        });
                    }
                }
            }
            return View(model);
        }

        // =====================================================
        // DETAY (DETAIL) - GÜNCELLENMİŞ
        // =====================================================
        // =====================================================
        // DETAY (DETAIL) - HİBRİT ÇÖZÜM (API + UNSPLASH FALLBACK)
        // =====================================================
        public async Task<IActionResult> Detail(int id, string checkInDate, string checkOutDate)
        {
            var client = new HttpClient();
            var model = new HotelDetailViewModel();

            // Tarih kontrolü
            string apiCheckIn = !string.IsNullOrEmpty(checkInDate) ? checkInDate : DateTime.Now.ToString("yyyy-MM-dd");
            string apiCheckOut = !string.IsNullOrEmpty(checkOutDate) ? checkOutDate : DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            // -------------------------------------------------------------
            // 1. FOTOĞRAFLAR (API'den Çek + Bulamazsa Rastgele Ekle)
            // -------------------------------------------------------------
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
                            // Booking API'sinin tüm olası fotoğraf isimlerini dene
                            string url = (string)item.url_max ??
                                         (string)item.url_original ??
                                         (string)item.url_1440 ??
                                         (string)item.url_square60;

                            if (!string.IsNullOrEmpty(url))
                            {
                                model.Photos.Add(url);
                            }
                            if (model.Photos.Count >= 10) break;
                        }
                    }
                }
            }
            catch { /* API hatası olursa geç, aşağıda dolduracağız */ }

            // 🔥 CAN KURTARAN BÖLÜM: Eğer API'den hiç fotoğraf gelmediyse,
            // Sayfa boş kalmasın diye yüksek kaliteli temsili otel fotoları ekle.
            if (model.Photos.Count == 0)
            {
                model.Photos.Add("https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80"); // Lüks Havuz
                model.Photos.Add("https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1200&q=80"); // Lobi
                model.Photos.Add("https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1200&q=80"); // Oda
                model.Photos.Add("https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?auto=format&fit=crop&w=1200&q=80"); // Detay
            }

            // -------------------------------------------------------------
            // 2. AÇIKLAMA (API'den Çek)
            // -------------------------------------------------------------
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

                    // Veri yapısını güvenli çözme
                    try { desc = (string)json.data[0].description; } catch { }
                    if (desc == null) { try { desc = (string)json.data.description; } catch { } }

                    model.Description = desc;
                }
            }
            catch { }

            // Eğer açıklama hala boşsa standart bir yazı ekle
            if (string.IsNullOrEmpty(model.Description))
            {
                model.Description = "Bu otel şehir merkezinde harika bir konuma sahiptir. Misafirlerine konforlu bir konaklama deneyimi sunan tesis, modern olanaklarla donatılmıştır. Resepsiyon 24 saat açıktır.";
            }

            // -------------------------------------------------------------
            // 3. ODALAR (API'den Çek + Fallback)
            // -------------------------------------------------------------
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

                    if (json.data != null)
                    {
                        var rooms = json.data;
                        try { if (json.data.rooms != null) rooms = json.data.rooms; } catch { }

                        foreach (var r in rooms)
                        {
                            // Özellikleri güvenli çek
                            var facilities = new List<string>();
                            try { if (r.facilities != null) foreach (var f in r.facilities) facilities.Add((string)f); } catch { }
                            if (facilities.Count == 0) { facilities.Add("Wifi"); facilities.Add("TV"); facilities.Add("Klima"); }

                            model.Rooms.Add(new RoomItemViewModel
                            {
                                Name = (string)r.name ?? "Standart Oda",
                                Price = (string)r.price?.value?.ToString() ?? "Sorunuz",
                                Currency = (string)r.price?.currency ?? "EUR",
                                Facilities = facilities
                            });
                        }
                    }
                }
            }
            catch { }

            // Eğer hiç oda bulunamazsa (API boş dönerse) örnek bir oda göster
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