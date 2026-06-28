# TesisatTeklifApp — Yapılanlar (Geliştirme Özeti)

ÖZDEMİR Mühendislik için sıfırdan geliştirilen teklif/sipariş/stok sisteminde bugüne kadar
yapılan tüm işler. Kronolojik değil, konu başlıklarına göre gruplanmıştır.

## 1. Altyapı & Mimari
- Mac'e **.NET 10 SDK** kuruldu; proje `dotnet` CLI ile geliştiriliyor (cross-platform, online sunucuda da çalışır).
- **4 katmanlı mimari**: Domain → Application → Infrastructure → Web (+ test projesi).
- İş kuralları servislerde: hesaplama `OfferCalculationService`, stok `StockControlService`,
  PDF `PdfExportService`, Excel `ExcelExportService`, ödeme `PaymentService`, satınalma `PurchaseService`.
- **EF Core + SQLite** (SQL Server'a tek ayarla geçilebilir), tüm para/stok `decimal`, **soft-delete**.
- Otomatik numaralandırma: Teklif `TSF-2026-0001`, Sipariş `SPR-2026-0001`, Satınalma `SA-2026-0001`.

## 2. Güvenlik & Roller
- **ASP.NET Core Identity** (cookie tabanlı giriş).
- 3 rol: **Admin** (tam yetki), **Satış Personeli** (müşteri/teklif/sipariş/PDF), **Görüntüleyici** (salt-okunur).
- Seed demo hesaplar; rol bazlı menü ve sayfa yetkilendirmesi.

## 3. Temel Modüller
- **Ürün & Stok yönetimi**: CRUD, arama, kritik stok renkleri, stok takibi aç/kapa.
- **Müşteri yönetimi**: CRUD, çok kriterli arama; teklif ekranından **anında müşteri ekleme**.
- **Teklif formu (A–J bölümleri)**: müşteri, kombi/kazan, doğalgaz tesisatı, malzeme, radyatör,
  hizmetler, açıklama, fiyat özeti, ödeme planı, iş programı. Ürün seçilince fiyat otomatik;
  adet/metre/panel değişince **toplam anlık** hesaplanır.
- **Siparişe dönüştürme**: stok kontrolü; yetersiz ürünler **kırmızı**, "Eksik Ürünler" tablosu,
  eksik miktar. Stoktan düşme modları (onayda/tamamlanınca/manuel), iptalde **stok iadesi**,
  **stok hareket geçmişi**. Aynı sipariş için **çift düşme engeli**.

## 4. Çıktılar & Raporlar
- **PDF** (QuestPDF): kurumsal tasarım, ÖZDEMİR logosu, eksik ürünler bölümü, **müşteri imzası gömülü**.
- **Excel** (ClosedXML): teklif/sipariş ve rapor dışa aktarımı.
- **10 rapor**: teklif/sipariş listesi, onaylanan/iptal/tamamlanan, eksik stok, kritik stok,
  ciro, ürün bazlı satış, müşteri bazlı.

## 5. Finans & Takip
- **Müşteri Takip ekranı**: bir müşterinin tüm siparişleri, ödeme dökümü, kalan bakiye.
- **Ödeme Takvimi**: tüm vade tarihleri; yaklaşan/gecikmiş/bugün renkli; **"Tahsil Edildi"** işaretleme;
  özet kartlar (gecikmiş, bu ay, bekleyen, tahsil edilen).
- **Dashboard**: uyarı kutuları (gecikmiş ödeme, bu ay vadesi, kritik stok, bekleyen tedarik) +
  aylık ciro grafiği + son teklifler.

## 6. İş Planlama
- **İş / Montaj Takvimi**: **Liste** ve **aylık takvim ızgarası** görünümü.
  İş başlangıç tarihi girilmişse o, yoksa teklif/sipariş tarihine düşer.
  Takvimde **ödeme vadeleri de tutarıyla** görünür (renk: gecikmiş/bugün/yaklaşan/tahsil).

## 7. Satınalma & Tedarik
- **Tedarikçi (cari) yönetimi**.
- **Satınalma siparişi**: oluşturma, list/detay, **mal girişi (teslim al)** → stok otomatik artar + hareket kaydı.
- **Satınalma önerisi**: kritik stok + bekleyen tedarik eksiklerinden otomatik liste.

## 8. Saha & Mobil
- **Fotoğraf yükleme**: teklif/siparişe kamera/galeriden foto; veritabanında saklanır, galeri + silme.
- **Müşteri imzası**: ekranda parmakla/fareyle **imza çizimi**, kaydetme, PDF'e gömme.
- **Mobil arayüz**: telefonda otomatik **alt gezinme çubuğu**; **"Basit" mod** (tabletten tek tıkla sade düzen);
  kayan **çekmece menü** (off-canvas).
- **USB ile tablet erişimi**: `adb reverse` ile tabletten `localhost:5219` test imkanı.

## 9. Toplu İşlemler
- **Excel ile toplu stok/fiyat güncelleme**: şablon indir → düzenle → yükle.
  Id ile eşleştirir; Id boş satır = yeni ürün; stok değişiminde otomatik hareket kaydı.
- **Gerçek ürün kataloğu içe aktarıldı**: "Özdemir Mühendislik Başlıklar1.xlsx" dosyasından
  **165 ürün** (114 Radyatör, 49 Kombi/Şofben/Klima, 2  Isı pompası) eklendi; **eski 12 demo ürün gizlendi**.

## 10. Kurumsal Görünüm
- Lacivert/gri kurumsal tema, ÖZDEMİR logosu.
- Nav menüsündeki emojiler **Bootstrap Icons** (kurumsal çizgi ikonlar) ile değiştirildi (yerel, çevrimdışı çalışır).

## 11. Kalite
- **21+ birim/entegrasyon testi**: fiyat hesaplama (KDV dahil/hariç, iskonto, radyatör),
  stok durum mantığı, stoktan düşme + iade + çift-düşme engeli, PDF üretimi, siparişe dönüştürme,
  kayıt tracking hatası regresyonu. Hepsi geçiyor.
- Düzeltilen önemli hatalar: teklif kaydında **"Product already tracked"** çakışması;
  Teklifler↔Siparişler liste geçişi; mobil basit-mod ve çekmece menü davranışı.

## İleride (planlanan)
- Android uygulaması (ortak Domain/Application + Web API + müşteri imzası).
- Kombi bakım/garanti hatırlatma, teklif paketleri, gider/kasa takibi, WhatsApp ile teklif gönderme.
