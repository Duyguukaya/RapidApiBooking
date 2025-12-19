using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RapidApiBooking.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RapidApiBooking.Controllers
{
    public class DashboardController : Controller
    {
        // ==================================================================================
        // DİKKAT: Buraya kendi Anthropic API anahtarınızı (sk-ant...) yapıştırmayı unutmayın!
        // ==================================================================================
        private const string AnthropicApiKey = "apikey";

        // RapidAPI Key (Ortak)
        private const string RapidApiKey = "a7262bfbe1msh650352f9eedc7a1p1ad5dejsn8fec4454c5aa";

        public async Task<IActionResult> Index(string city = "Istanbul")
        {
            var model = new DashboardViewModel();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            double currentEurRate = 0;

            // =========================================================
            // 1. DÖVİZ (Frankfurter API)
            // =========================================================
            try
            {
                string usdRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=USD&to=TRY");
                double usdPrice = Convert.ToDouble(JObject.Parse(usdRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.UsdTry = new MarketItem { Symbol = "USD", Price = usdPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.5", IsUp = true };

                string eurRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=EUR&to=TRY");
                double eurPrice = Convert.ToDouble(JObject.Parse(eurRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                currentEurRate = eurPrice;
                model.Market.EurTry = new MarketItem { Symbol = "EUR", Price = eurPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.2", IsUp = true };

                string gbpRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=GBP&to=TRY");
                double gbpPrice = Convert.ToDouble(JObject.Parse(gbpRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.GbpTry = new MarketItem { Symbol = "GBP", Price = gbpPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.3", IsUp = true };

                string jpyRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=JPY&to=TRY");
                double jpyPrice = Convert.ToDouble(JObject.Parse(jpyRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.JpyTry = new MarketItem { Symbol = "JPY", Price = jpyPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.1", IsUp = false };
            }
            catch { }

            // =========================================================
            // 2. KRİPTO PARALAR (Coinranking)
            // =========================================================
            try
            {
                var requestCrypto = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://coinranking1.p.rapidapi.com/coins?referenceCurrencyUuid=yhjMzLPhuIDl&timePeriod=24h&orderBy=marketCap&orderDirection=desc&limit=50&offset=0"),
                    Headers = { { "x-rapidapi-key", RapidApiKey }, { "x-rapidapi-host", "coinranking1.p.rapidapi.com" } },
                };
                using (var response = await client.SendAsync(requestCrypto))
                {
                    response.EnsureSuccessStatusCode();
                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var coins = json["data"]["coins"];

                    var btc = coins.FirstOrDefault(x => (string)x["symbol"] == "BTC");
                    if (btc != null) model.Market.Bitcoin = new MarketItem { Symbol = "BTC", Price = ((double)btc["price"]).ToString("N0", new CultureInfo("en-US")), ChangeRate = Math.Abs((double)btc["change"]).ToString("N2", new CultureInfo("en-US")), IsUp = (double)btc["change"] >= 0 };

                    var eth = coins.FirstOrDefault(x => (string)x["symbol"] == "ETH");
                    if (eth != null) model.Market.Ethereum = new MarketItem { Symbol = "ETH", Price = ((double)eth["price"]).ToString("N0", new CultureInfo("en-US")), ChangeRate = Math.Abs((double)eth["change"]).ToString("N2", new CultureInfo("en-US")), IsUp = (double)eth["change"] >= 0 };
                }
            }
            catch { /* Hata yönetimi */ }

            // =========================================================
            // 3. AKARYAKIT (Gas Price)
            // =========================================================
            model.FuelPrices = new List<FuelItem>();
            try
            {
                var requestFuel = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://gas-price.p.rapidapi.com/europeanCountries"),
                    Headers = { { "x-rapidapi-key", RapidApiKey }, { "x-rapidapi-host", "gas-price.p.rapidapi.com" } },
                };
                using (var responseFuel = await client.SendAsync(requestFuel))
                {
                    responseFuel.EnsureSuccessStatusCode();
                    var jsonFuel = JObject.Parse(await responseFuel.Content.ReadAsStringAsync());
                    var fuelList = jsonFuel["result"] ?? jsonFuel["results"];
                    if (fuelList != null)
                    {
                        var tr = fuelList.FirstOrDefault(x => x["country"] != null && x["country"].ToString() == "Turkey");
                        if (tr != null)
                        {
                            double benzin = tr["gasoline"] != null ? (double)tr["gasoline"] : 0;
                            double motorin = tr["diesel"] != null ? (double)tr["diesel"] : 0;
                            double lpg = tr["lpg"] != null ? (double)tr["lpg"] : 0;

                            if (currentEurRate > 0)
                            {
                                model.FuelPrices.Add(new FuelItem { Name = "Benzin", Price = (benzin * currentEurRate).ToString("N2", new CultureInfo("tr-TR")) + " ₺" });
                                model.FuelPrices.Add(new FuelItem { Name = "Motorin", Price = (motorin * currentEurRate).ToString("N2", new CultureInfo("tr-TR")) + " ₺" });
                                if (lpg > 0) model.FuelPrices.Add(new FuelItem { Name = "LPG", Price = (lpg * currentEurRate).ToString("N2", new CultureInfo("tr-TR")) + " ₺" });
                            }
                            else
                            {
                                model.FuelPrices.Add(new FuelItem { Name = "Benzin", Price = benzin + " €" });
                                model.FuelPrices.Add(new FuelItem { Name = "Motorin", Price = motorin + " €" });
                            }
                        }
                    }
                }
            }
            catch { /* Hata yönetimi */ }

            // =========================================================
            // 4. HAVA DURUMU (WeatherAPI)
            // =========================================================
            try
            {
                var requestWeather = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://weather-api167.p.rapidapi.com/api/weather/current?place={city}&units=standard&lang=en&mode=json"),
                    Headers = { { "x-rapidapi-key", RapidApiKey }, { "x-rapidapi-host", "weather-api167.p.rapidapi.com" }, { "Accept", "application/json" } },
                };
                using (var responseWeather = await client.SendAsync(requestWeather))
                {
                    responseWeather.EnsureSuccessStatusCode();
                    var jsonWeather = JObject.Parse(await responseWeather.Content.ReadAsStringAsync());

                    model.CityName = jsonWeather["name"]?.ToString() ?? city;
                    double tempK = (double)(jsonWeather["main"]?["temprature"] ?? 0);
                    model.Temperature = (tempK - 273.15).ToString("N0");
                    model.WeatherCondition = jsonWeather["weather"]?[0]?["description"]?.ToString();
                    model.WeatherIcon = jsonWeather["weather"]?[0]?["icon"]?.ToString();
                    model.Humidity = jsonWeather["main"]?["humidity"]?.ToString();
                    model.WindSpeed = jsonWeather["wind"]?["speed"]?.ToString();
                }
            }
            catch
            {
                model.CityName = city; model.Temperature = "--"; model.WeatherCondition = "Bulunamadı"; model.WeatherIcon = ""; model.Humidity = "--"; model.WindSpeed = "--";
            }

            // =========================================================
            // 5. CLAUDE AI GEZİ ÖNERİLERİ
            // =========================================================
            model.TravelRecommendations = await GetClaudeRecommendations(city, client);

            // =========================================================
            // 6. CLAUDE AI TAM MENÜ (YENİLENMİŞ)
            // =========================================================
            model.DailyMenu = await GetClaudeFullMenu(client);

            // =========================================================
            // 7. NULL KONTROLLERİ
            // =========================================================
            if (model.Market.UsdTry == null) model.Market.UsdTry = new MarketItem { Symbol = "USD", Price = "---", IsUp = true };
            if (model.Market.EurTry == null) model.Market.EurTry = new MarketItem { Symbol = "EUR", Price = "---", IsUp = true };
            if (model.Market.GbpTry == null) model.Market.GbpTry = new MarketItem { Symbol = "GBP", Price = "---", IsUp = true };
            if (model.Market.JpyTry == null) model.Market.JpyTry = new MarketItem { Symbol = "JPY", Price = "---", IsUp = true };

            if (model.Market.Bitcoin == null) model.Market.Bitcoin = new MarketItem { Symbol = "BTC", Price = "---", IsUp = true };
            if (model.Market.Ethereum == null) model.Market.Ethereum = new MarketItem { Symbol = "ETH", Price = "---", IsUp = true };

            if (model.FuelPrices == null || model.FuelPrices.Count == 0) model.FuelPrices = new List<FuelItem> { new FuelItem { Name = "Veri Yok", Price = "---" } };

            return View(model);
        }

        // --- YARDIMCI METOTLAR ---

        // Gezi Önerileri (Detaylı)
        private async Task<string> GetClaudeRecommendations(string cityName, HttpClient client)
        {
            try
            {
                if (string.IsNullOrEmpty(AnthropicApiKey)) return "";

                string prompt = $"{cityName} şehrini ziyaret edenler için mutlaka görülmesi gereken en popüler 4 yeri listele. " +
                                "Her madde için başa uygun bir emoji koy. " +
                                "Sadece yer ismini yazıp bırakma; her yer için o yerin tarihini veya önemini anlatan 1-2 cümlelik ilgi çekici ve bilgilendirici bir açıklama ekle. " +
                                "Cevabı sadece maddeler halinde ver.";

                var requestBody = new
                {
                    model = "claude-3-haiku-20240307",
                    max_tokens = 600,
                    messages = new[] { new { role = "user", content = prompt } }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                request.Headers.Add("x-api-key", AnthropicApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode) return "";
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(responseBody);
                    return jsonResponse["content"]?[0]?["text"]?.ToString() ?? "";
                }
            }
            catch { return ""; }
        }

        // Günün Tam Menüsü (YENİ - JSON Döner)
        // --- GÜNCELLENMİŞ METOT (Daha Uzun Açıklamalı) ---
        private async Task<List<MenuItem>> GetClaudeFullMenu(HttpClient client)
        {
            var menu = new List<MenuItem>();
            try
            {
                if (string.IsNullOrEmpty(AnthropicApiKey) || AnthropicApiKey.Contains("BURAYA"))
                {
                    return new List<MenuItem> {
                new MenuItem { Course="Uyarı", Name="API Key Eksik", Description="Lütfen Controller'a anahtarınızı girin." }
            };
                }

                // PROMPT GÜNCELLENDİ: Daha detaylı açıklama istiyoruz
                string prompt = "Bana bugün akşam yemeği için Türk mutfağından veya Dünya mutfağından birbirine uyumlu 4 çeşit (Çorba, Ana Yemek, Yan Lezzet, Tatlı) harika bir menü hazırla. " +
                                "Cevabı SADECE aşağıdaki JSON formatında ver, başka hiçbir metin yazma: " +
                                "[ " +
                                "{ \"Course\": \"Çorba\", \"Name\": \"Yemek Adı\", \"Description\": \"Yemeğin malzemelerini ve lezzetini anlatan 2 cümlelik iştah açıcı ve detaylı bir açıklama\" }, " +
                                "{ \"Course\": \"Ana Yemek\", \"Name\": \"Yemek Adı\", \"Description\": \"Pişirme tekniğini ve lezzetini anlatan 2 cümlelik iştah açıcı ve detaylı bir açıklama\" }, " +
                                "{ \"Course\": \"Yan Lezzet\", \"Name\": \"Salata/Meze Adı\", \"Description\": \"İçindeki tazelikleri anlatan 2 cümlelik iştah açıcı ve detaylı bir açıklama\" }, " +
                                "{ \"Course\": \"Tatlı\", \"Name\": \"Tatlı Adı\", \"Description\": \"Dokusu ve tadını anlatan 2 cümlelik iştah açıcı ve detaylı bir açıklama\" } " +
                                "]";

                var requestBody = new
                {
                    model = "claude-3-haiku-20240307",
                    max_tokens = 800, // Açıklamalar uzayacağı için limiti artırdık
                    messages = new[] { new { role = "user", content = prompt } }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                request.Headers.Add("x-api-key", AnthropicApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode) return new List<MenuItem>();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(responseBody);
                    string content = jsonResponse["content"]?[0]?["text"]?.ToString() ?? "";

                    int startIndex = content.IndexOf("[");
                    int endIndex = content.LastIndexOf("]");
                    if (startIndex >= 0 && endIndex > startIndex)
                    {
                        content = content.Substring(startIndex, endIndex - startIndex + 1);
                        menu = JsonConvert.DeserializeObject<List<MenuItem>>(content);
                    }
                }
            }
            catch
            {
                // Hata yönetimi
            }
            return menu;
        }
    }
}