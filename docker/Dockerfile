# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY UlkuOcaklariManisa.sln ./
COPY src/UOM.Domain/UOM.Domain.csproj src/UOM.Domain/
COPY src/UOM.Application/UOM.Application.csproj src/UOM.Application/
COPY src/UOM.Infrastructure/UOM.Infrastructure.csproj src/UOM.Infrastructure/
COPY src/UOM.Web/UOM.Web.csproj src/UOM.Web/
RUN dotnet restore src/UOM.Web/UOM.Web.csproj
COPY src/ src/
RUN dotnet publish src/UOM.Web/UOM.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN groupadd --system app && useradd --system --gid app app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "UOM.Web.dll"]