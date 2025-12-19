using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RapidApiBooking.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RapidApiBooking.Controllers
{
    public class DashboardController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();
            var client = new HttpClient();

            // Tarayıcı taklidi
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            string rapidApiKey = "a7262bfbe1msh650352f9eedc7a1p1ad5dejsn8fec4454c5aa";

            // Euro kurunu saklamak için değişken (Varsayılan 0)
            double currentEurRate = 0;

            // =========================================================
            // 1. DÖVİZ (Frankfurter API)
            // =========================================================
            try
            {
                // USD
                string usdRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=USD&to=TRY");
                double usdPrice = Convert.ToDouble(JObject.Parse(usdRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.UsdTry = new MarketItem { Symbol = "USD", Price = usdPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.5", IsUp = true };

                // EUR (Burada kuru değişkene alıyoruz!)
                string eurRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=EUR&to=TRY");
                double eurPrice = Convert.ToDouble(JObject.Parse(eurRes)["rates"]["TRY"], CultureInfo.InvariantCulture);

                // *** KURU SAKLA ***
                currentEurRate = eurPrice;

                model.Market.EurTry = new MarketItem { Symbol = "EUR", Price = eurPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.2", IsUp = true };

                // GBP
                string gbpRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=GBP&to=TRY");
                double gbpPrice = Convert.ToDouble(JObject.Parse(gbpRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.GbpTry = new MarketItem { Symbol = "GBP", Price = gbpPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.3", IsUp = true };

                // JPY
                string jpyRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=JPY&to=TRY");
                double jpyPrice = Convert.ToDouble(JObject.Parse(jpyRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.JpyTry = new MarketItem { Symbol = "JPY", Price = jpyPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.1", IsUp = false };
            }
            catch
            {
                // Döviz API hatası durumunda kur 0 kalırsa aşağıda kontrol edeceğiz.
            }

            // =========================================================
            // 2. KRİPTO PARALAR (Coinranking)
            // =========================================================
            try
            {
                var requestCrypto = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://coinranking1.p.rapidapi.com/coins?referenceCurrencyUuid=yhjMzLPhuIDl&timePeriod=24h&orderBy=marketCap&orderDirection=desc&limit=50&offset=0"),
                    Headers =
                    {
                        { "x-rapidapi-key", rapidApiKey },
                        { "x-rapidapi-host", "coinranking1.p.rapidapi.com" },
                    },
                };

                using (var response = await client.SendAsync(requestCrypto))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);
                    var coins = json["data"]["coins"];

                    var btcData = coins.FirstOrDefault(x => (string)x["symbol"] == "BTC");
                    if (btcData != null)
                    {
                        model.Market.Bitcoin = new MarketItem
                        {
                            Symbol = "BTC",
                            Price = ((double)btcData["price"]).ToString("N0", new CultureInfo("en-US")),
                            ChangeRate = Math.Abs((double)btcData["change"]).ToString("N2", new CultureInfo("en-US")),
                            IsUp = (double)btcData["change"] >= 0
                        };
                    }

                    var ethData = coins.FirstOrDefault(x => (string)x["symbol"] == "ETH");
                    if (ethData != null)
                    {
                        model.Market.Ethereum = new MarketItem
                        {
                            Symbol = "ETH",
                            Price = ((double)ethData["price"]).ToString("N0", new CultureInfo("en-US")),
                            ChangeRate = Math.Abs((double)ethData["change"]).ToString("N2", new CultureInfo("en-US")),
                            IsUp = (double)ethData["change"] >= 0
                        };
                    }
                }
            }
            catch
            {
                model.Market.Bitcoin = new MarketItem { Price = "Hata", ChangeRate = "-", IsUp = false };
                model.Market.Ethereum = new MarketItem { Price = "Hata", ChangeRate = "-", IsUp = false };
            }

            // =========================================================
            // 3. AKARYAKIT FİYATLARI (TL ÇEVRİMİ İLE)
            // =========================================================
            model.FuelPrices = new List<FuelItem>();

            try
            {
                var requestFuel = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://gas-price.p.rapidapi.com/europeanCountries"),
                    Headers =
                    {
                        { "x-rapidapi-key", rapidApiKey },
                        { "x-rapidapi-host", "gas-price.p.rapidapi.com" },
                    },
                };

                using (var responseFuel = await client.SendAsync(requestFuel))
                {
                    responseFuel.EnsureSuccessStatusCode();
                    var bodyFuel = await responseFuel.Content.ReadAsStringAsync();
                    var jsonFuel = JObject.Parse(bodyFuel);

                    var fuelList = jsonFuel["result"] ?? jsonFuel["results"];

                    if (fuelList != null)
                    {
                        var turkeyData = fuelList.FirstOrDefault(x => x["country"] != null && x["country"].ToString() == "Turkey");

                        if (turkeyData != null)
                        {
                            // API'den gelen veriler (EUR cinsinden)
                            // Örnek veri: "1.046"
                            double benzinEur = turkeyData["gasoline"] != null ? (double)turkeyData["gasoline"] : 0;
                            double motorinEur = turkeyData["diesel"] != null ? (double)turkeyData["diesel"] : 0;
                            double lpgEur = turkeyData["lpg"] != null ? (double)turkeyData["lpg"] : 0;

                            // === TL ÇEVİRME İŞLEMİ ===
                            // Eğer kur bilgisini yukarıda başarıyla aldıysak çarpıyoruz.
                            // Alamazsak (0 ise) mecburen Euro gösteriyoruz.

                            string benzinStr, motorinStr, lpgStr;

                            if (currentEurRate > 0)
                            {
                                // TL Hesapla
                                benzinStr = (benzinEur * currentEurRate).ToString("N2", new CultureInfo("tr-TR")) + " ₺";
                                motorinStr = (motorinEur * currentEurRate).ToString("N2", new CultureInfo("tr-TR")) + " ₺";
                                lpgStr = (lpgEur * currentEurRate).ToString("N2", new CultureInfo("tr-TR")) + " ₺";
                            }
                            else
                            {
                                // Kur alınamadıysa Euro göster
                                benzinStr = benzinEur.ToString() + " €";
                                motorinStr = motorinEur.ToString() + " €";
                                lpgStr = lpgEur.ToString() + " €";
                            }

                            model.FuelPrices.Add(new FuelItem { Name = "Benzin", Price = benzinStr });
                            model.FuelPrices.Add(new FuelItem { Name = "Motorin", Price = motorinStr });

                            if (lpgEur > 0)
                            {
                                model.FuelPrices.Add(new FuelItem { Name = "LPG", Price = lpgStr });
                            }
                        }
                    }
                }
            }
            catch
            {
                model.FuelPrices.Add(new FuelItem { Name = "Benzin", Price = "---" });
                model.FuelPrices.Add(new FuelItem { Name = "Motorin", Price = "---" });
            }

            // =========================================================
            // 4. NULL KONTROLLERİ VE SABİT VERİLER
            // =========================================================

            if (model.Market.GbpTry == null) model.Market.GbpTry = new MarketItem { Symbol = "GBP", Price = "---", ChangeRate = "0", IsUp = true };
            if (model.Market.JpyTry == null) model.Market.JpyTry = new MarketItem { Symbol = "JPY", Price = "---", ChangeRate = "0", IsUp = true };
            if (model.Market.UsdTry == null) model.Market.UsdTry = new MarketItem { Symbol = "USD", Price = "---", ChangeRate = "0", IsUp = true };
            if (model.Market.EurTry == null) model.Market.EurTry = new MarketItem { Symbol = "EUR", Price = "---", ChangeRate = "0", IsUp = true };

            if (model.Market.Bitcoin == null) model.Market.Bitcoin = new MarketItem { Symbol = "BTC", Price = "---", ChangeRate = "0", IsUp = true };
            if (model.Market.Ethereum == null) model.Market.Ethereum = new MarketItem { Symbol = "ETH", Price = "---", ChangeRate = "0", IsUp = true };

            if (model.FuelPrices.Count == 0)
            {
                model.FuelPrices.Add(new FuelItem { Name = "Veri Yok", Price = "---" });
            }

            model.Temperature = "28";
            model.WeatherCondition = "Güneşli";

            return View(model);
        }
    }
}