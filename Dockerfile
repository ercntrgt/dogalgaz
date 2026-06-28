# ÖZDEMİR TesisatTeklifApp — Web (Blazor Server) bulut imajı (Render vb.)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/TesisatTeklifApp.Web/TesisatTeklifApp.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# QuestPDF (SkiaSharp) PDF üretimi için font kütüphaneleri (Türkçe karakter dahil).
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu fonts-liberation \
 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Render PORT ortam değişkenini verir; Program.cs ona bağlanır.
ENTRYPOINT ["dotnet", "TesisatTeklifApp.Web.dll"]
