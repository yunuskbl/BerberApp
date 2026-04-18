# ✂ BerberApp — SaaS Berber Randevu Sistemi

Modern berberler, kuaförler ve güzellik merkezleri için geliştirilmiş çok kiracılı (multi-tenant) randevu yönetim sistemi.

## 🚀 Özellikler

- **Multi-Tenant Mimari** — Her berber kendi subdomain'i ile çalışır
- **Admin Panel** — Personel, hizmet, müşteri ve randevu yönetimi
- **Müşteri Booking** — Müşteriler kolayca online randevu alabilir
- **Salon Discovery** — Müşteriler yakınlarındaki salonları bulabilir
- **WhatsApp Bildirimleri** — Randevu onayı, hatırlatma ve iptal bildirimleri
- **Slot Algoritması** — Çalışma saatlerine göre otomatik müsait slot hesaplama
- **Responsive Tasarım** — Mobil uyumlu müşteri sayfaları
- **Docker Compose** — Tek komutla tüm sistem ayağa kalkar

## 🛠 Teknoloji Stack

### Backend
- **.NET 8** — Clean Architecture
- **PostgreSQL 16** — Veritabanı
- **Entity Framework Core 8** — ORM
- **MediatR** — CQRS pattern
- **FluentValidation** — Validasyon
- **Hangfire** — Arka plan işleri
- **Twilio** — WhatsApp bildirimleri
- **Serilog** — Loglama
- **JWT** — Authentication

### Frontend
- **Angular 19** — Standalone Components
- **TypeScript** — Tip güvenliği
- **SCSS** — Stil
- **Custom Components** — Calendar, Select

### DevOps
- **Docker Compose** — 3 container (DB, API, Angular)
- **Nginx** — Reverse proxy + SPA routing

## 📦 Kurulum

### Gereksinimler
- Docker Desktop
- Git

### Hızlı Başlangıç

```bash
# Repoyu klonla
git clone https://github.com/KULLANICI_ADI/BerberApp.git
cd BerberApp

# Twilio bilgilerini ekle (docker-compose.yml)
# Twilio__AccountSid=YOUR_SID
# Twilio__AuthToken=YOUR_TOKEN

# Çalıştır
docker-compose up --build
```

### Erişim

| Servis | URL |
|--------|-----|
| Admin Panel | http://localhost:4200 |
| Salon Listesi | http://localhost:4200/salons |
| Müşteri Booking | http://localhost:4200/book/{subdomain} |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| Hangfire | http://localhost:8080/hangfire |

### Varsayılan Giriş
Email: admin@berber.com
Şifre: Admin123!

## 🏗 Mimari
BerberApp/
├── BerberApp.Domain/          # Entities, Enums
├── BerberApp.Application/     # CQRS, Handlers, DTOs
├── BerberApp.Infrastructure/  # EF Core, Repositories, Services
├── BerberApp.Api/             # Controllers, Middleware
├── admin-panel/               # Angular 19
└── docker-compose.yml

### Admin Panel
- Dashboard — İstatistik kartları ve bugünkü randevular
- Randevular — Takvim filtreli randevu listesi ve slot seçimi
- Personel — CRUD işlemleri
- Hizmetler — Renk kodlu kart tasarımı
- Müşteriler — Arama destekli liste

### Müşteri Sayfaları
- Salon Discovery — Tüm salonları listele ve ara
- Booking Wizard — Adım adım randevu alma

## 🔔 WhatsApp Bildirimleri

Twilio Sandbox kullanılmaktadır. Production için:
1. Twilio hesabınızda WhatsApp Business hesabı açın
2. `docker-compose.yml`'de credentials'ları güncelleyin

## 📋 API Endpoints

### Auth
- `POST /api/auth/register` — Yeni salon kaydı
- `POST /api/auth/login` — Giriş

### Admin (JWT gerekli)
- `GET/POST /api/staff` — Personel
- `GET/POST /api/services` — Hizmetler
- `GET/POST /api/customers` — Müşteriler
- `GET/POST /api/appointments` — Randevular

### Public
- `GET /api/salons` — Tüm salonlar
- `GET /api/booking/{subdomain}` — Salon bilgisi
- `GET /api/booking/{subdomain}/services` — Hizmetler
- `GET /api/booking/{subdomain}/staff` — Personel
- `GET /api/booking/{subdomain}/available-slots` — Müsait slotlar
- `POST /api/booking/{subdomain}/appointments` — Randevu oluştur

## 📄 Lisans

MIT License

---

**BerberApp** — Modern berberler için modern çözüm ✂