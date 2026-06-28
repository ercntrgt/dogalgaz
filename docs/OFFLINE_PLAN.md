# Offline Tablet Uygulaması + Senkronizasyon Planı (MAUI Blazor Hybrid)

## Hedef
Sahadaki tablet **internetsiz** çalışsın (teklif/sipariş/imza/fotoğraf yerelde), internet
gelince **otomatik senkron** olsun. Ofisteki Blazor Server web uygulaması aynen kalır.

## Mimari
```
Mevcut (ofis):  Blazor Server Web  ──►  Sunucu SQLite/SQL Server   (online)
Yeni (saha):    MAUI Blazor Hybrid ──►  Tablet yerel SQLite        (offline)
                          │
                          └── internet gelince ──►  Sunucu Web API (senkron)
```

### Kod yeniden kullanımı
- **Domain** entity'leri: aynen.
- **OfferCalculationService** (saf hesaplama): aynen.
- Razor ekranları: teklif formu, imza, foto vb. büyük ölçüde yeniden kullanılır.
- Yeni: **LocalDbContext** (Identity'siz, sadece domain) + senkron servisi.

> Not: Sunucudaki `AppDbContext` ASP.NET Identity'ye bağlı olduğu için offline'da kullanılamaz;
> tablette Identity'siz ayrı bir yerel context olur.

## Senkron modeli
- Her senkronlanan kayda **SyncId (Guid)** + **LastModified (UTC)** + **SyncState** (New/Modified/Synced).
- **Outbox**: offline yapılan değişikliklerin kuyruğu.
- İnternet gelince:
  1. **Push**: outbox'taki teklif/müşteri kayıtları sunucu API'sine gönderilir.
  2. **Pull**: ürün kataloğu/fiyatlar + güncel kayıtlar sunucudan çekilir.
- Çakışma: SyncId + LastModified ile "son yazan kazanır" (ilk sürüm); ileride alan-bazlı çözüm.

## Sunucu Web API (yeni uçlar)
- `GET /api/sync/catalog?since=` → ürünler/fiyatlar (offline'a indir)
- `GET /api/sync/customers?since=`
- `POST /api/sync/offers` → offline tekliflerini al (SyncId ile)
- Kimlik: cihaz başına **token** (JWT) veya API anahtarı.

## Aşamalar
1. **Ortam**: MAUI Android workload + JDK 17 kurulumu. *(devam ediyor)*
2. **İskelet**: MAUI Blazor Hybrid projesi; Domain + Application referansı; yerel SQLite.
3. **Offline çekirdek**: ürün/müşteri/teklif yerelde oluştur-kaydet; OfferCalculationService ile anlık fiyat; imza/foto yerelde.
4. **Bağlantı algılama**: online/offline durumu (Connectivity API) + "bekleyen X kayıt" göstergesi.
5. **Sunucu API**: senkron uçları + kimlik.
6. **Senkron servisi**: push/pull + outbox + çakışma; internet gelince otomatik tetikleme.
7. **Tablete dağıtım**: `adb` ile APK kurulumu; saha testi.

## Gerçekçi beklenti
Bu çok adımlı bir faz. Mac'te Android derleme zinciri zaman alır; ilk çalışan APK birkaç
iterasyon sürebilir. Ofis web uygulaması bu süreçte etkilenmez.
