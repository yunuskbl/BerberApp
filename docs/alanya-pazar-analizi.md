# Alanya Pazar Analizi ve Ürün Yol Haritası

> Ağustos 2026 · Ayarlıyo'nun Alanya'da hangi sektörlere, hangi ürünle ve hangi gelir
> modeliyle açılacağına dair araştırma ve yol haritası.
>
> Görsel sürüm: https://claude.ai/code/artifact/3c6336df-1d9d-4c1e-8624-46071106c8d0

Bu doküman üç şeyi ayırır: **doğrulanmış rakamlar** (kaynak ve yıl ile), **rakamlardan
çıkan yorum**, ve **öneri**. Fiyat ve gelir tahminleri varsayımdır — pilotla test
edilecek sayılardır, veri değil.

---

## 1. Ana tez

**Alanya'da sorun doluluk değil, kârlılık.**

2026 sezonunda oteller doldu ama otelci de esnaf da daha az kazandı. Emlakta hacim
dört yılda dörtte birine indi. Bu tabloda "sana daha çok müşteri getiririm" satışı
zayıf kalır; **"aynı müşteriden daha çok kazandırırım ve kaybını durdururum"** satışı
güçlüdür.

Ayarlıyo'nun bugünkü altyapısı — çok kiracılı randevu motoru, WhatsApp bildirim hattı,
abonelik ve tenant onay akışı — bu ikinci cümleyi kurmaya zaten çok yakın.

---

## 2. Pazarın hâli

### Rakamlar

| Veri | Değer | Kaynak / yıl |
|---|---|---|
| Konaklama tesisi | 607 tesis, ~176.959 yatak | ALTSO / 2023 |
| Otel sayısı (belgeli) | 575 otel, 167.235 yatak — Antalya ilçelerinde 2. sıra | 2023 |
| Rus vatandaşının Alanya'da aldığı konut | 15.225 (2022'den bu yana) | Tapu / 2022–2026 |
| Yerleşik yabancı nüfus (Alanya) | ~29.835 — Antalya'nın lideri | Göç İdaresi / 2019 |
| Yerleşik yabancı (Antalya geneli) | 102.643 | AKTOB |
| Gazipaşa-Alanya Havalimanı yolcu | 1.004.000 (Antalya Havalimanı: 39,16 M) | DHMİ / 2025 |
| Antalya'ya havayoluyla turist (Oca–Haz) | 5.574.375 | 2026 |
| Türkiye turizm geliri (1. çeyrek) | 9,2 M turist / 9,896 milyar $ | 2026 |

### Turizm: rekor doluluk, daralan kâr

- Antalya 2025'te rekor kırdı; ziyaretçilerin yaklaşık yarısı Rus ve Alman.
- 2026 ilk yarı havayolu: Rusya 1.279.820 · Almanya 1.178.927 · İngiltere 575.974 ·
  Polonya 413.449.
- Sezon uzuyor: ALTİD'e göre ekim dolulukları kasım sonuna kadar sürdü.
- **Ama** sektör temsilcilerinin ortak mesajı: yüksek doluluk yanıltıcı. Enerji,
  personel ve gıda maliyetleri kârlılığı eritiyor; turistin harcama eğilimi düşüyor.
  Otelciler Odası "son yılların en zorlu sezonlarından biri" diyor.

**Satış anlamı:** Kârlılığı sıkışan işletme, "gelirini %20 artırırım" vaadine değil,
bugün kanayan somut bir kaybı durdurma vaadine para verir. Alanya'da o kayıplar
ölçülebilir: gelmeyen randevu (no-show), boş kalan slot/masa/koltuk, acenteye giden
çift haneli komisyon, ve yabancı müşteriyle iletişim kurulamadığı için hiç başlamayan satış.

### Emlak: hacim dörtte bire indi

Rus vatandaşlarının Alanya'daki konut alımları:

| Yıl | Adet |
|---|---|
| 2022 | 6.640 |
| 2023 | 4.203 |
| 2024 | 2.352 |
| 2025 | 1.662 |
| 2026 (Oca–Nis) | 368 |

Ana fren 2022'den beri süren **ikamet iznine kapalı mahalle** uygulaması. ALTSO,
kısıtlar yüzünden Alanya'nın komşu destinasyonlara yatırımcı kaybettiğini söylüyor;
bazı mahallelerin yeniden açılması umut yarattı ama saha henüz hareketlenmedi.

**Yorum:** Bol lead dönemi bitti; kalan lead'in kalitesi ve dönüşümü kritikleşti.
Bu tam olarak CRM'in değer ürettiği koşuldur.

### Yön: sezonu 12 aya yayma hamlesi

- ALTAV 2026 teması: **"Sağlıklı Yaş Alma / 60+ turizmi"**.
- Kışın golf, futbol kampı ve sağlık turizminde rezervasyon artışı.
- 2027–28 tanıtım stratejisi: ekoturizm ve gastronomi.

**Yorum:** Uzun konaklayan, tekrar eden, yerel hizmet satın alan müşteri profili
büyüyor. Bu kişi berbere gider, diş hekimine gider, daire kiralar — yani randevu
motorunun gerçek müşterisidir.

---

## 3. Sektör haritası

Öncelik etiketleri: **[şimdi]** mevcut kodla aylar içinde · **[sonra]** bir mimari
değişiklikle · **[sabırlı]** uzun satış döngüsü.

### Hizmet esnafı — [şimdi]
- **Kim:** Berber, kuaför, güzellik/tırnak, masaj/spa, diş ve estetik klinikleri,
  veteriner, oto servis, kurs.
- **Kanayan yer:** Defter/WhatsApp karmaşası, gelmeyen müşteri, sezonluk personel
  devri, yabancı müşteriyle dil bariyeri.
- **Hamle:** Bugünkü ürün + RU/DE/EN booking sayfası + kapora.
- **Para:** Abonelik + kapora işlem payı.

### Tur, tekne, transfer, rent a car — [şimdi]
- **Kim:** Günlük tekne turu, jeep safari, rafting, dalış, ATV, havalimanı transferi,
  araç kiralama.
- **Kanayan yer:** Rezervasyon defterde, kontenjan çakışıyor, otel/rehber komisyonu
  elle takip ediliyor, iptal ve hava durumu kaosu, ödeme nakit.
- **Hamle:** Kaynak = tarih × kontenjan. Randevu motorunun en kolay genellemesi.
  Online kapora + otomatik komisyon hesabı.
- **Para:** Abonelik + rezervasyon komisyonu (acente oranının çok altında).

### Küçük konaklama — [sonra]
- **Kim:** Apart, pansiyon, butik otel, kiralık villa/daire. 607 tesisin gövdesi burada.
- **Kanayan yer:** Kurumsal PMS + kanal yöneticisi pahalı ve ağır; misafir iletişimi
  WhatsApp'ta dağınık; check-in ve kimlik bildirimi elle.
- **Hamle:** Ağır PMS'e girme. Hafif oda takvimi + dijital check-in + çok dilli misafir
  asistanı ile gir; kanal entegrasyonu sonra.
- **Para:** Oda başı abonelik + doğrudan rezervasyon komisyonu + misafire satılan
  hizmetten pay.

### Yeme-içme — [sonra]
- **Kanayan yer:** Dört dilde menü maliyeti, masa rezervasyonu telefonda, yorum
  toplama yok, kışın müşteriye ulaşamama.
- **Hamle:** Çok dilli QR menü + masa rezervasyonu + Google yorum toplama. Menü ucuz
  giriş kapısı, rezervasyon asıl ürün.
- **Para:** Düşük abonelik, hacim işi.

### Emlak — [sabırlı]
- **Kanayan yer:** Hacim düştü, her lead değerli; portföy Excel'de; yabancı müşteri
  süreci (tapu, DASK, vergi no, ikamet) belirsiz.
- **Hamle:** Çok dilli portföy + lead CRM + görüntüleme randevusu + müşteriye açık
  süreç takibi.
- **Para:** Yüksek abonelik + nitelikli lead ücreti. Satış payı isteme — güven kırar.

### Sağlık ve estetik turizmi — [sabırlı]
- **Kanayan yer:** Lead → konsültasyon → paket → otel/transfer koordinasyonu elle;
  hasta yurt dışından yazıyor, cevap gecikince kaçıyor.
- **Hamle:** Çok dilli lead + tedavi planı + seyahat paketi akışı.
- **Para:** Yüksek abonelik + koordinasyon paketi.

### Perakende ve market — [sabırlı, şimdilik girme]
- Randevu ihtiyacı yok; mevcut ürüne uzak. WhatsApp kampanya modülü olgunlaşınca
  ek satış kalemi olarak değerlendirilir.

---

## 4. Ortak damar

Yedi segmenti tek tek yazılımlamaya kalkarsan yedi ayrı ürünle boğulursun. Hepsi aynı
dört şeyi istiyor; ürünü bu dördü üzerine kur, sektörü sadece *giydir*.

| Ortak ihtiyaç | Alanya'ya özel hâli | Sende ne var |
|---|---|---|
| Kapasite ve rezervasyon | Koltuk, masa, tekne koltuğu, araç, oda, görüntüleme saati — hepsi kaynak × zaman × kontenjan | Slot algoritması ve takvim var. **Kaynak modeli personele sabitli, genelleştirilmeli.** |
| Çok dilli iletişim | Müşterinin yarısı Rus veya Alman. Türkçe arayüz = kaybedilen satış | **Yok.** Booking sayfası ve şablonlar tek dilde |
| WhatsApp'tan yürüyen ilişki | Bölgede birincil kanal WhatsApp; Rus müşteride Telegram da güçlü | **Güçlü.** Meta Cloud API + WPPConnect hibrit, şablonlar, webhook imza doğrulaması |
| Para almak | Kapora olmadan no-show durmuyor; ön ödeme olmadan komisyon modeli kurulamıyor | **Yarım.** iyzico kodda planlı, anahtarlar eksik |

### Kritik teknik karar: Personel → Kaynak

Bugün çekirdek varlık `Staff`. Bunu **Kaynak**'a genellemek (kişi / koltuk / masa /
oda / araç / tekne + kontenjan) tek başına restoran, tur, rent a car ve konaklamayı
açar. Geciktirdiğin her ay, her yeni dikey için kopyala-yapıştır kod demek.
**Faz 1'de yapılmalı, faz 3'te değil.**

### Meta doğrulaması bir "açık iş" değil, bir baraj

`DURUM.md`'ye göre Meta işletme doğrulaması reddedilmiş; şablon oluşturma kapalı ve
OTP WPPConnect'ten serbest metin olarak gidiyor. Çok dilli bildirim demek her dil için
ayrı onaylı şablon demek. Doğrulama açılmadan Alanya stratejisinin en değerli parçası
kilitli kalır.

---

## 5. İki taraflı kazanç nasıl kurulur

Teknik cevap: **senin gelirin, işletmenin kazancından sonra doğsun.** Sabit abonelik
bunu sağlamaz — işletme kazanmasa da öder, bu yüzden kışın iptal eder.

| Mekanizma | İşletme ne kazanır | Sen ne kazanırsın | Neden dayanır |
|---|---|---|---|
| Kapora ile no-show'u durdurma | Boşa giden slot geri kazanılır, kayıp TL olarak ölçülür | Kaporadan işlem payı | Sen ancak işletme parayı aldığında kazanırsın |
| Acente yerine doğrudan rezervasyon | Çift haneli acente komisyonunun büyük kısmı işletmede kalır | Tek haneli komisyon | İşletme farkı ilk ayda hesap makinesinde görür |
| Otel × yerel esnaf köprüsü | Otel misafirine tur/restoran/berber satar, komisyon alır; esnaf sezonluk müşteriye ulaşır | Köprü işlem payı | Üç taraf da kazanır; ağ etkisi burada doğar |
| Çok dilli satış açma | Dil yüzünden hiç başlamayan satışlar başlar (30 bin yerleşik yabancı) | Üst pakete geçiş, dil modülü | Rakiplerde yok; taklidi aylar sürer |
| Nitelikli lead (emlak/sağlık) | Daralan pazarda dönüşen lead | Lead başı ücret veya üst paket | Tek kapanan iş yıllık aboneliği karşılar |

**Satışın tek cümlesi:** Her pilot için ay sonunda tek bir sayı üret — *"Bu ay senin
adına X kaporası tahsil ettik, Y randevu kaybını önledik, Z acente komisyonu sende
kaldı."* Ürüne baştan bir **"kazandırdığımız tutar"** panosu koy.

---

## 6. Gelir modelleri

Ne kadar erken nakit ürettiğine göre sıralı:

| Kalem | Faz | Not |
|---|---|---|
| Abonelik (SaaS) | 0 | Mevcut `PlanType` yapısı hazır; sektöre göre isim ve limit değişir, kod değişmez |
| Kurulum, veri taşıma, eğitim | 0 | Tek seferlik. Yerinde destek uzak rakiplerin veremediği şey |
| Kapora / ön ödeme işlem payı | 1 | iyzico açılınca. Modelin bel kemiği |
| Mesaj paketi | 1 | Meta şablon maliyetinin üstüne marj; kota + aşımda satış |
| Doğrudan rezervasyon komisyonu | 2 | En güçlü "iki taraf kazanır" argümanı |
| Otel × esnaf köprü payı | 2 | Ağ etkisi. Arz birikmeden açma |
| Nitelikli lead / CRM üst paket | 3 | Yüksek bilet, uzun döngü |
| Beyaz etiket / ajans lisansı | 3 | Çoklu kiracı + subdomain altyapısı hazır |
| Tüketici tarafı öne çıkarma | 4 | Erken açılırsa boş pazar yeri olur, markayı yakar |

---

## 7. Yol haritası

Beş faz, ~18 ay. Her fazın sonunda bir **geçiş şartı** var; şart sağlanmadan sonraki
faza geçilmez.

### Faz 0 — Barajlar ve saha doğrulaması (0–6 hafta)

- **Meta işletme doğrulamasını geçir.** Vergi levhasıyla yeniden başvur. Açılmadan
  çok dilli şablon yok, günlük limit düşük, OTP kırılgan WPPConnect'e bağımlı.
- **iyzico anahtarlarını devreye al.** Ödeme kapalıyken faz 1 yazılamaz.
- **Netgsm IP whitelist'ini düzelt** — `DURUM.md`'deki rakam hatası SMS zincirini
  pahalı Twilio'ya düşürüyor.
- **30 saha görüşmesi:** 10 hizmet esnafı, 6 tur/tekne, 6 apart/pansiyon, 4 restoran,
  4 emlak. Tek soru: *"Geçen hafta kaç rezervasyon boşa düştü, kaç müşteriyle dil
  yüzünden anlaşamadın?"*

**Geçiş şartı:** Meta doğrulaması onaylı, test kaporası tahsil edilmiş, 30 görüşme notu elde.

### Faz 1 — Randevudan rezervasyona (1.–4. ay)

- **Personel → Kaynak genellemesi.** Kaynak tipi + kontenjan alanı.
- **Çok dillilik:** booking sayfası ve şablonlar TR / EN / RU / DE.
- **Kapora ve ön ödeme akışı** (iyzico): tutar/yüzde, iptal politikası, otomatik iade.
- **Takvim tarih aralığı API'si** — `DURUM.md`'deki `from`/`to` eksiği; kaynak sayısı
  artınca bugünkü sorgu şişer.
- **"Kazandırdığımız tutar" panosu.**

**Geçiş şartı:** 3 farklı sektörden 3 pilot (1 esnaf, 1 tur/tekne, 1 apart) canlıda
kapora tahsil ediyor.

### Faz 2 — Turizm dikeyi (4.–9. ay)

Bu dikey önce seçilir: envanteri en basit, komisyon acısı en yüksek, ve seni otellerin
kapısına sokan segment odur.

- Kontenjanlı tur/aktivite rezervasyonu; hava durumuna bağlı toplu iptal ve iade.
- **Otel resepsiyonu paneli:** otel misafirine bölgedeki turu/restoranı/berberi satar,
  komisyon otomatik paylaşılır.
- Çok dilli dijital voucher ve QR ile giriş.
- Küçük konaklama için hafif oda takvimi + dijital check-in.
  *Kanal yöneticisine bu fazda girme* — ayrı bir ürün büyüklüğünde.
- Kış tarifesi ve yıllık peşin seçeneğini ürüne göm.

**Geçiş şartı:** Aylık tekrarlayan gelir sabit giderleri karşılıyor; en az 5 otel
köprüde aktif.

### Faz 3 — Emlak ve sağlık turizmi CRM (9.–15. ay)

- Çok dilli portföy; portföyden otomatik ilan sayfası; lead yakalama (WhatsApp/form/Telegram).
- Görüntüleme (viewing) randevusu — randevu motorunun aynısı, yeni isimle.
- **Müşteriye açık süreç takibi:** yabancı alıcı "sıradaki adım ne" diye sürekli
  soruyor; bu ekran tek başına satılabilir.
- Sağlık turizminde: lead → konsültasyon → tedavi planı → otel/transfer koordinasyonu.
- Beyaz etiket lisansını yerel ajanslara aç.

**Geçiş şartı:** Emlak veya sağlıkta 10+ ödeyen müşteri, yenileme oranı ölçülebilir.

### Faz 4 — Tüketici tarafını aç (15.–18. ay)

- Turist ve yerleşik yabancı için çok dilli "Alanya'da ne var, nereden randevu alınır".
- Öne çıkarma ve kampanya satışı.
- İkinci coğrafya: Side, Manavgat, Belek, Antalya merkez. Ürün aynı, satış tekrar edilir.

---

## 8. Fiyatlandırma önerisi

> Bu rakamlar **veri değil, başlangıç varsayımıdır**; ilk 30 görüşmede test edilecek.

| Paket | Kime | Aylık | Üstüne |
|---|---|---|---|
| Başlangıç | Tek kişilik esnaf, deneme | Ücretsiz | 1 kaynak, sınırlı randevu, marka görünür — huninin girişi |
| Esnaf | Berber, kuaför, güzellik, klinik | ~1.200 ₺ | Kaporadan %2 işlem payı |
| Turizm | Tur, tekne, transfer, apart | ~2.900 ₺ | Doğrudan rezervasyonda tek haneli komisyon |
| Kurumsal | Otel, emlak, sağlık turizmi | ~6.500 ₺ | Lead ücreti veya köprü payı |
| Kurulum | Hepsi, tek seferlik | 3.500–7.500 ₺ | Yerinde kurulum, veri taşıma, eğitim |

**Alanya'ya özel — sezon dışı tarifesi:** İşletmelerin bir kısmı kasım–mart arası
kapanır. Sabit ücretle giderlerse martta geri gelmezler; geri gelmeyen müşteriyi
yeniden kazanmak ilk satıştan pahalıdır. İki savunma: (a) kasım–mart indirimli kış
tarifesi, (b) yıllık peşin ödemede 10 ay fiyatı. İkincisi hem churn'ü keser hem sezon
başında nakit akışını öne çeker.

---

## 9. Riskler ve karşı hamleler

| Risk | Neden ciddi | Karşı hamle |
|---|---|---|
| Sezonluk terk | Kasım'da kapanan işletme aboneliği de kapatır | Kış tarifesi + yıllık peşin; kışın çalışan segmentleri (emlak, sağlık, yerleşik yabancı) portföye kat |
| Tek coğrafya, tek pazar | Rus pazarındaki her dalgalanma tüm tabanı aynı anda vurur | Baştan çok dilli kur; faz 4'te Side/Manavgat/Antalya'ya yay |
| Meta bağımlılığı | Şablon onayı, mesaj ücreti ve hesap kısıtı senin kontrolünde değil; WPPConnect yedeği kırılgan | Doğrulamayı geçir; mesajı gelir kalemi yap; Telegram'ı Rus kitlesi için ikinci kanal olarak değerlendir |
| Yerleşik büyük oyuncular | Konaklamada uluslararası platformlar, salon yazılımında global SaaS'lar var | Girmedikleri yer: küçük tesis + çok dilli + WhatsApp öncelikli + yerinde destek. Rekabet ettiğin şey ürün değil, mesafe |
| Aynı anda çok dikey | Referans müşteri üretmeden nakdi bitirmenin en hızlı yolu | Faz geçiş şartlarına uy; bir dikeyde 3 mutlu referans olmadan sonrakine geçme |
| Tahsilat | Küçük esnafta gecikme yaygın; kovalamak zaman yer | Otomatik kart tahsilatı, peşin dönem, gecikmede otomatik kısıtlama |
| Mevzuat (konaklama) | Kimlik bildirimi, turizm belgesi, faturalandırma yükümlülükleri ürün kapsamına girer | Faz 2 öncesi mali müşavir ve ALTSO ile doğrula; doğru kurgulanırsa yük değil satış argümanı |

---

## 10. İlk 30 gün

1. **Meta işletme doğrulamasına yeniden başvur** — vergi levhası PDF'i ile,
   "işletmem listede yok" akışından. Diğer her şeyin önündeki baraj.
2. **iyzico anahtarlarını `.env`'e ekle ve tek bir gerçek kapora tahsil et** —
   uçtan uca çalıştığını görmeden faz 1 planlanamaz.
3. **Booking sayfasını Rusça ve Almanca'ya aç** — şablon çevirisi Meta onayını
   beklerken arayüz çevirisi hemen yapılabilir. Tek en yüksek getirili değişiklik.
4. **30 saha görüşmesi yap, tek soruyla** — cevapların dağılımı hangi dikeyin önce
   geleceğini söyler, bu doküman değil.
5. **Üç pilot seç: 1 esnaf, 1 tekne/tur, 1 apart** — ücretsiz değil, indirimli.
   Bedava kullanan geri bildirim vermez.
6. **"Kaynak" modelinin teknik tasarımını çıkar** — kod yazmadan önce şema ve
   migration planı; sonradan dönmek çok pahalı.
7. **ALTSO ve ALTİD ile temas kur** — üye indirimi karşılığı tanıtım, en ucuz dağıtım
   kanalı.

---

## 11. Bu dokümanın sınırı

Alanya'daki işletme sayılarının sektör kırılımı (kaç emlak ofisi, kaç restoran, kaç
berber) bu araştırmada doğrulanamadı — ALTSO'nun yıllık ekonomik raporu bu veriyi
içeriyor ancak siteye erişim engellendi. Pazar büyüklüğü hesabı yapmadan önce şunları
birinci elden al:

- ALTSO Ekonomik Rapor'un son sayısı — https://www.altso.org.tr/yayinlarimiz/alanya-ekonomik-rapor/
- Alanya Esnaf ve Sanatkârlar Odası (ALESO) üye kırılımı
- ALTİD kapasite tablosu — https://www.altid.org.tr/en/bilgi-hizmetleri/alanya-tesis-kapasite-2/

Fiyat ve gelir rakamları da varsayımdır; 30 görüşme onları yerine oturtacak.

---

## Kaynaklar

- [Alanya konaklama tesis sayısı ve yatak kapasitesi](https://www.mansetalanya.com/iste-alanyada-konaklama-tesis-sayisi-ve-yatak-kapasitesi) — Manşet Alanya
- [Ruslar 4 yılda Alanya'da 15 bin konut aldı](https://www.yenialanya.com/ruslar-alanyadan-vazgecmiyor-4-yilda-15-bin-konut-satin-aldilar) — Yeni Alanya
- [Yabancıya konut satışında Alanya zirvede](https://www.yenialanya.com/yabanciya-konut-satisinda-alanya-zirvedeki-yerini-birakmadi) — Yeni Alanya
- [Alanya'da turizm ve emlak çıkmazı](https://www.gazetealanya.com/alanyada-turizm-ve-emlak-cikmazi-sektor-temsilcilerinden-ortak-cagri) — Gazete Alanya
- [Turizmin kalbi Alanya'da sektör destek bekliyor](https://www.yenialanya.com/turizmin-kalbi-alanyada-sektor-destek-bekliyor) — Yeni Alanya
- [2025 turizm rekoru: ziyaretçilerin yarısı Rus ve Alman](https://www.gazetealanya.com/antalya-ve-alanya-2025te-turizm-rekoru-kirdi-ziyaretcilerin-yarisi-rus-ve-alman) — Gazete Alanya
- [Antalya'ya 6 ayda 5,5 milyon turist](https://www.yenialanya.com/antalyaya-6-ayda-55-milyon-turist-zirvede-rusya-almanya-ve-ingiltere-var) — Yeni Alanya
- [ALTAV 2026 rotası: sağlıklı yaş alma turizmi](https://www.yenialanya.com/altavin-2026-rotasi-belli-oldu-saglikli-yas-alma-turizmi) — Yeni Alanya
- [Turizm sezonu kasım sonuna uzadı](https://www.yenialanya.com/altid-alanyada-turizm-sezonu-kasim-sonuna-uzadi) — ALTİD / Yeni Alanya
- [Alanya yerleşik yabancı nüfusunda lider](https://www.yenialanya.com/haber/23368281/alanya-yerlesik-yabanci-nufusunda-lider-konumda) — Yeni Alanya
- [2026 ilk çeyrek turizm geliri](https://www.yenialanya.com/turizmde-ilk-ceyrek-rekoru-gelir-9-milyar-dolari-asti) — Yeni Alanya
- [Küçük otelde dijital dönüşüm ve PMS maliyetleri](https://www.hmsotel.com/kucuk-otel-isletmeciliginde-dijital-donusum-2026-rehberi/)
