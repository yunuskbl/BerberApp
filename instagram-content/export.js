const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const OUT = path.join(__dirname, 'export');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT);

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  });
  const page = await browser.newPage();

  // Instagram kare formatı
  await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 2 });

  const BASE = 'http://localhost:5501';

  /* ─────────────────────────────────────────
     POST 1 — 6 carousel slaydı
  ───────────────────────────────────────── */
  await page.goto(BASE, { waitUntil: 'networkidle0' });

  // Animasyonları/geçişleri durdur, anında göster
  await page.addStyleTag({ content: `
    *, *::before, *::after {
      animation-duration: 0s !important;
      animation-delay: 0s !important;
      transition-duration: 0s !important;
      opacity: 1 !important;
    }
  `});

  for (let i = 0; i < 6; i++) {
    // Slaydı seç
    await page.evaluate((idx) => {
      window.goTo(idx);
    }, i);

    await new Promise(r => setTimeout(r, 200));

    // Sadece .carousel-track elemanını yakala
    const el = await page.$('.carousel-track');
    const clip = await el.boundingBox();

    // 1080×1080 merkeze al
    await page.screenshot({
      path: path.join(OUT, `post1-slayt${i + 1}.png`),
      clip: {
        x: clip.x,
        y: clip.y,
        width: clip.width,
        height: clip.height,
      },
    });

    console.log(`✓ post1-slayt${i + 1}.png`);
  }

  /* ─────────────────────────────────────────
     POST 2 — tek görsel
  ───────────────────────────────────────── */
  // Animasyonları bekle (wordReveal delay'leri için)
  await page.evaluate(() => {
    document.querySelectorAll('style').forEach(s => {
      if (s.textContent.includes('animation-duration')) s.remove();
    });
  });

  // Animasyon gecikmelerini sıfırla, hepsini görünür yap
  await page.addStyleTag({ content: `
    *, *::before, *::after {
      animation-duration: 0.01s !important;
      animation-delay: 0s !important;
      transition-duration: 0s !important;
      opacity: 1 !important;
      -webkit-text-fill-color: unset;
    }
    .p2-highlight {
      background: linear-gradient(90deg, #C9953A 0%, #F0C060 35%, #D4A84B 65%, #C9953A 100%) !important;
      -webkit-background-clip: text !important;
      -webkit-text-fill-color: transparent !important;
      background-clip: text !important;
    }
    .p2-divider, .p2-divider-bottom { width: 60px !important; }
  `});

  await new Promise(r => setTimeout(r, 300));

  const el2 = await page.$('.single-post');
  const clip2 = await el2.boundingBox();

  await page.screenshot({
    path: path.join(OUT, 'post2-tek-gorsel.png'),
    clip: {
      x: clip2.x,
      y: clip2.y,
      width: clip2.width,
      height: clip2.height,
    },
  });

  console.log('✓ post2-tek-gorsel.png');

  await browser.close();
  console.log(`\n✅ Tüm görseller hazır: ${OUT}`);
})();
