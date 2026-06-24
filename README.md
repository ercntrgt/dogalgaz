# TesisatTeklifApp — ÖZDEMİR Mühendislik Tesisat Teklif & Sipariş Sistemi

Doğalgaz tesisatı / kombi / kazan / radyatör işleri için kullanılan kâğıt sipariş–teklif
formunun dijitalleştirilmiş hâli. Teklif oluşturur, otomatik fiyat hesaplar, teklifi siparişe
dönüştürür, stok yeterliliğini kontrol edip yetersiz ürünleri **kırmızı** gösterir, PDF/Excel
çıktısı üretir ve raporlar.

## Teknoloji
- **.NET 10**, ASP.NET Core **Blazor Server** (anlık fiyat hesaplama)
- **Entity Framework Core** + **SQLite** (SQL Server'a geçirilebilir)
- **ASP.NET Core Identity** (rol bazlı yetkilendirme)
- **QuestPDF** (PDF), **ClosedXML** (Excel)
- **Bootstrap** tabanlı kurumsal arayüz

## Katmanlı Mimari
```
src/
  TesisatTeklifApp.Domain          # Entity'ler, enum'lar (bağımlılığı yok)
  TesisatTeklifApp.Application      # Servis arayüzleri, DTO'lar, OfferCalculationService (saf)
  TesisatTeklifApp.Infrastructure  # DbContext, EF, servisler, StockControlService, PDF, Excel, seed
  TesisatTeklifApp.Web             # Blazor Server UI + Identity
tests/
  TesisatTeklifApp.Tests           # xUnit testleri (hesaplama, stok, PDF)
```
Hesaplama `OfferCalculationService`, stok `StockControlService`, PDF `PdfExportService`,
Excel `ExcelExportService` içinde yapılır. Controller/Component içinde iş mantığı yoktur.

## Çalıştırma (macOS / Linux / Windows)
```bash
# .NET 10 SDK kurulu olmalı. PATH'e ekli değilse:
export PATH="$HOME/.dotnet:$PATH"

# Uygulamayı çalıştır
dotnet run --project src/TesisatTeklifApp.Web
```
Tarayıcıda `https://localhost:xxxx` (veya `http://localhost:5219`). Veritabanı ilk açılışta
otomatik oluşturulur ve seed edilir (roller, demo kullanıcılar, örnek ürünler).

### Testler
```bash
dotnet test tests/TesisatTeklifApp.Tests
```

## Demo Kullanıcılar
| Rol | E-posta | Şifre |
|-----|---------|-------|
| Yönetici (Admin) | admin@ozdemir.local | Admin!123 |
| Satış Personeli | satis@ozdemir.local | Satis!123 |
| Görüntüleyici | gozlemci@ozdemir.local | Gozlem!123 |

### Roller
- **Admin:** ürün/stok/kullanıcı yönetimi, tüm teklif ve siparişler, ayarlar.
- **Satış Personeli:** müşteri ekleme, teklif oluşturma, siparişe dönüştürme, PDF; stok salt-okunur.
- **Görüntüleyici:** yalnızca görüntüleme.

## Temel Akış
1. **Ürünler / Stok** (Admin): ürün ve stok bilgileri tanımlanır.
2. **Müşteriler:** müşteri eklenir/aranır.
3. **Yeni Teklif:** A–J bölümleri (müşteri, kombi/kazan, doğalgaz tesisatı, malzeme,
   radyatör, hizmetler, açıklama, fiyat özeti, ödeme planı, iş programı). Ürün seçilince
   birim fiyat otomatik gelir; adet/metre/panel değişince **toplam anlık** hesaplanır.
4. **Siparişe Dönüştür:** stok kontrolü yapılır; yetersiz ürünler **kırmızı** işaretlenir,
   "Eksik Ürünler" listelenir; yeterli ürünler ayara göre stoktan düşülür. Sipariş iptalinde
   stok iade edilir.
5. **PDF / Excel:** kurumsal PDF çıktısı (logo, eksik ürünler bölümü) ve Excel dışa aktarım.
6. **Raporlar:** teklif/sipariş listeleri, eksik/kritik stok, ciro, ürün ve müşteri bazlı.

## Yapılandırma
`src/TesisatTeklifApp.Web/appsettings.json`:
```json
{
  "DatabaseProvider": "Sqlite",          // "SqlServer" yapılabilir
  "ConnectionStrings": { "DefaultConnection": "Data Source=tesisat.db" }
}
```
SQL Server'a geçmek için `DatabaseProvider` = `SqlServer` ve connection string güncellenir;
ardından `dotnet ef database update` çalıştırılır.

## Numara Formatları
- Teklif: `TSF-2026-0001`
- Sipariş: `SPR-2026-0001`

## İleride (Android)
Domain/Application katmanları yeniden kullanılarak bir Web API eklenecek; Android uygulaması
teklif/sipariş oluşturup **müşteri imzasını** yakalayacak (`Offer.CustomerSignature` alanı
bu amaçla hazır bırakıldı).

## Notlar
- Para alanları `decimal`, stok miktarları `decimal` (metre takibi için).
- Silmeler soft-delete (`IsDeleted`).
- QuestPDF Community lisansı küçük firmalar için ücretsizdir.
