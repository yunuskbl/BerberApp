# Ayarlıyo — Proje Durumu ve Devir Notları

> Bu dosya, çalışmanın kesintisiz devam edebilmesi için tutulan durum kaydıdır.
> Yeni bir oturuma başlarken önce bunu okuyun.
>
> **Son güncelleme:** 4 Ağustos 2026 · **Son commit:** `acfe8c6`

---

## 1. Sistemin şu anki hâli

### Bildirim mimarisi

| Bildirim | Kanal | Durum |
|---|---|---|
| Randevu onayı / hatırlatma / iptal / tamamlandı (müşteriye) | **Meta WhatsApp Cloud API** | ✅ Çalışıyor |
| Yeni randevu talebi (personele) | Meta WhatsApp | ✅ Çalışıyor |
| Kayıt OTP'si (işletme kaydı) | **E-posta** | ✅ Çalışıyor |
| Randevu OTP'si (müşteri telefon doğrulama) | **WhatsApp → Netgsm → Twilio** zinciri | ⚠️ Netgsm bozuk, zincir Twilio'ya düşüyor |
| Abonelik sona erme / aylık limit (işletmeye) | **E-posta** | ✅ Çalışıyor |

**Önemli:** İşletmeye giden bildirimler bilinçli olarak e-postaya taşındı — Meta serbest-metin
mesajı kabul etmiyor, şablon gerektiriyor ve şablon başına ücret alıyor.

### WhatsApp yapılandırması

- **Sağlayıcı:** `WhatsApp__Provider=Hybrid`
  - Kendi numarasını bağlamış tenant → WPPConnect
  - Bağlamamış tenant → merkezi Meta *(pratikte hepsi bu durumda)*
- **Meta Phone Number ID:** `1192563087266067` (+90 530 560 69 16)
- **WABA ID:** `960611216587787`
- **Onaylı şablonlar** (kod bunlarla eşleştirildi):

| Kod anahtarı | Meta'daki şablon adı |
|---|---|
| RandevuOnay | `randevu_onay` |
| YeniRandevuBildirimi | `randevu__onay` *(çift alt çizgi — kasıtlı)* |
| HizmetTamamlandi | `randevu_tamamlandi` |
| RandevuHatirlatma | `randevu_hatirlatma` |
| RandevuIptal | `randevu_iptal` |

- **`otp_dogrulama` şablonu YOK** — Meta hesabı şablon oluşturma izni vermiyor
  (işletme doğrulaması reddedildiği için). Bu yüzden WhatsApp OTP'si Meta'dan değil,
  **WPPConnect'ten serbest metin olarak** gidiyor.

### WPPConnect (yalnızca OTP için)

- SuperAdmin → WhatsApp sayfasından merkezi oturum bağlandı, QR okutuldu ✅
- `WPPCONNECT_SECRET_KEY=THISISMYSECURETOKEN` — **değiştirmeyin**.
  Upstream imaj `SECRET_KEY` env'ini okumuyor, koddaki sabiti kullanıyor.
- Dış port (`21465`) güvenlik gerekçesiyle **kapatıldı**; API iç ağdan erişiyor.
- Tenant'lara özel "kendi numaranı bağla" arayüzü **gizlendi**
  (WPPConnect 2.10 QR üretemiyor — çözülemedi, merkezi Meta ile devam edildi).
- 6 tenant'ın ölü WPPConnect oturum kaydı veritabanından temizlendi.

### SMS

- **Zincir:** `SMS_PROVIDERS=Netgsm,Twilio` → `ChainedSmsService` sırayla dener,
  ilki başarılı olunca durur. Biri yapılandırılmamışsa atlanır.
- **Netgsm:** ❌ Hata 30 — IP kısıtlaması *(bkz. Açık İşler #1)*
  - API şifresi panel giriş şifresinden **farklı**: `.env`'de API şifresi olmalı
- **Twilio:** ✅ Doğrulandı ve çalışıyor
  - Hesap: Full, aktif, ~$20 bakiye
  - Geo Permissions'ta **Türkiye açıldı** (öncesinde hata 21408 veriyordu)
  - Test SMS'i ulaştı — gönderici adı **"qsms"** görünüyor (markasız, ortak başlık)
  - Maliyet Netgsm'e göre yüksek

### E-posta

- Google Workspace'e geçildi: **info@ayarliyo.com**
- Cloudflare'de MX + SPF + DKIM kuruldu
- Site içi ve şablonlardaki tüm adresler `info@ayarliyo.com` yapıldı
- `.env`: `EMAIL_SMTP_USERNAME=info@ayarliyo.com` + App Password

---

## 2. Bu oturumda yapılanlar

### Güvenlik (kapsamlı denetim sonrası)
- **Staff→Admin yetki yükseltmesi kapatıldı** — 13 yönetim controller'ına rol kısıtı.
  Personel hesabı salonu silebiliyor, fiyat değiştirebiliyordu.
- `GET /api/tenants` SuperAdmin'e kısıtlandı (tüm salonların bilgisi sızıyordu)
- WhatsApp webhook'una **HMAC imza doğrulaması** (sahte ONAYLA/REDDET engellendi)
- Cross-tenant IDOR (çalışma saati silme) düzeltildi
- OTP brute-force koruması: 5 deneme limiti + endpoint bazlı rate limit
- Şifre sıfırlama kodu kriptografik RNG'ye çevrildi
- Swagger auth middleware sırası düzeltildi (şema auth'suz erişilebiliyordu)
- Dosya yükleme uzantı whitelist'i (stored XSS)
- `AllowedHosts` kısıtlandı, log sızıntıları kapatıldı (OTP payload, WppConnect secret)
- Meta AccessToken `appsettings.json`'dan temizlendi → yalnızca env'den

### İşletme onay akışı
- `Tenant.IsApproved` eklendi — **yeni kayıtlar onaylanana kadar salonlar listesinde görünmez**
- Mevcut işletmeler etkilenmedi (migration `DEFAULT true`)
- SuperAdmin → **Yeni Kayıtlar** ekranı: şüphe puanlama + Onayla / Askıya Al / Sil
- Yeni kayıtta: platform yöneticisine e-posta + `TENANT_REGISTERED` audit log

### Kayıt e-posta kuralları
- Tek kullanımlık mail servisleri engellendi (28 domain)
- Rastgele karakter tespiti (sesli harf yok / 5+ ardışık sessiz)
- Kurumsal domain'ler serbest
- Tek tip hata: *"Geçerli bir mail adresi giriniz."*

### Randevu & takvim düzeltmeleri
- **Geçmiş saate randevu alınabiliyordu** → müsait saat listesi artık geçmişi eliyor
- **3 saat kayma** → API tüm tarihleri UTC `Z` ekiyle serileştiriyor
- Gece 00:00–03:00 arası tarih kayması (UTC vs Türkiye) birkaç yerde düzeltildi
- Tamamlama sonrası takvim yenilenmiyordu (`refreshViews`)
- **Personel filtresi takvimde çalışmıyordu** — şablon ölü kodu çağırıyordu
- Başlık sayacı görünüme duyarlı hale getirildi (4 vs 5 uyuşmazlığı)
- Üst tarih filtresi ile alt takvim **çift yönlü senkronize** edildi
- **Para birimi karışık toplama** düzeltildi (1200 TRY + 20 USD = 1220 hatası)
  + `Receipt.Currency` artık gerçek para birimini kaydediyor

### Altyapı
- Kök `.dockerignore` eklendi — API imajı `admin-panel/node_modules` kopyalıyordu,
  sunucu diski dolup derleme düşüyordu

### İçerik
- Instagram carousel: **"Yırtılan Defter"** — 8 kare, tek sahne, masa üstü kurgu
  - Artifact: https://claude.ai/code/artifact/80f9b76f-cdf4-438f-93ca-845ae5b23cda
  - 1300×1300 PNG'ler üretildi
- Kling ve Gemini için görsel üretim promptları hazırlandı

---

## 3. Açık işler

### 🔴 Öncelikli

1. **Netgsm IP whitelist düzeltmesi**
   Netgsm gerçek çıkış IP'sini **`172.108.235.172`** olarak bildirdi.
   Whitelist'e yanlışlıkla `178.104.235.172` eklenmiş (rakamlar yer değiştirmiş).
   → Doğrula: `curl -s https://api.ipify.org`
   → Netgsm panelinde düzelt, kendi IP'ni de ekle (daha önce panele kilitlenildi)
   → Not: IP kısıtlamasını tamamen kaldırmak daha sağlam (şifre zaten koruyor)

2. **Meta işletme doğrulaması** — reddedildi, yeniden başvurulabilir
   - Elde: e-Vergi Levhası (Yunus Emre Kobal, şahıs şirketi, Alanya)
   - "İşletmem listede yok" → vergi levhası PDF yükle
   - Doğrulanınca: 250/gün limiti artar **ve şablon oluşturma açılır**

3. **`otp_dogrulama` şablonu** — doğrulama geçince oluştur (Kimlik Doğrulama kategorisi),
   sonra OTP'yi Meta'ya taşıyabiliriz. Şu an WPPConnect'e bağımlıyız.

### 🟡 Yapılacak

4. **DMARC kaydı** — Cloudflare'e ekle:
   `_dmarc` TXT → `v=DMARC1; p=none; rua=mailto:info@ayarliyo.com`
5. **`IYZICO_API_KEY` / `IYZICO_SECRET_KEY`** `.env`'de yok — deploy'da uyarı veriyor,
   ödeme kullanılacaksa eklenmeli
6. **`randevu_tamamlandi` şablonu Pazarlama kategorisinde** — Utility olmalı,
   yoksa pazarlama iznini kapatan müşterilere gitmez

### ❓ Karar bekleyen

7. **"Bekleyenler" çipi takvimde ne yapsın?**
   Şu an sadece listeyi filtreliyor; takvim sorgusu status parametresi göndermiyor.
   Çipe basınca takvim boşalıp spinner gösteriyor ve aynı veriyle dönüyor.
8. **Takvim ay-bazlı sorgu**
   Takvim şu an personelin **tüm randevu geçmişini** çekiyor (API tarih aralığı desteklemiyor).
   Düzeltmek için API'ye `from`/`to` parametreleri + ay navigasyonunda yeniden çekme gerekir —
   **ikisi birlikte** yapılmalı, yoksa ay değiştirince grid boşalır.

### 🟢 Düşük öncelikli (güvenlik denetiminden kalan)

9. Refresh token'lar veritabanında düz metin saklanıyor (hash'lenmeli)
10. CSP `script-src 'unsafe-inline'` içeriyor
11. JWT anahtar uzunluğu başlangıçta doğrulanmıyor

---

## 4. Deploy

```bash
cd ~/BerberApp
git pull origin main
docker compose build --no-cache api admin-panel && docker compose up -d api admin-panel
```

- **Sadece backend değiştiyse:** `api`
- **Sadece frontend değiştiyse:** `admin-panel`
- **Sadece `.env` değiştiyse:** rebuild gerekmez → `docker compose up -d --force-recreate api`
- Tarayıcıda test ederken `Ctrl+Shift+R` (eski JS cache'de kalmasın)

**Disk dolarsa:**
```bash
docker builder prune -af
docker image prune -af
```

---

## 5. Ortam değişkenleri (sunucudaki `.env`)

Değerler **sunucuda**, burada yalnızca hangi anahtarların gerekli olduğu listelenir.

| Anahtar | Not |
|---|---|
| `DB_PASSWORD`, `JWT_KEY`, `SUPERADMIN_PASSWORD` | ✅ dolu |
| `META_ACCESS_TOKEN` | ✅ System User ("messaging") ile üretilmiş **kalıcı** token |
| `META_APP_SECRET` | ✅ dolu — webhook imza doğrulaması için |
| `META_PHONE_NUMBER_ID` | `1192563087266067` |
| `SMS_PROVIDERS` | `Netgsm,Twilio` (varsayılan) |
| `NETGSM_USERCODE` / `NETGSM_PASSWORD` / `NETGSM_MSGHEADER` | ⚠️ `PASSWORD` = **API şifresi**, panel şifresi değil |
| `TWILIO_ACCOUNT_SID` / `TWILIO_AUTH_TOKEN` / `TWILIO_SMS_FROM_NUMBER` | ✅ dolu, doğrulandı |
| `EMAIL_SMTP_USERNAME` / `EMAIL_SMTP_PASSWORD` | ✅ info@ayarliyo.com + App Password |
| `ADMIN_NOTIFICATION_EMAIL` | opsiyonel — boşsa SMTP hesabına düşer |
| `WPPCONNECT_SECRET_KEY` | `THISISMYSECURETOKEN` — **sabit, değiştirmeyin** |
| `IYZICO_API_KEY` / `IYZICO_SECRET_KEY` | ❌ eksik |

---

## 6. Faydalı komutlar

```bash
# API logları
docker compose logs -f api

# WhatsApp / SMS akışını izle
docker compose logs --tail=50 api | grep -E "\[META API\]|\[SMS\]|\[Netgsm\]|\[WPPConnect\]"

# Container'daki env'i doğrula (değerleri kısaltarak)
docker compose exec api printenv | grep -E "Meta__|Sms__|Netgsm__|Twilio__" | sed -E 's/=(.{8}).*/=\1.../'

# Netgsm'i doğrudan test et
UC=$(grep '^NETGSM_USERCODE=' .env | cut -d= -f2); PW=$(grep '^NETGSM_PASSWORD=' .env | cut -d= -f2); HD=$(grep '^NETGSM_MSGHEADER=' .env | cut -d= -f2)
curl -s "https://api.netgsm.com.tr/sms/send/get/?usercode=$UC&password=$PW&gsmno=905383996916&message=test&msgheader=$HD&dil=TR"; echo

# Twilio hesabını kontrol et
SID=$(grep '^TWILIO_ACCOUNT_SID=' .env | cut -d= -f2); TOK=$(grep '^TWILIO_AUTH_TOKEN=' .env | cut -d= -f2)
curl -s -u "$SID:$TOK" "https://api.twilio.com/2010-04-01/Accounts/$SID/Balance.json"; echo
```

---

## 7. Bilinmesi gereken tuzaklar

- **WPPConnect `SECRET_KEY`**: upstream imaj env'i yok sayar, hep `THISISMYSECURETOKEN`.
- **Netgsm şifresi**: panel giriş şifresi ≠ API şifresi. `.env`'de API şifresi olmalı.
- **Meta 200 ≠ teslim edildi**: `200` yalnızca "Meta kabul etti" demektir.
- **Twilio `queued` ≠ teslim edildi**: mesaj SID'iyle `delivered` durumunu ayrıca sorgula.
- **Docker `--force-recreate` kodu yeniden derlemez** — kod değiştiyse `build --no-cache` şart.
- **`.env` düzenlerken `$` içeren değerlere dikkat**: docker-compose onu değişken sanıyor.
- **Git branch**: `main` ve `claude/customer-lookup-autofill-JlS8D` senkron tutuluyor.
