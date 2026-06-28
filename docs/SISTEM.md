# TesisatTeklifApp — Sistem Tanımı

## Ne işe yarar?
ÖZDEMİR Mühendislik'in doğalgaz tesisatı, kombi, kazan, radyatör, klima, malzeme ve işçilik
işleri için kullandığı **kağıt sipariş–teklif formunun dijital hali**. Sistem; teklif oluşturur,
fiyatı otomatik hesaplar, teklifi siparişe çevirir, stok yeterliliğini denetler, ödeme/tahsilat
takibi yapar, iş programını takvimde gösterir, satınalmayı yönetir ve kurumsal PDF/Excel çıktısı verir.

## Teknoloji
- **.NET 10**, ASP.NET Core **Blazor Server** (anlık, JavaScript'siz hesaplama)
- **Entity Framework Core + SQLite** (SQL Server'a geçişe hazır)
- **ASP.NET Core Identity** (rol bazlı giriş)
- **QuestPDF** (PDF), **ClosedXML** (Excel), **Bootstrap + Bootstrap Icons** (kurumsal arayüz)

## Mimari (katmanlı)
```
Domain          → Entity'ler ve enum'lar (iş nesneleri)
Application     → Servis arayüzleri, DTO'lar, saf hesaplama servisi
Infrastructure  → Veritabanı, EF, servisler, PDF/Excel, stok/ödeme/satınalma
Web             → Blazor Server ekranları + Identity
Tests           → Birim/entegrasyon testleri
```
Kural: hesaplama/stok/PDF iş mantığı **ekranlarda değil servislerde**. Para ve stok `decimal`.
Silmeler kalıcı değil **soft-delete** (geçmiş bozulmaz).

## Roller ve Yetkiler
| Rol | Yapabildikleri |
|-----|----------------|
| **Admin** | Ürün/stok/kullanıcı/ayar yönetimi, satınalma, tüm teklif ve siparişler, raporlar |
| **Satış Personeli** | Müşteri ekleme, teklif/sipariş, siparişe dönüştürme, PDF, fotoğraf/imza; stok salt-okunur |
| **Görüntüleyici** | Yalnızca görüntüleme |

Demo hesaplar: `admin@ozdemir.local / Admin!123`, `satis@ozdemir.local / Satis!123`,
`gozlemci@ozdemir.local / Gozlem!123`

## Ana Modüller
1. **Yönetim Paneli (Dashboard)** — uyarı kutuları (gecikmiş ödeme, bu ay vadesi, kritik stok,
   bekleyen tedarik), aylık ciro grafiği, son teklifler.
2. **Ürünler & Stok** — ürün kartı (fiyat, KDV, marka/model, birim, stok takibi, kritik/min stok),
   kritik stok renk uyarısı, **Excel ile toplu güncelleme**.
3. **Müşteriler** — kayıt/arama, **Takip ekranı** (siparişler, ödeme dökümü, kalan bakiye).
4. **Teklif / Sipariş** — A–J bölümlü form, anlık fiyat, siparişe dönüştürme, kopyalama, iptal,
   PDF/Excel, **fotoğraf**, **müşteri imzası**.
5. **Ödeme Takvimi** — vadeler, tahsilat işaretleme, gecikmiş/yaklaşan takibi.
6. **İş / Montaj Takvimi** — liste + aylık takvim; işler ve ödeme vadeleri tarihlerine düşer.
7. **Satınalma & Tedarikçiler** — tedarikçi yönetimi, satınalma siparişi, mal girişiyle stok artışı,
   eksik/kritik stoktan **satınalma önerisi**.
8. **Raporlar** — 10 farklı rapor + Excel çıktısı.
9. **Yönetim** — stok ayarları, stok hareketleri, işlem geçmişi, kullanıcılar.

## İş Akışı (tipik)
1. Admin **ürün ve stokları** tanımlar (veya Excel ile toplu yükler).
2. Satış personeli **müşteri** seçer/ekler, **teklif** oluşturur → fiyat otomatik gelir, kaydedince `TSF-...` numarası üretilir.
3. Teklif **siparişe dönüştürülür** (`SPR-...`) → stok kontrol edilir; yetersiz ürünler kırmızı işaretlenir,
   ayara göre stoktan düşülür. İptalde stok iade edilir.
4. **Ödeme planı** (taksit/vade) girilir → **Ödeme Takvimi**'nden tahsilat takip edilir.
5. **PDF** çıktısı alınır (imzalı), gerekirse **fotoğraflar** eklenir.
6. Eksik stoklar için **satınalma siparişi** açılır, mal gelince **teslim al** ile stok güncellenir.
7. Raporlar ve takvimden iş/ödeme durumu izlenir.

## Numara Formatları
- Teklif: `TSF-YIL-SIRA` (TSF-2026-0001)
- Sipariş: `SPR-YIL-SIRA` (SPR-2026-0001)
- Satınalma: `SA-YIL-SIRA` (SA-2026-0001)

## Veri & Saklama
- Tek dosyalı **SQLite** veritabanı (`tesisat.db`). Fotoğraf ve imza da veritabanında saklanır
  (taşınabilir, sunucu klasör izni gerektirmez).
- İleride SQL Server'a geçiş: `appsettings.json` → `DatabaseProvider = "SqlServer"` + bağlantı dizesi.

## Çalıştırma
```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet run --project src/TesisatTeklifApp.Web
# Tarayıcı: http://localhost:5219
```
- **Tablet (USB)**: `adb reverse tcp:5219 tcp:5219` → tablette `http://localhost:5219`.
- **Mobil**: telefondan girince alt gezinme çubuğu; tabletten "Basit" moduyla sade düzen.

## Arayüz
- Kurumsal lacivert/gri tema, ÖZDEMİR logosu, Bootstrap Icons.
- Masaüstü: sol menü. Mobil/tablet: alt gezinme + çekmece menü + basit mod.
- Stok renkleri: yeşil yeterli, sarı kritik, kırmızı yetersiz, gri takipsiz.

## İleride (Android)
Domain/Application katmanları yeniden kullanılarak **Web API** eklenecek; Android uygulaması teklif/sipariş
oluşturup **müşteri imzasını** sahada alacak (altyapı `Offer.CustomerSignature` ile hazır).
