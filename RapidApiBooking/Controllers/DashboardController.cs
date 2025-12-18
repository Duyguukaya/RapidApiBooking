using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RapidApiBooking.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using System;

namespace RapidApiBooking.Controllers
{
    public class DashboardController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();
            var client = new HttpClient();

            // Tarayıcı taklidi yap (Engellenmeyi önler)
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            // =========================================================
            // 1. DÖVİZ (Frankfurter API - USD, EUR, GBP, JPY)
            // =========================================================
            try
            {
                // USD
                string usdRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=USD&to=TRY");
                double usdPrice = Convert.ToDouble(JObject.Parse(usdRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
                model.Market.UsdTry = new MarketItem { Symbol = "USD", Price = usdPrice.ToString("N2", new CultureInfo("tr-TR")), ChangeRate = "0.5", IsUp = true };

                // EUR
                string eurRes = await client.GetStringAsync("https://api.frankfurter.app/latest?from=EUR&to=TRY");
                double eurPrice = Convert.ToDouble(JObject.Parse(eurRes)["rates"]["TRY"], CultureInfo.InvariantCulture);
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
            catch (Exception ex)
            {
                // Hata varsa sebebini kısa yaz
                model.Market.UsdTry = new MarketItem { Price = "HATA", ChangeRate = "!", IsUp = false };
            }

            // =========================================================
            // 2. KRİPTO PARALAR (CoinPaprika API - YENİ KAYNAK)
            // =========================================================
            try
            {
                // BITCOIN
                string btcUrl = "https://api.coinpaprika.com/v1/tickers/btc-bitcoin";
                var btcRes = await client.GetStringAsync(btcUrl);
                var btcJson = JObject.Parse(btcRes);

                // JSON Yolu: quotes -> USD -> price
                double btcPrice = (double)btcJson["quotes"]["USD"]["price"];
                double btcChange = (double)btcJson["quotes"]["USD"]["percent_change_24h"];

                model.Market.Bitcoin = new MarketItem
                {
                    Symbol = "BTC",
                    // "N0" virgülden sonrasını atar (Örn: 96,450)
                    Price = btcPrice.ToString("N0", new CultureInfo("en-US")),
                    ChangeRate = Math.Abs(btcChange).ToString("N2", new CultureInfo("en-US")),
                    IsUp = btcChange >= 0
                };

                // ETHEREUM
                string ethUrl = "https://api.coinpaprika.com/v1/tickers/eth-ethereum";
                var ethRes = await client.GetStringAsync(ethUrl);
                var ethJson = JObject.Parse(ethRes);

                double ethPrice = (double)ethJson["quotes"]["USD"]["price"];
                double ethChange = (double)ethJson["quotes"]["USD"]["percent_change_24h"];

                model.Market.Ethereum = new MarketItem
                {
                    Symbol = "ETH",
                    Price = ethPrice.ToString("N0", new CultureInfo("en-US")),
                    ChangeRate = Math.Abs(ethChange).ToString("N2", new CultureInfo("en-US")),
                    IsUp = ethChange >= 0
                };
            }
            catch (Exception ex)
            {
                // BURASI ÖNEMLİ: Statik veri YERİNE hatayı ekrana basıyoruz.
                // Böylece neden çalışmadığını göreceksin.
                string hataMesaji = ex.Message.Length > 15 ? ex.Message.Substring(0, 15) + "..." : ex.Message;

                model.Market.Bitcoin = new MarketItem { Price = hataMesaji, ChangeRate = "Err", IsUp = false };
                model.Market.Ethereum = new MarketItem { Price = "Hata", ChangeRate = "Err", IsUp = false };
            }

            // Null Check
            if (model.Market.GbpTry == null) model.Market.GbpTry = new MarketItem();
            if (model.Market.JpyTry == null) model.Market.JpyTry = new MarketItem();
            if (model.Market.UsdTry == null) model.Market.UsdTry = new MarketItem();
            if (model.Market.Bitcoin == null) model.Market.Bitcoin = new MarketItem();

            // Sabitler
            model.FuelPrices.Add(new FuelItem { Name = "Benzin", Price = "42.15 ₺" });
            model.FuelPrices.Add(new FuelItem { Name = "Motorin", Price = "41.80 ₺" });
            model.FuelPrices.Add(new FuelItem { Name = "LPG", Price = "21.90 ₺" });
            model.Temperature = "28";
            model.WeatherCondition = "Güneşli";

            return View(model);
        }
    }
}