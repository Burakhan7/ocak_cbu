# Ülkü Ocakları Manisa — Yerel Web Sitesi
## Mimari ve Geliştirme Spesifikasyonu (Cursor için)

> Bu doküman projenin tek doğruluk kaynağıdır (single source of truth). Cursor her fazdan önce bu dosyayı okumalı, belirsiz bir noktada **varsayım yapmak yerine `// TODO(spec):` notu bırakmalı** ve devam etmelidir.

---

## 0. Cursor İçin Çalışma Kuralları

1. Fazları sırayla uygula (Bölüm 13). Bir faz bitmeden diğerine geçme; her fazın sonunda "Kabul Kriterleri" maddelerini kontrol et.
2. Her faz sonunda `dotnet build` ve `dotnet test` hatasız geçmeli.
3. Kurumsal renk/font/logo bilgisi henüz yok. **Renk ve font tahmin etme.** Sadece Bölüm 9'daki CSS değişkenlerini (design token) kullan; değerler sonradan doldurulacak.
4. Tüm kullanıcıya görünen metinler **Türkçe**. Kod, değişken, tablo, sınıf adları **İngilizce**.
5. Türkçe karakter, kültür ve tarih biçimi: `tr-TR`. Saat dilimi: `Europe/Istanbul`. DB'de tarihler **UTC** tutulur, görünümde çevrilir.
6. Kullanıcıdan gelen hiçbir HTML sanitize edilmeden render edilmez.
7. Gizli bilgi (connection string, SMTP şifresi vb.) repoya girmez: `appsettings.Development.json` + User Secrets + ortam değişkenleri.
8. Yeni NuGet paketi eklemeden önce Bölüm 3'teki listeye bak; listede olmayan paket gerekiyorsa gerekçesini commit mesajına yaz.
9. Her entity için migration ayrı ve anlamlı isimli olsun (`AddEventEntity`, `AddQuoteOrdering` gibi).

---

## 1. Proje Özeti

| Başlık | Açıklama |
|---|---|
| **Sahibi / Yöneticisi** | Ülkü Ocakları Manisa İl Başkanlığı |
| **Hedef kitle** | Manisa Celal Bayar Üniversitesi (CBÜ) öğrencileri (öncelikli), Manisa'daki gençler |
| **Amaç** | CBÜ öğrencilerini il başkanlığının faaliyetlerine çekmek; Genel Merkez'in önemli içeriklerini yerelde görünür kılmak |
| **Ana site (parent)** | https://ulkuocaklari.org.tr |
| **Üyelik** | **Yok.** Ziyaretçi hesabı yok. Sadece admin paneli için kullanıcı girişi var. |
| **Domain / Hosting** | Henüz belirlenmedi. Mimari hem IIS (Windows) hem Docker (Linux) üzerinde çalışabilecek şekilde kurulmalı. |

### 1.1 Konumlandırma

Site üç içerik katmanından oluşur:

```
┌─────────────────────────────────────────────────────────┐
│  1. GENEL MERKEZ KATMANI (parent'tan)                    │
│     Alıntılar · seçilmiş Genel Merkez haberleri ·         │
│     projelere yönlendirme · dergi · OCAK TV               │
├─────────────────────────────────────────────────────────┤
│  2. MANİSA İL BAŞKANLIĞI KATMANI                          │
│     İl haberleri · etkinlikler · il yönetimi ·             │
│     ilçe teşkilatları haritası · iletişim                  │
├─────────────────────────────────────────────────────────┤
│  3. CBÜ KATMANI (sitenin farkı — en çok trafik buradan)   │
│     CBÜ Hub · öğrenci rehberi · kaynak arşivi ·            │
│     burs/staj duyuruları · kampüs etkinlikleri             │
└─────────────────────────────────────────────────────────┘
```

**Kural:** Genel Merkez içerikleri **kopyalanmaz**; özet + görsel + "Devamı ana sitede" bağlantısı olarak sunulur. Projeler (UZEM, Tercih Robotu, TeknOcak, BilgiOcakta vb.) yeniden yazılmaz, ana siteye yönlendirilir.

### 1.2 Dönüşüm Hedefleri (sitenin "satması" gereken şey)

Bir CBÜ öğrencisinin siteye girip yapması istenen aksiyonlar, öncelik sırasıyla:

1. **Bir etkinliğe katılmak** (etkinlik detayındaki CTA)
2. **Ocağa uğramak** (adres + harita + çalışma saatleri, her sayfadan en fazla 1 tıkla ulaşılır)
3. **Sosyal medyayı / WhatsApp duyuru kanalını takip etmek**
4. **İletişim formu ile ulaşmak**

Her sayfa tipinde en az bir birincil CTA bulunmalı (Bölüm 7'de sayfa bazında tanımlandı).

---

## 2. Teknoloji Yığını

| Katman | Seçim | Not |
|---|---|---|
| Runtime | **.NET 10 (LTS)** | |
| Web | **ASP.NET Core MVC** (public site) + **Areas/Admin** (Razor Views) | SSR şart (SEO). SPA kullanılmayacak. |
| ORM | **Entity Framework Core 10** | Code-first, migrations |
| Veritabanı | **Microsoft SQL Server 2022** | Lokal geliştirmede Docker container |
| Kimlik doğrulama | **ASP.NET Core Identity** | Sadece admin kullanıcıları |
| CSS | **Bootstrap 5.3** + özel CSS (design token'lar) | LibMan ile, Node gerektirmeden |
| JS | Vanilla JS + gerekirse küçük modüller | jQuery yok |
| Zengin metin editörü | **TinyMCE (self-hosted, community)** | Admin panelinde |
| Harita | **Google Maps JavaScript API** | Anahtar `GOOGLE_MAPS_API_KEY` / `GoogleMaps:ApiKey`. Sayfa başına tek harita. |
| Görsel işleme | **SixLabors.ImageSharp** | WebP dönüştürme, yeniden boyutlandırma |
| HTML sanitize | **Ganss.Xss (HtmlSanitizer)** | |
| E-posta | **MailKit** | SMTP ile bildirim |
| Loglama | **Serilog** (Console + File, ileride Seq) | |
| Arka plan işleri | `BackgroundService` (Hangfire gerekmez) | KVKK silme işleri, sitemap yenileme |
| Bot koruması | **Cloudflare Turnstile** (opsiyonel, config ile açılıp kapanır) + honeypot | |
| Test | **xUnit** + `WebApplicationFactory` + FluentAssertions | |

---

## 3. Solution Yapısı

```
UlkuOcaklariManisa.sln
│
├── src/
│   ├── UOM.Domain/                 # Entity'ler, enum'lar, domain kuralları. Hiçbir pakete bağımlı değil.
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Common/                 # BaseEntity, IAuditable, ISoftDelete, IPublishable
│   │
│   ├── UOM.Application/            # Servis arayüzleri, DTO'lar, iş kuralları, validasyon
│   │   ├── Abstractions/           # IFileStorage, IEmailSender, ISlugService, IClock, ICacheInvalidator
│   │   ├── Features/               # Feature bazlı klasörleme: News/, Events/, Quotes/ ...
│   │   │   └── News/
│   │   │       ├── NewsService.cs
│   │   │       ├── NewsDtos.cs
│   │   │       └── NewsValidators.cs
│   │   └── Common/                 # PagedResult<T>, Result<T>
│   │
│   ├── UOM.Infrastructure/         # EF Core, dosya depolama, e-posta, image processing
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/     # IEntityTypeConfiguration<T> her entity için
│   │   │   ├── Migrations/
│   │   │   └── Seed/
│   │   ├── Storage/                # LocalFileStorage (şimdilik), ileride S3/Azure
│   │   ├── Email/
│   │   ├── Imaging/
│   │   └── BackgroundJobs/
│   │
│   └── UOM.Web/                    # ASP.NET Core MVC
│       ├── Controllers/            # Public controller'lar
│       ├── Areas/Admin/            # Admin paneli (Controllers, Views)
│       ├── Views/
│       ├── ViewComponents/         # QuoteBand, UpcomingEvents, ParentNewsStrip ...
│       ├── TagHelpers/             # <seo-meta>, <responsive-image>
│       ├── wwwroot/
│       │   ├── css/
│       │   │   ├── tokens.css      # Design token'lar (Bölüm 9)
│       │   │   └── site.css
│       │   ├── js/
│       │   ├── lib/                # LibMan çıktıları
│       │   └── uploads/            # .gitignore'da
│       └── Program.cs
│
├── tests/
│   ├── UOM.Application.Tests/
│   └── UOM.Web.IntegrationTests/
│
├── docker/
│   ├── docker-compose.yml          # sqlserver + web
│   └── Dockerfile
│
├── docs/
│   ├── ARCHITECTURE.md             # Bu dosya
│   └── ISTENECEK-VERILER.md
│
├── .editorconfig
├── Directory.Build.props           # Nullable enable, TreatWarningsAsErrors (Release), LangVersion latest
└── README.md
```

**Bağımlılık yönü:** `Web → Application → Domain` ve `Infrastructure → Application → Domain`. `Web`, `Infrastructure`'ı sadece DI kaydı için referanslar.

---

## 4. Domain Modeli

### 4.1 Ortak taban

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }          // Soft delete, global query filter ile
    public byte[] RowVersion { get; set; } = default!; // Concurrency
}

public enum PublishStatus { Draft = 0, PendingApproval = 1, Published = 2, Archived = 3 }

public interface IPublishable
{
    PublishStatus Status { get; set; }
    DateTime? PublishedAtUtc { get; set; }     // İleri tarihli yayın desteklenir
    string Slug { get; set; }
}

public interface ISeoEntity
{
    string? MetaTitle { get; set; }
    string? MetaDescription { get; set; }
    int? OgImageId { get; set; }
}
```

`CreatedAtUtc`, `UpdatedAtUtc`, `CreatedByUserId` ve `UpdatedByUserId` alanları `SaveChangesAsync` override'ında otomatik doldurulur.

### 4.2 Entity listesi

#### İçerik
| Entity | Alanlar (temel) | Not |
|---|---|---|
| **NewsPost** | Title, Slug, Summary, BodyHtml, CoverImageId, Scope (enum), CategoryId, IsFeatured, IsPinned, Status, PublishedAtUtc, ViewCount, SEO alanları | `Scope`: `Province` (İl), `Campus` (CBÜ), `District` (İlçe) |
| **NewsCategory** | Name, Slug, Order | Örn. Faaliyet, Basın Açıklaması, Anma, Spor |
| **ParentNewsLink** | Title, Summary, ImageUrl veya ImageId, ExternalUrl, PublishedDate, Order, IsActive | Genel Merkez haberi. **Sadece link + özet**, gövde metni yok. |
| **Event** | Title, Slug, Summary, BodyHtml, CoverImageId, StartsAtUtc, EndsAtUtc, VenueId, Audience (enum), Type (enum), CallToActionType (enum), CallToActionUrl, Capacity?, Status, SEO alanları | Bkz. 4.3 |
| **Venue** | Name, Address, Latitude, Longitude, CampusId? | Ocak binası, kampüs salonu vb. |
| **Announcement** | Title, Body, Type (enum: Scholarship, Internship, Exam, General), ExternalUrl?, DeadlineUtc?, ExpiresAtUtc, IsPinned, Status | Süresi geçenler otomatik listeden düşer |
| **StaticPage** | Title, Slug, BodyHtml, ShowInFooter, Order, Status, SEO alanları | Hakkımızda, KVKK, Çerez Politikası vb. |

#### CBÜ katmanı
| Entity | Alanlar | Not |
|---|---|---|
| **Campus** | Name, ShortName, Address, Latitude, Longitude, Description, Order | Admin girer. Seed'de örnek kayıt yok, sadece boş tablo. |
| **Faculty** | Name, CampusId, Order | |
| **GuideArticle** | Title, Slug, Summary, BodyHtml, Category (enum), Order, CoverImageId, Status, LastReviewedAtUtc, SEO alanları | `Category`: Barınma, Ulaşım, Akademik, Sosyal, Burs, Kariyer, YeniÖğrenci. `LastReviewedAtUtc` sayfada "Son güncelleme" olarak gösterilir. |
| **Resource** | Title, Description, Type (enum: File, Link), FileId?, ExternalUrl?, FacultyId?, CourseCode?, Semester?, DownloadCount, Status | Ders notu, kaynak, sınav hazırlık linki |

#### Genel Merkez katmanı
| Entity | Alanlar | Not |
|---|---|---|
| **Quote** | Text, Author (enum), SourceNote?, IsActive, Order | `Author`: Ataturk, Basbug, Lider, GenelBaskan. Ana sayfada döner. |
| **ParentProject** | Name, ShortDescription, LogoImageId, ExternalUrl, Order, IsActive | UZEM, Tercih Robotu, TeknOcak, BilgiOcakta, Odak vb. |
| **MagazineIssue** | Title (örn. "Haziran 2026"), CoverImageId, ExternalUrl, IssueDate, IsActive | Ana sitenin `/dergiler` sayfasına link |
| **Video** | Title, YouTubeVideoId, Source (enum: OcakTV, Province), PublishedDate, IsFeatured, Order | Embed `youtube-nocookie.com` üzerinden |

#### Teşkilat
| Entity | Alanlar | Not |
|---|---|---|
| **District** | Name, Slug, PresidentName?, Address?, Phone?, Latitude?, Longitude?, IsActive, Order | Manisa'nın 17 ilçesi seed edilir (Bölüm 4.5) |
| **BoardMember** | FullName, Title, PhotoImageId?, Order, IsActive, ConsentOnFile (bool) | İl yönetimi. `ConsentOnFile = false` ise fotoğraf gösterilmez. |
| **SiteSettings** | Tek satırlık tablo: SiteName, Tagline, Address, Phone, Email, WhatsAppChannelUrl, OpeningHoursText, MapLat, MapLng, SocialLinks (JSON: Instagram, X, YouTube, Facebook, TikTok), GenelMerkezSocialLinks (JSON), FooterText, AnalyticsEnabled, TurnstileEnabled | Admin'den düzenlenir, cache'lenir |

#### Medya ve sistem
| Entity | Alanlar | Not |
|---|---|---|
| **MediaFile** | StoragePath, OriginalFileName, ContentType, SizeBytes, Width?, Height?, AltText, Variants (JSON: thumb/medium/large WebP yolları) | Tüm görseller buradan |
| **GalleryAlbum** | Title, Slug, Description, CoverImageId, EventId?, Date, Status | |
| **GalleryImage** | AlbumId, MediaFileId, Caption, Order | |
| **ContactMessage** | FullName, Email, Phone?, Subject (enum), Message, IsStudentAtCbu (bool), FacultyId?, KvkkConsentAtUtc, IpHash, IsRead, ReadByUserId?, DeleteAfterUtc | Bkz. Bölüm 10 |
| **EventInterest** | EventId, FullName, Phone/Email, FacultyId?, KvkkConsentAtUtc, IpHash, DeleteAfterUtc | **Opsiyonel modül.** Hesap gerektirmez. `Features:EventInterestForm` false ise devre dışı. |
| **AuditLog** | UserId, Action, EntityName, EntityId, ChangesJson, AtUtc, IpHash | Admin panelindeki tüm yazma işlemleri |
| **Redirect** | FromPath, ToPath, StatusCode (301/302), HitCount | Slug değişince otomatik 301 kaydı |

### 4.3 Event enum'ları

```csharp
public enum EventAudience { CbuStudents, AllYouth, HighSchool, Public }
public enum EventType { Seminar, Conference, Sports, Trip, Commemoration, StudyGroup, Social, Workshop, Other }
public enum EventCtaType
{
    None,              // Sadece bilgi
    JustCome,          // "Kayıt gerekmez, gel" + harita
    ExternalForm,      // Google Form vb. dış link
    InternalInterest,  // EventInterest formu (opsiyonel modül)
    WhatsApp           // WhatsApp duyuru kanalı / grup linki
}
```

### 4.4 İlişki ve indeks kuralları

- Her `Slug` alanı tablo bazında **unique index** (soft-delete edilmişler hariç: filtered index `WHERE IsDeleted = 0`).
- `NewsPost(Status, PublishedAtUtc DESC)`, `Event(Status, StartsAtUtc)`, `Announcement(ExpiresAtUtc)` için composite index.
- `MediaFile` silinemez, eğer bir entity referans veriyorsa (`DeleteBehavior.Restrict`).
- Global query filter: `!IsDeleted`. Public tarafta ek olarak: `Status == Published && PublishedAtUtc <= now`.

### 4.5 Seed verisi

- **Roller:** `SuperAdmin`, `ProvinceAdmin`, `Editor`, `CampusEditor`
- **İlk SuperAdmin:** e-posta ve şifre **ortam değişkeninden** okunur (`SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`). İlk girişte şifre değiştirme zorunlu.
- **District:** Ahmetli, Akhisar, Alaşehir, Demirci, Gölmarmara, Gördes, Kırkağaç, Köprübaşı, Kula, Salihli, Sarıgöl, Saruhanlı, Selendi, Soma, Şehzadeler, Turgutlu, Yunusemre (17 ilçe, diğer alanlar boş, `IsActive = false`).
- **StaticPage:** `hakkimizda`, `kvkk-aydinlatma-metni`, `cerez-politikasi`, `kullanim-kosullari` (gövde: `TODO: İl başkanlığından alınacak` yer tutucusu, `Status = Draft`).
- **GuideArticle kategorileri** için her kategoriden 1 adet boş taslak.
- **SiteSettings:** tek boş satır.
- Alıntı, il yönetimi, proje gibi gerçek içerik **seed edilmez**. Bunlar il başkanlığından gelecek (bkz. `ISTENECEK-VERILER.md`).

---

## 5. Yetkilendirme ve İçerik Onay Akışı

### 5.1 Roller

| Yetki | SuperAdmin | ProvinceAdmin | Editor | CampusEditor |
|---|:-:|:-:|:-:|:-:|
| Kullanıcı yönetimi | ✅ | ❌ | ❌ | ❌ |
| Site ayarları | ✅ | ✅ | ❌ | ❌ |
| İçerik **yayınlama** (onay) | ✅ | ✅ | ❌ | ❌ |
| İl haberleri / etkinlik yazma | ✅ | ✅ | ✅ | ❌ |
| CBÜ içerikleri (rehber, kaynak, duyuru, CBÜ haberleri) yazma | ✅ | ✅ | ✅ | ✅ |
| Alıntılar, Genel Merkez içerikleri, il yönetimi | ✅ | ✅ | ❌ | ❌ |
| İletişim mesajlarını görme | ✅ | ✅ | ❌ | ❌ |
| Audit log görme | ✅ | ✅ | ❌ | ❌ |

Policy tabanlı yetkilendirme kullan (`[Authorize(Policy = "CanPublish")]`), rol isimlerini controller'lara gömme.

### 5.2 Onay akışı

```
Editor / CampusEditor           ProvinceAdmin / SuperAdmin
        │                                  │
   [Draft] ──"Onaya Gönder"──▶ [PendingApproval] ──"Yayınla"──▶ [Published]
        ▲                                  │
        └──────── "Reddet + not" ──────────┘
```

- Reddetme notu zorunlu, editöre admin panelinde bildirim olarak görünür.
- `PendingApproval` olan içerik sayısı admin dashboard'unda rozet olarak gösterilir.
- Onaya gönderildiğinde `ProvinceAdmin` rolündekilere e-posta gider (config ile kapatılabilir).
- **Önizleme:** Her içerik için yayınlanmadan önce public şablonda görüntülenebilen imzalı önizleme linki (`/onizleme/{type}/{id}?token=...`, 24 saat geçerli, `noindex`).

### 5.3 Güvenlik

- Admin paneli yolu: `/yonetim` (`/admin` değil).
- Giriş: e-posta + şifre + **zorunlu TOTP 2FA** (Identity'nin authenticator desteği).
- Lockout: 5 hatalı denemede 15 dakika.
- Şifre politikası: min 12 karakter.
- Cookie: `HttpOnly`, `Secure`, `SameSite=Strict`, 8 saat sliding.
- Login ve tüm public form endpoint'lerinde rate limiting (`Microsoft.AspNetCore.RateLimiting`).

---

## 6. URL Haritası (Public)

Tüm URL'ler Türkçe, küçük harf, tire ile.

| URL | Sayfa | Controller.Action |
|---|---|---|
| `/` | Ana sayfa | `Home.Index` |
| `/cbu` | **CBÜ Hub** | `Campus.Index` |
| `/cbu/rehber` | Öğrenci rehberi listesi | `Guide.Index` |
| `/cbu/rehber/{slug}` | Rehber makalesi | `Guide.Detail` |
| `/cbu/kaynaklar` | Kaynak arşivi (filtre: fakülte, ders kodu) | `Resource.Index` |
| `/cbu/kaynaklar/{id}/indir` | Dosya indirme (sayaç artırır) | `Resource.Download` |
| `/duyurular` | Burs, staj, sınav duyuruları | `Announcement.Index` |
| `/haberler` | İl ve CBÜ haberleri (filtre: kapsam, kategori) | `News.Index` |
| `/haberler/{slug}` | Haber detay | `News.Detail` |
| `/etkinlikler` | Yaklaşan + geçmiş etkinlikler | `Event.Index` |
| `/etkinlikler/{slug}` | Etkinlik detay | `Event.Detail` |
| `/etkinlikler/takvim.ics` | Tüm yaklaşan etkinlikler iCal feed | `Event.Calendar` |
| `/etkinlikler/{slug}/takvime-ekle.ics` | Tek etkinlik .ics | `Event.Ics` |
| `/genel-merkez` | Genel Merkez'den haberler, projeler, dergi, OCAK TV | `Parent.Index` |
| `/teskilat` | İl yönetimi + ilçe teşkilatları haritası | `Organization.Index` |
| `/teskilat/{ilce-slug}` | İlçe detay | `Organization.District` |
| `/galeri` | Albümler | `Gallery.Index` |
| `/galeri/{slug}` | Albüm detay (lightbox) | `Gallery.Album` |
| `/videolar` | Videolar | `Video.Index` |
| `/iletisim` | İletişim + harita + form | `Contact.Index` |
| `/ara?q=` | Site içi arama | `Search.Index` |
| `/{slug}` | Statik sayfalar (hakkimizda, kvkk-aydinlatma-metni vb.) | `Page.Detail` (en son route) |
| `/sitemap.xml`, `/robots.txt`, `/feed.xml` (RSS) | | |

**Kurallar:**
- Bulunamayan slug önce `Redirect` tablosuna bakar, yoksa özel 404 sayfası açılır (404 sayfasında arama kutusu + yaklaşan etkinlikler).
- Trailing slash yok; varsa 301 ile kaldırılır.
- Kanonik URL her sayfada `<link rel="canonical">`.

---

## 7. Sayfa Spesifikasyonları

### 7.1 Ana sayfa (`/`)

Bölümler yukarıdan aşağıya. Her bölüm bir **ViewComponent**'tir ve admin'den sırası/görünürlüğü değiştirilebilir (`HomeSection` ayarı `SiteSettings` içinde JSON olarak).

| # | Bölüm | Veri kaynağı | CTA |
|---|---|---|---|
| 1 | **Hero**: kısa slogan + "Yaklaşan etkinlik" kartı (en yakın 1 etkinlik) | Event | "Etkinliğe katıl" |
| 2 | **Alıntı bandı**: aktif alıntılar, 8 sn'de bir döner, `prefers-reduced-motion` ise dönmez | Quote | — |
| 3 | **CBÜ hızlı erişim**: 4 kart: Yeni Öğrenci Rehberi · Kaynak Arşivi · Duyurular · Kampüs Etkinlikleri | Statik link + sayaçlar | Kartın kendisi |
| 4 | **Yaklaşan etkinlikler** (3 adet) | Event | "Tüm etkinlikler" |
| 5 | **İl ve CBÜ haberleri** (öne çıkan 1 + 4 normal) | NewsPost | "Tüm haberler" |
| 6 | **Genel Merkez'den** (yatay şerit, 4 kart) | ParentNewsLink | "Ana sitede oku ↗" |
| 7 | **Projeler** (logo grid) | ParentProject | Dış link |
| 8 | **Son dergi + OCAK TV son video** (yan yana) | MagazineIssue, Video | Dış link / embed |
| 9 | **Ocağa uğra**: adres, çalışma saatleri, mini harita, WhatsApp kanal butonu | SiteSettings | "Yol tarifi al" (Google Maps deep link) |

### 7.2 CBÜ Hub (`/cbu`)

Sitenin en önemli sayfası. CBÜ öğrencisine "bu site benim için" hissini vermeli.

- Üst: "CBÜ'lü Ülküdaşlar" başlığı + kısa metin (admin'den düzenlenebilir).
- **"Yeni misin?"** bloğu: `GuideArticle.Category == YeniÖğrenci` olanlar, sıralı kontrol listesi görünümünde.
- **Bu hafta kampüste:** `Audience == CbuStudents` olan, önümüzdeki 7 gündeki etkinlikler.
- **Son duyurular:** süresi geçmemiş burs, staj ve sınav duyuruları (deadline'a göre sıralı, 3 günden az kalanlar vurgulu).
- **Kaynak arşivi**: fakülte seçici + arama kutusu.
- **Kampüsler**: Campus tablosundan harita (Google Maps), her pinde en yakın ocak noktası ve uzaklığı.
- Alt CTA: WhatsApp duyuru kanalı + Instagram.

### 7.3 Etkinlik detay (`/etkinlikler/{slug}`)

- Kapak görseli, başlık, tarih/saat (Türkçe biçim: "20 Kasım 2026 Cuma, 19.00"), mekan + mini harita.
- **Birincil CTA**, `CallToActionType`'a göre:
  - `JustCome` → "Kayıt gerekmez, bekliyoruz" + yol tarifi butonu
  - `ExternalForm` → "Kayıt ol ↗"
  - `InternalInterest` → sayfa içi kısa form (Ad, telefon veya e-posta, fakülte, KVKK onayı)
  - `WhatsApp` → "WhatsApp'tan katıl"
- İkincil CTA: "Takvimime ekle" (.ics) + "Paylaş" (Web Share API, desteklenmiyorsa WhatsApp/X/kopyala).
- Geçmiş etkinlikse CTA yerine "Bu etkinlik sona erdi" + varsa ilişkili galeri albümü.
- JSON-LD `Event` şeması.

### 7.4 Haber detay

- Başlık, tarih, kapsam rozeti (İl / CBÜ / İlçe), kapak, gövde, galeri (varsa).
- Alt: "İlgili haberler" (aynı kategori, 3 adet) + "Yaklaşan etkinlik" kartı (CTA).
- JSON-LD `NewsArticle`.

### 7.5 Teşkilat (`/teskilat`)

- İl Başkanı ve il yönetimi (fotoğraf yalnızca `ConsentOnFile == true` ise gösterilir).
- Manisa haritası: aktif ilçelerin pinleri, tıklayınca adres, telefon ve yol tarifi.
- İlçe listesi (aktif olanlar üstte).

### 7.6 Genel Merkez (`/genel-merkez`)

- Genel Başkan'ın resmi fotoğrafı ve unvanı (SiteSettings veya ayrı alan).
- ParentNewsLink listesi (sayfalı).
- Projeler, dergiler, OCAK TV videoları.
- Üstte belirgin bir "Ana siteye git ↗" bağlantısı.

### 7.7 İletişim (`/iletisim`)

- Adres, telefon, e-posta, çalışma saatleri, harita.
- Form alanları: Ad Soyad*, E-posta*, Telefon, Konu* (enum: Genel, Etkinlik, Gönüllülük, Basın, Diğer), "CBÜ öğrencisiyim" checkbox'ı (işaretlenince fakülte seçici açılır), Mesaj*, KVKK onayı* (aydınlatma metnine link).
- Honeypot alanı + Turnstile (config açıksa).
- Başarıdan sonra teşekkür mesajı + "Bu arada yaklaşan etkinliklere göz at" bağlantısı.

### 7.8 Global öğeler

- **Header:** logo · Ana Sayfa · CBÜ · Etkinlikler · Haberler · Genel Merkez · Teşkilat · İletişim. Sağda: arama ikonu + vurgulu "Ocağa Uğra" butonu.
- **Mobil:** hamburger menü + **alt sabit bar** (Ana Sayfa · CBÜ · Etkinlikler · Ocağa Uğra). Hedef kitle ağırlıkla mobil olacağı için bu bar zorunlu.
- **Footer:** iletişim bilgileri, il sosyal medyası, Genel Merkez sosyal medyası, hızlı linkler, statik sayfalar (`ShowInFooter`), "Ülkü Ocakları Eğitim ve Kültür Vakfı" ana site linki, telif satırı.
- **Duyuru şeridi (opsiyonel):** `Announcement.IsPinned` olan tek bir duyuru, kapatılabilir (kapanma bilgisi localStorage'da tutulur, try/catch ile).

---

## 8. Admin Paneli (`/yonetim`)

### 8.1 Dashboard
- Onay bekleyen içerik sayısı (rol uygunsa)
- Okunmamış iletişim mesajı sayısı
- Yaklaşan 5 etkinlik
- Süresi 7 gün içinde dolacak duyurular
- Son 7 günün en çok görüntülenen 5 haberi (ViewCount)
- "Güncellenmesi gereken rehber" listesi (`LastReviewedAtUtc` 180 günden eski olanlar)

### 8.2 CRUD ekranları
Her içerik tipi için: liste (arama, durum filtresi, sayfalama), oluştur/düzenle, önizle, onaya gönder/yayınla/arşivle, soft delete + çöp kutusundan geri al.

**Ortak form davranışları:**
- Slug başlıktan otomatik üretilir (Bölüm 11.2), elle değiştirilebilir. Yayınlanmış bir içeriğin slug'ı değişince otomatik `Redirect` kaydı açılır.
- SEO paneli (katlanabilir): meta başlık (60 karakter sayacı), meta açıklama (160 karakter sayacı), OG görseli, Google arama sonucu önizlemesi.
- Kaydedilmemiş değişiklik varken sayfadan çıkışta uyarı.
- Concurrency çakışmasında (`RowVersion`) kullanıcıya "Bu içerik başka biri tarafından güncellendi" mesajı gösterilir, veri ezilmez.

### 8.3 Medya kütüphanesi
- Sürükle-bırak çoklu yükleme.
- İzin verilen türler: jpg, jpeg, png, webp (görsel); pdf, docx, pptx, xlsx, zip (kaynak dosyası). **Doğrulama dosya uzantısından değil, magic byte'lardan yapılır.**
- Limitler: görsel 10 MB, dosya 25 MB (config).
- Görseller yüklenince: EXIF verisi silinir (konum bilgisi sızmasın), 3 WebP varyantı üretilir (thumb 400px, medium 960px, large 1920px), orijinal saklanır.
- Alt metin zorunlu.

### 8.4 Diğer ekranlar
- **Alıntılar:** sürükle-bırak sıralama.
- **İlçeler:** harita üzerinde pin bırakarak konum seçme.
- **İletişim mesajları:** okundu işaretleme, CSV dışa aktarma (sadece ProvinceAdmin+), saklama süresi geri sayımı.
- **Site ayarları:** SiteSettings formu + ana sayfa bölüm sırası/görünürlüğü.
- **Kullanıcılar:** davet e-postasıyla kullanıcı ekleme, rol atama, devre dışı bırakma.
- **Audit log:** filtrelenebilir liste.
- **Yönlendirmeler:** Redirect CRUD.

---

## 9. Tasarım Sistemi

> ⚠️ Kurumsal kimlik kılavuzu henüz alınmadı. Aşağıdaki değerler **yer tutucudur**. Kodun hiçbir yerinde hex kodu sabit yazılmaz; her yerde CSS değişkeni kullanılır.

`wwwroot/css/tokens.css`:

```css
:root {
  /* Marka — TODO(spec): kurumsal kimlik kılavuzundan doldurulacak */
  --color-brand-primary: #000000;   /* TODO */
  --color-brand-secondary: #000000; /* TODO */
  --color-brand-accent: #000000;    /* TODO */

  /* Nötr */
  --color-bg: #ffffff;
  --color-surface: #f6f6f6;
  --color-text: #1a1a1a;
  --color-text-muted: #5c5c5c;
  --color-border: #e2e2e2;

  /* Durum */
  --color-success: #1f7a3f;
  --color-warning: #b26b00;
  --color-danger: #b42318;

  /* Tipografi — TODO(spec): kurumsal font */
  --font-heading: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
  --font-body: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;

  --radius-sm: 4px;
  --radius-md: 8px;
  --radius-lg: 16px;
  --shadow-card: 0 1px 3px rgba(0,0,0,.08);
  --container-max: 1200px;
}
```

**Kurallar:**
- Mobile-first. Kırılım noktaları Bootstrap varsayılanları.
- Gövde metni minimum 16px, kontrast WCAG AA (4.5:1).
- Logo `wwwroot/img/brand/` altına SVG olarak konur. Şimdilik yer tutucu SVG (sadece metin: "ÜLKÜ OCAKLARI MANİSA").
- Kurumsal font alınırsa self-host edilir (`@font-face`, `font-display: swap`). Google Fonts CDN kullanılmaz (KVKK).
- İkonlar: Bootstrap Icons (SVG sprite).

---

## 10. KVKK ve Gizlilik

Üyelik olmasa da iletişim formu ve (opsiyonel) etkinlik ilgi formu kişisel veri topluyor.

| Konu | Uygulama |
|---|---|
| Aydınlatma metni | `/kvkk-aydinlatma-metni` statik sayfası. Her formda link var. Metin il başkanlığından/hukukçudan gelecek. |
| Açık rıza | Form gönderiminde zorunlu checkbox. Onay zamanı `KvkkConsentAtUtc` alanına yazılır. Ön işaretli **olmaz**. |
| Veri minimizasyonu | Sadece gerekli alanlar. TC kimlik no, adres, doğum tarihi **toplanmaz**. |
| IP | Düz IP saklanmaz; `SHA256(ip + günlük salt)` olarak `IpHash` tutulur (sadece spam/abuse tespiti için). |
| Saklama süresi | `ContactMessage`: 180 gün, `EventInterest`: etkinlik bitişinden 30 gün sonra. `DeleteAfterUtc` alanı doldurulur, `BackgroundService` her gece **hard delete** yapar. Süreler config'den okunur. |
| Çerezler | Varsayılan durumda sadece zorunlu çerezler (antiforgery, admin oturumu) kullanılır → çerez banner'ı gerekmez. `AnalyticsEnabled = true` yapılırsa: önce çerezsiz bir analitik (Plausible/Umami self-host) tercih edilir; çerezli bir araç eklenirse onay banner'ı zorunlu hale gelir. |
| Üçüncü taraf | YouTube: `youtube-nocookie.com` + tıklayınca yüklenen "lite embed" (sayfa açılışında iframe yüklenmez). Harita: OSM tile'ları. Google Fonts yok. |
| Fotoğraflar | İl yönetimi fotoğrafları `ConsentOnFile` olmadan yayınlanmaz. Etkinlik galerisi için admin paneline uyarı: "Yüzü net görünen kişilerin rızası alındı mı?" checkbox'ı (albüm yayınlanmadan önce zorunlu). |
| Veri talepleri | Admin panelinde e-posta ile arama + ilgili kayıtları silme aracı (KVKK md. 11 talepleri için). |

---

## 11. Teknik Detaylar

### 11.1 Performans ve cache
- **Output caching** (`AddOutputCache`): public sayfalar 5 dk, ana sayfa 2 dk. Admin'den içerik yayınlanınca ilgili tag'ler invalidate edilir (`ICacheInvalidator`).
- `SiteSettings` ve aktif `Quote`'lar `IMemoryCache`'te tutulur, güncellenince temizlenir.
- Statik dosyalar: `Cache-Control: public, max-age=31536000, immutable` + `asp-append-version`.
- Görseller `<picture>` + `srcset` ile verilir (`<responsive-image media-id="..." sizes="...">` TagHelper'ı).
- Response compression (Brotli + Gzip).
- Hedef: mobil Lighthouse performans ≥ 90, LCP < 2.5 sn.

### 11.2 Slug üretimi
`ISlugService`: Türkçe karakter dönüşümü (ç→c, ğ→g, ı→i, İ→i, ö→o, ş→s, ü→u), küçük harf, alfanümerik dışı karakter → tire, ardışık tireler teke iner, max 80 karakter, çakışmada `-2`, `-3` eki. **Unit test zorunlu.**

### 11.3 Tarih/saat
- `IClock` arayüzü (test edilebilirlik için). `DateTime.Now` doğrudan kullanılmaz.
- Görünümde `Europe/Istanbul`'a çeviren bir extension: `ToLocalDisplay()`. Biçim: `d MMMM yyyy dddd, HH.mm` (`tr-TR`).

### 11.4 Arama
- İlk sürüm: SQL Server **Full-Text Search** (NewsPost, Event, GuideArticle, Announcement, Resource üzerinde). Migration'da FTS catalog ve index'leri raw SQL ile oluşturulur.
- Full-Text kurulu değilse (örn. bazı Docker image'ları) `LIKE` ile fallback (config: `Search:Provider = FullText | Like`).

### 11.5 SEO
- `<seo-meta>` TagHelper'ı: title, description, canonical, OG, Twitter Card.
- JSON-LD: `Organization` (tüm sayfalarda), `NewsArticle`, `Event`, `BreadcrumbList`.
- `sitemap.xml`: dinamik, 1 saat cache.
- `robots.txt`: `/yonetim` ve `/onizleme` disallow.
- RSS: `/feed.xml` (son 20 haber).
- `Development` ve `Staging` ortamlarında tüm sayfalara `X-Robots-Tag: noindex`.

### 11.6 Güvenlik header'ları
Middleware ile:
- `Content-Security-Policy` (nonce tabanlı. İzinli kaynaklar: self, youtube-nocookie.com, maps.googleapis.com, maps.gstatic.com, harita karoları için `*.google.com` ve `*.googleusercontent.com`, challenges.cloudflare.com. `unsafe-eval` yok.)
- `Strict-Transport-Security` (production)
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- Upload klasöründen script çalıştırılamaz (sadece bilinen MIME türleri servis edilir).

### 11.7 Hata yönetimi ve loglama
- Production: özel 404 ve 500 sayfaları. Stack trace gösterilmez.
- Serilog: request logging, kişisel veri (e-posta, telefon) loglanmaz.
- Health check: `/health` (DB bağlantısı dahil), sadece localhost'tan veya bir header ile erişilir.

### 11.8 Konfigürasyon (`appsettings.json` iskeleti)

```json
{
  "ConnectionStrings": { "Default": "" },
  "Site": { "BaseUrl": "", "Environment": "Development" },
  "Storage": { "Provider": "Local", "LocalRoot": "wwwroot/uploads", "MaxImageMb": 10, "MaxFileMb": 25 },
  "Email": { "Enabled": false, "SmtpHost": "", "SmtpPort": 587, "From": "", "NotifyOnContact": [], "NotifyOnPendingApproval": true },
  "Turnstile": { "Enabled": false, "SiteKey": "", "SecretKey": "" },
  "Kvkk": { "ContactRetentionDays": 180, "EventInterestRetentionDaysAfterEvent": 30 },
  "Features": { "EventInterestForm": false, "Search": true },
  "Search": { "Provider": "FullText" },
  "RateLimit": { "FormsPerMinutePerIp": 3, "LoginPerMinutePerIp": 5 }
}
```

### 11.9 Dağıtım
- `docker/docker-compose.yml`: `mcr.microsoft.com/mssql/server:2022-latest` + web. Lokal geliştirmede `docker compose up` ile ayağa kalkmalı.
- `Dockerfile`: multi-stage, `mcr.microsoft.com/dotnet/aspnet:10.0` runtime, non-root kullanıcı.
- IIS'e deploy için `dotnet publish -c Release` çıktısı + `web.config` yeterli olmalı.
- Migration'lar **uygulama açılışında otomatik çalışmaz** (production). Ayrı bir komut: `dotnet UOM.Web.dll --migrate` (bir CLI argümanı ile tetiklenen, migrate edip çıkan mod).
- `uploads` klasörü container'da volume olarak bağlanır.

---

## 12. Test Stratejisi

| Tür | Kapsam |
|---|---|
| Unit | SlugService, yayın kuralları (`IsVisible(now)`), duyuru süresi dolma, KVKK silme tarih hesabı, event CTA çözümleme, Türkçe tarih biçimlendirme |
| Integration | Public sayfalar 200 dönüyor; taslak içerik public'te 404; slug değişince 301; admin yetkisiz erişim → login'e yönlendirme; Editor yayınlayamıyor (403); iletişim formu KVKK onayı olmadan reddediliyor; rate limit çalışıyor |
| Manuel kontrol listesi | Mobilde alt bar, Lighthouse, ekran okuyucu ile ana sayfa, CSP ihlali yok (console) |

Integration testleri için SQL Server Testcontainers kullanılır; mümkün değilse SQLite in-memory kullanılabilir, ancak FTS testleri atlanır (`[Trait("Requires","SqlServer")]`).

---

## 13. Geliştirme Fazları

### Faz 0 — İskelet
- Solution, projeler, `Directory.Build.props`, `.editorconfig`, docker-compose, Serilog, health check.
- **Kabul:** `docker compose up` sonrası boş bir "Merhaba" sayfası ve `/health` 200 dönüyor.

### Faz 1 — Domain + Veritabanı
- Tüm entity'ler, configuration'lar, global filter'lar, audit alanlarının otomatik doldurulması, seed.
- **Kabul:** Migration temiz uygulanıyor, seed çalışıyor, 17 ilçe DB'de, SuperAdmin oluşmuş.

### Faz 2 — Admin: Kimlik + Temel CRUD
- Identity, 2FA, roller, policy'ler, admin layout, dashboard iskeleti, medya kütüphanesi, SiteSettings, NewsPost, Event, Venue CRUD + onay akışı + önizleme + audit log.
- **Kabul:** Editor haber yazıp onaya gönderebiliyor, ProvinceAdmin yayınlayabiliyor, Editor yayınlayamıyor. Görsel yüklenince 3 WebP varyantı oluşuyor ve EXIF siliniyor.

### Faz 3 — Public: Çekirdek sayfalar
- Layout (header, mobil alt bar, footer), ana sayfa (ViewComponent'ler), haberler, etkinlikler (.ics dahil), iletişim (form + KVKK), statik sayfalar, 404/500.
- **Kabul:** Yayınlanmış içerik görünüyor, taslak görünmüyor. Etkinlik CTA'sı 5 tipin hepsinde doğru render ediliyor. Form spam koruması çalışıyor.

### Faz 4 — CBÜ katmanı
- Campus, Faculty, GuideArticle, Resource, Announcement (admin + public), CBÜ Hub sayfası, kampüs haritası.
- **Kabul:** CampusEditor sadece CBÜ içeriklerini düzenleyebiliyor. Süresi geçen duyuru listeden düşüyor. Kaynak indirme sayacı artıyor.

### Faz 5 — Genel Merkez + Teşkilat
- Quote, ParentNewsLink, ParentProject, MagazineIssue, Video, District, BoardMember (admin + public), alıntı bandı, `/genel-merkez`, `/teskilat` + harita, galeri.
- **Kabul:** Alıntılar dönüyor ve reduced-motion'a saygı gösteriyor. YouTube iframe tıklamadan yüklenmiyor. Rızası olmayan yönetim üyesinin fotoğrafı görünmüyor.

### Faz 6 — SEO, Arama, Performans
- SEO TagHelper, JSON-LD, sitemap, RSS, robots, redirect sistemi, FTS arama, output cache + invalidation, responsive image TagHelper, compression.
- **Kabul:** Lighthouse mobil Performance ≥ 90, SEO = 100, Accessibility ≥ 95. Google Rich Results Test'te Event ve NewsArticle geçerli.

### Faz 7 — Güvenlik, KVKK, Yayına Hazırlık
- Güvenlik header'ları + CSP, rate limiting, KVKK silme job'ı, veri talebi aracı, `--migrate` modu, Dockerfile, README (kurulum + deploy adımları).
- **Kabul:** securityheaders.com'da A. Console'da CSP ihlali yok. Saklama süresi dolan kayıt job sonrası DB'den silinmiş. Tüm testler geçiyor.

---

## 14. Kapsam Dışı (v1)

- Ziyaretçi üyeliği / giriş
- Yorum sistemi
- Bağış / ödeme
- Çok dillilik
- Mobil uygulama
- Ana siteden tam metin kopyalama. Ana sayfa slotları ve `/genel-merkez` eksik kaldığında resmi RSS/Atom, yoksa liste sayfasından başlık, tarih, görsel ve kısa özet alınır; gövde metni saklanmaz.
- Push bildirim

## 15. Açık Sorular (`TODO(spec)`)

| # | Soru | Etkilediği yer |
|---|---|---|
| 1 | Kurumsal renk, font ve logo dosyaları | Bölüm 9 |
| 2 | Domain ve hosting (IIS mi, Linux/Docker mı) | 11.9 |
| 3 | SMTP sağlayıcısı | 11.8 |
| 4 | KVKK veri sorumlusu unvanı ve aydınlatma metni | Bölüm 10 |
| 5 | Etkinlik ilgi formu (InternalInterest) v1'de açık olacak mı? | `Features:EventInterestForm` |
| 6 | Ana sitenin RSS/API'si var mı? | 14 |
| 7 | Analitik kullanılacak mı, hangisi? | Bölüm 10 |
