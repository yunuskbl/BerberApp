# ayarlıyo — iPhone 17 Mockup Prompt (Claude Opus için)

Aşağıdaki promptu Claude'a **görsel yükleyerek** kullan.
Görseli yükle → promptu yapıştır → Opus HTML üretsin.

---

## PROMPT (Kopyala-Yapıştır)

```
Yüklediğim görseli kullanarak bir iPhone 17 mockup sahnesi oluştur.

KURALLAR:
1. Çıktı tek bir HTML dosyası olsun — harici CSS/JS yok, her şey inline.
2. iPhone 17 çerçevesi: koyu metal rengi, yuvarlak köşeler (border-radius: 50px),
   Dynamic Island (üstte oval siyah kesim), ince yan butonlar.
3. Yüklediğim görsel ekranın içine tam oturmalı (object-fit: cover).
4. Ekranın üstünde gerçekçi bir iOS status bar olsun (9:41, sinyal, batarya).
5. WhatsApp bildirimi ekranın üst kısmında overlay olarak çıksın:
   - Gönderen: "ayarlıyo ✂" — İş Hesabı
   - Avatar: turuncu-altın gradient, içinde "A" harfi
   - Mesaj içeriği: "✂ Randevu Onayı — Merhaba! Randevunuz oluşturuldu. 📅 [tarih] · [hizmet adı]"
   - iOS blur card stili (backdrop-filter: blur)
   - Slide-down animasyon
6. Arka plan koyu (siyah ya da koyu gri), telefon ortada gölgeli.
7. Telefon boyutu: yaklaşık 340x736px.

WHATSAPP BİLDİRİM DETAYLARI:
- Uygulama adı: "WHATSAPP" (küçük font, gri)
- Zaman: "şimdi"
- Gönderen adı: "ayarlıyo" (kalın, beyaz)
- Alt başlık: "İş Hesabı" (küçük, soluk gri)
- Mesaj: Kısa, okunabilir randevu özeti

Sadece HTML kodunu ver, açıklama yazma.
```

---

## GELİŞMİŞ PROMPT (WhatsApp Konuşma Ekranı için)

```
Yüklediğim görseli referans alarak gerçekçi bir WhatsApp konuşma ekranı
içeren iPhone 17 mockup'ı HTML olarak oluştur.

TELEFON ÇERÇEVESİ:
- iPhone 17: koyu metal, 340x736px, border-radius 50px
- Dynamic Island (üst ortada, 110x34px oval siyah pill)
- Sol kenar: barber pole şerit deseni (kırmızı/beyaz/mavi, 6px genişlik, animasyonlu)
- Arka plan: #0f0f0f

WHATSAPP EKRANI (tam ekran içerik):
- Arka plan: #0B141A (WhatsApp koyu)
- Header bar (#1F2C34):
  * Geri ok (yeşil)
  * Avatar: turuncu-altın gradient, "A" harfi
  * Kişi adı: "ayarlıyo ✂" — beyaz, kalın
  * Alt metin: "İş Hesabı · çevrimiçi" — gri
  * Sağda: 📹 📞 ⋮ ikonları
- Mesaj balonları (#1F2C34, sol hizalı — ayarlıyo'dan gelen):
  * Randevu onay mesajı (emoji + kalın başlık + detaylar)
  * Yol tarifi linki
  * Değerlendirme mesajı
- Alt input bar: "Mesaj" placeholder + yeşil gönder butonu

MESAJ İÇERİKLERİ (aynen kullan):
✂ *ayarlıyo - Randevu Onayı*
Merhaba [Müşteri Adı]! 👋
Randevunuz başarıyla oluşturuldu.
📅 Tarih: [Tarih]
⏰ Saat: [Saat]
💈 Hizmet: [Hizmet]
👤 Personel: [Personel]
🏪 Salon: [Salon Adı]

📍 *Yol Tarifi için tıklayın:*
ayarliyo.com/api/map/[salon-subdomain]

🔗 *Yeni randevu almak için:*
ayarliyo.com/[salon-subdomain]

---

⭐ *Puan vermek için tıklayın:*
ayarliyo.com/rate/[salon-subdomain]/[id]

Sadece HTML kodunu ver.
```

---

## KULLANIM

1. **claude.ai** → yeni sohbet → **Claude Opus** seç
2. Uygulama ekran görselini sürükle-bırak (yükle)
3. Yukarıdaki promptlardan birini yapıştır
4. Üretilen HTML'i `mockup-yeni.html` olarak kaydet
5. Tarayıcıda aç → puppeteer ile PNG export et

## HAZIR MOCKUPLAR

`export/` klasöründe hazır PNG'ler mevcut:
- `mockup-sahne1-bildirim.png` → Uygulama üstünde WhatsApp bildirimi
- `mockup-sahne2-whatsapp.png` → Tam WhatsApp konuşma ekranı
- `mockup-tam.png` → Her iki sahne bir arada

Görselde **ekran alanını kendi uygulama ekran görüntünle** değiştirmek için
`mockup.html` içindeki `.screen-placeholder` div'ini şununla değiştir:
```html
<img src="senin-gorselin.png" class="screen-content" alt="Uygulama">
```
