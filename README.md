# Ülkü Ocakları Manisa — Yerel Web Sitesi

ASP.NET Core MVC (.NET 10) ile Ülkü Ocakları Manisa İl Başkanlığı yerel sitesi.

Mimari ve faz planı: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (İsteğe bağlı) Docker ve Docker Compose — SQL Server + web konteyneri

## Hızlı başlangıç (yerel)

```bash
dotnet restore UlkuOcaklariManisa.sln
dotnet build UlkuOcaklariManisa.sln
dotnet test UlkuOcaklariManisa.sln
dotnet run --project src/UOM.Web/UOM.Web.csproj
```

Tarayıcı: `https://localhost:5001` (geliştirme sertifikası gerekir) veya profilde tanımlı HTTP portu.

Connection string ve diğer gizli değerler repoda tutulmaz. Yerel SQL için User Secrets kullanın:

```bash
dotnet user-secrets init --project src/UOM.Web
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=UOM;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False" --project src/UOM.Web
```

## Veritabanı: migrate ve seed (Faz 1)

### Migration

İlk migration (tüm domain tabloları + Identity):

- `20261118170325_AddIdentityAndOrganizationEntities`

Yerel SQL Server çalışıyorsa:

```bash
export DOTNET_ROOT="$HOME/.dotnet"   # gerekirse
dotnet ef database update \
  --project src/UOM.Infrastructure \
  --startup-project src/UOM.Web
```

Veya uygulamayı `Database:AutoMigrate=true` ile başlatın (Docker profili bunu kullanır).

### Seed

Seed, migration sonrası `DatabaseSeeder` ile çalışır (`DatabaseInitializer.MigrateAndSeedAsync`).

| Veri | Açıklama |
|------|----------|
| Roller | `SuperAdmin`, `ProvinceAdmin`, `Editor`, `CampusEditor` |
| 17 ilçe | `IsActive=false`, diğer alanlar boş |
| Statik sayfalar | `hakkimizda`, `kvkk-aydinlatma-metni`, `cerez-politikasi`, `kullanim-kosullari` — gövde `TODO: İl başkanlığından alınacak`, `Draft` |
| Rehber | Her `GuideCategory` için bir boş taslak |
| SiteSettings | Tek boş satır |

**SuperAdmin** yalnızca ortam değişkenleri verildiğinde oluşturulur:

| Değişken | Açıklama |
|----------|----------|
| `SEED_ADMIN_EMAIL` | İlk SuperAdmin e-postası |
| `SEED_ADMIN_PASSWORD` | Geçici şifre (min 12 karakter, Identity kuralları) |

Kullanıcı `MustChangePassword=true` ile oluşturulur (Faz 2 admin girişinde zorunlu şifre değişimi).

Docker örneği (`docker/.env`):

```env
MSSQL_SA_PASSWORD=YourStrong!Passw0rd
SEED_ADMIN_EMAIL=admin@ulkuocaklari.local
SEED_ADMIN_PASSWORD=ChangeMe!SecurePass12
```

Yerel `dotnet run` için:

```bash
export SEED_ADMIN_EMAIL=admin@ulkuocaklari.local
export SEED_ADMIN_PASSWORD='ChangeMe!SecurePass12'
export Database__AutoMigrate=true
dotnet run --project src/UOM.Web/UOM.Web.csproj
```

## Docker Compose

`docker/docker-compose.yml` içindeki `env_file` ve `${...}` değişkenleri **`docker/` klasörüne** göre çözülür. `docker/.env` oluşturup varsayılan geliştirme SuperAdmin tohumunu kullanmak için (örnek parolalar yalnızca yerel geliştirme içindir):

```bash
cd docker
cp .env.example .env
docker compose up --build
```

Depo kökünden çalıştırmak isterseniz proje dizini ve env dosyasını açıkça verin; aksi halde `SEED_ADMIN_*` konteynere gelmez ve `/admin` girişi başarısız olur:

```bash
cp docker/.env.example docker/.env
docker compose --project-directory docker -f docker/docker-compose.yml --env-file docker/.env up --build
```

İlk çalıştırmada volume daha önce **SEED** olmadan migrate edildiyse SuperAdmin oluşmamış olabilir; bir kez `down -v` ile sıfırlayın veya kullanıcıyı elle ekleyin. Admin: `http://localhost:8080/admin` — `docker/.env.example` içindeki geliştirme e-posta/şifre (girişten sonra şifre değişimi zorunlu).

`web` konteyneri SQL Server **sağlıklı** olana kadar bekler (`depends_on: service_healthy`). İlk migrate, SQL henüz hazır değilken veya `UOM` veritabanı zaten varken oluşan kısa süreli **SQL 1801** yarışına karşı uygulama tarafında geri denemeli migrate yapar.

Eski bir volume yarım migrate / tutarsız durumda kaldıysa ve `web` yine de çöküyorsa, bir kez volume’u sıfırlayın (yerel veri silinir):

```bash
cd docker && docker compose down -v && docker compose up --build
# veya repo kökünden:
docker compose --project-directory docker -f docker/docker-compose.yml down -v
docker compose --project-directory docker -f docker/docker-compose.yml --env-file docker/.env up --build
```

**ImageSharp (görsel işleme):** NuGet denetimi açıkken Release derlemesi paket güvenlik uyarılarını hata sayar. Şu an `SixLabors.ImageSharp` **3.1.12** kullanılıyor; güvenlik yamaları **3.2.0+** ile gelir ve doğrudan referans için ücretsiz [Six Labors Community](https://licensing.sixlabors.com) build lisansı gerekir (`UOM.Infrastructure`, `UOM.Application.Tests` assembly adları). İleride `sixlabors.lic` köke eklenip sürüm 3.2.0’a çıkıldığında `Directory.Build.props` içindeki geçici `NuGetAuditSuppress` satırları kaldırılmalıdır.

- Web: http://localhost:8080 — yer tutucu **Merhaba** sayfası
- Sağlık: http://localhost:8080/health — SQL bağlıysa `sqlserver` kontrolü sağlıklı; bağlantı yoksa *Degraded* (yine **200**)
- SQL Server: `localhost:1433` (geliştirme parolası `docker/.env.example` içinde)
- `Database__AutoMigrate=true` ile web konteyneri açılışta migrate + seed yapar

### Docker sorun giderme

`failed to resolve source metadata for docker.io/docker/dockerfile:1: context deadline exceeded` gibi bir hata görürseniz: bu repodaki `docker/Dockerfile` dosyasında `# syntax=docker/dockerfile:1` satırı **kaldırılmıştır** (standart Dockerfile sözdizimi yeterli; Docker Hub’dan ek front-end çekilmez). Image’ler yine **MCR** üzerinden gelir (`mcr.microsoft.com/dotnet/...`). Hata sürerse VPN, kurumsal güvenlik duvarı veya proxy’nin Docker Hub / registry erişimini kontrol edin.

## Çözüm yapısı

```
src/
  UOM.Domain/           # Entity'ler, enum'lar (paket bağımlılığı yok)
  UOM.Application/      # Kurallar, arayüzler (IClock, yayın kuralları)
  UOM.Infrastructure/   # EF Core, Identity, seed, migrations
  UOM.Web/              # ASP.NET Core MVC
tests/
  UOM.Application.Tests/
  UOM.Web.IntegrationTests/
docker/
  docker-compose.yml
  Dockerfile
```

## Public site (Faz 3)

Yayında içerik: `Published` ve `PublishedAtUtc <= now`. Taslaklar public’te **404**.

| URL | Açıklama |
|-----|----------|
| `/` | Ana sayfa (ViewComponent bölümleri) |
| `/haberler`, `/haberler/{slug}` | Haber listesi ve detay |
| `/etkinlikler`, `/etkinlikler/{slug}` | Etkinlikler, 5 CTA tipi, `.ics` |
| `/etkinlikler/takvim.ics` | Yaklaşan etkinlik iCal feed |
| `/iletisim` | İletişim formu (KVKK, honeypot, opsiyonel Turnstile) |
| `/{slug}` | Yayınlanmış statik sayfalar (`ShowInFooter` footer’da) |
| `/cbu` | CBÜ Hub (rehber, duyuru, kaynak özetleri, kampüs haritası) |
| `/cbu/rehber` | Öğrenci rehberi |
| `/cbu/kaynaklar` | Kaynak arşivi (`/cbu/kaynaklar/{id}/indir` sayaç artırır) |
| `/duyurular` | Burs / staj / sınav duyuruları (süresi geçenler listede yok) |

## Yönetim paneli (Faz 2)

- URL: **`/yonetim`** (giriş: `/yonetim/hesap/giris`)
- Zorunlu **TOTP 2FA** (Authenticator), ilk girişte şifre değişimi (`MustChangePassword`)
- İsteğe bağlı test kullanıcıları (migrate/seed sonrası):

| Değişken | Rol |
|----------|-----|
| `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | SuperAdmin |
| `SEED_PROVINCE_ADMIN_EMAIL` / `SEED_PROVINCE_ADMIN_PASSWORD` | ProvinceAdmin |
| `SEED_EDITOR_EMAIL` / `SEED_EDITOR_PASSWORD` | Editor |

Örnek yerel akış:

```bash
export ConnectionStrings__Default="Server=localhost,1433;Database=UOM;..."
export Database__AutoMigrate=true
export SEED_ADMIN_EMAIL=admin@ulkuocaklari.local
export SEED_ADMIN_PASSWORD='ChangeMe!SecurePass12'
export SEED_EDITOR_EMAIL=editor@ulkuocaklari.local
export SEED_EDITOR_PASSWORD='ChangeMe!SecurePass12'
dotnet run --project src/UOM.Web/UOM.Web.csproj
```

1. `https://localhost:5001/yonetim/hesap/giris` — SuperAdmin veya Editor ile giriş  
2. Şifre değiştir → Authenticator ile 2FA kur  
3. `/yonetim/haberler` — Editor: kaydet → **Onaya gönder**; ProvinceAdmin/SuperAdmin: **Yayınla** (Editor’da Yayınla düğmesi yok / policy 403)

Migration (Faz 2): `AddContentApprovalRejectionNotes`

## Faz durumu

- [x] Faz 0 — İskelet, Serilog, `/health`, Docker
- [x] Faz 1 — Domain, EF Core, global soft-delete, audit alanları, seed, SQL health
- [x] Faz 2 — `/yonetim` Identity+2FA, policy’ler, haber/etkinlik/mekan/site/medya CRUD, onay akışı, önizleme, audit log
- [x] Faz 3 — Public çekirdek sayfalar (layout, ana sayfa, haber/etkinlik, iletişim, statik slug, 404/500)
- [x] Faz 4 — CBÜ katmanı: Hub, rehber/kaynak/duyuru public + admin (`CanWriteCampusContent`), kampüs haritası (Leaflet)
- [x] Faz 5 — Genel Merkez + teşkilat: alıntı bandı, `/genel-merkez`, `/teskilat`, Leaflet, lite YouTube, admin CRUD (`CanManageParentLayer`)
- [ ] Faz 6+ — SEO, arama, performans (`ARCHITECTURE.md` Bölüm 13)

## TODO(spec)

Kurumsal kimlik, logo, domain, KVKK metinleri ve içerikler için bkz. [`docs/ISTENECEK-VERILER.md`](docs/ISTENECEK-VERILER.md).