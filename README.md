# 🌍 RapidApiBooking - AI Destekli Seyahat ve Yaşam Asistanı

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp)
![Bootstrap](https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap)
![RapidAPI](https://img.shields.io/badge/RapidAPI-Integrated-0055DA?style=for-the-badge&logo=rapid)
![AI Powered](https://img.shields.io/badge/AI-Claude%203-D97757?style=for-the-badge&logo=anthropic)

**RapidApiBooking**, kullanıcıların otel rezervasyonu yapmalarını sağlayan, aynı zamanda günlük yaşam verilerini (hava durumu, finans, akaryakıt) ve yapay zeka destekli önerileri tek bir paneldem sunan kapsamlı bir **ASP.NET Core MVC** projesidir.

Proje, **6 farklı API** servisini ve **Anthropic Claude AI** teknolojisini bir araya getirerek dinamik ve canlı bir kullanıcı deneyimi sunar.

## 🚀 Özellikler

### 🏨 Otel Rezervasyon Modülü
* **Detaylı Arama:** Şehir, tarih aralığı ve kişi sayısına göre otel arama.
* **Otel Listeleme:** Booking.com altyapısı ile gerçek zamanlı otel fiyatları, puanları ve görselleri.
* **Oda & Detay:** Seçilen otelin detaylı açıklaması, özellikleri ve müsait oda tiplerinin listelenmesi.

### 📊 Akıllı Dashboard (Daily Briefing)
* **🌦️ Canlı Hava Durumu:** Girilen lokasyonun anlık sıcaklık, nem ve rüzgar verileri.
* **💰 Finans Piyasaları:** * **Döviz:** Dolar, Euro, Sterlin ve Yen kurları (Frankfurter API).
    * **Kripto:** Bitcoin ve Ethereum anlık fiyatları ve değişim oranları.
* **⛽ Akaryakıt Fiyatları:** Türkiye geneli güncel Benzin, Motorin ve LPG fiyatları (TL çevrimli).
* **🤖 AI Gezi Rehberi:** Gittiğiniz şehirde gezilmesi gereken en popüler yerler, emojili ve detaylı açıklamalarla (Claude AI).
* **🍽️ AI Şefin Menüsü:** Her gün yenilenen, 4 aşamalı (Çorba, Ana Yemek, Yan Lezzet, Tatlı) tam akşam yemeği menüsü önerisi.

## 🛠️ Kullanılan Teknolojiler ve API'ler

Bu projede aşağıdaki teknolojiler ve servisler kullanılmıştır:

* **Framework:** ASP.NET Core 8.0 MVC
* **Dil:** C#
* **Frontend:** Bootstrap 5, HTML5, CSS3 (Deluxe Master Theme)
* **JSON İşlemleri:** Newtonsoft.Json

### 🔗 Entegre Edilen API Servisleri
| API Servisi | Amaç | Kaynak |
|-------------|------|--------|
| **Booking.com API** | Otel ve oda verilerini çekmek | RapidAPI |
| **Weather API** | Anlık hava durumu bilgisi | RapidAPI |
| **Gas Price API** | Avrupa/Türkiye akaryakıt fiyatları | RapidAPI |
| **Coinranking API** | Kripto para verileri | RapidAPI |
| **Frankfurter API** | Canlı döviz kurları | Open Source |
| **Anthropic Claude API** | Yapay zeka gezi ve yemek önerileri | Anthropic |

## 📸 Ekran Görüntüleri

<table>
  <tr>
    <td><b>Dashboard Paneli</b></td>
    <td><b>Otel Listeleme</b></td>
  </tr>
  <tr>
    <td><img src="https://via.placeholder.com/400x200?text=Dashboard+Screenshot" alt="Dashboard" /></td>
    <td><img src="https://via.placeholder.com/400x200?text=Otel+Listesi+Screenshot" alt="Hotel List" /></td>
  </tr>
</table>






---
*Geliştirici: [Duygu Kaya](https://github.com/KULLANICI_ADINIZ)*
