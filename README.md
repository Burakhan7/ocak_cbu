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
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;..." --project src/UOM.Web
```

## Docker Compose

```bash
cp docker/.env.example docker/.env   # isteğe bağlı: SA parolasını değiştirin
docker compose -f docker/docker-compose.yml up --build
```

- Web: http://localhost:8080 — yer tutucu **Merhaba** sayfası
- Sağlık: http://localhost:8080/health — SQL hazır değilse `sqlserver` kontrolü *Degraded* olabilir; uç nokta yine **200** döner
- SQL Server: `localhost:1433` (geliştirme parolası `docker/.env.example` içinde)

## Çözüm yapısı

```
src/
  UOM.Domain/           # Domain (Faz 1+)
  UOM.Application/      # Uygulama katmanı
  UOM.Infrastructure/   # Altyapı (EF Core — Faz 1+)
  UOM.Web/              # ASP.NET Core MVC
tests/
  UOM.Application.Tests/
  UOM.Web.IntegrationTests/
docker/
  docker-compose.yml
  Dockerfile
```

## Faz 0 durumu

- [x] İskelet solution, Serilog, `/health`
- [ ] Faz 1+: domain, veritabanı, admin ve public sayfalar (`ARCHITECTURE.md` Bölüm 13)

## TODO(spec)

Kurumsal kimlik, logo, domain, KVKK metinleri ve içerikler için bkz. [`docs/ISTENECEK-VERILER.md`](docs/ISTENECEK-VERILER.md).