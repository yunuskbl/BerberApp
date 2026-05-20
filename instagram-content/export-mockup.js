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
  await page.setViewport({ width: 900, height: 1800, deviceScaleFactor: 2 });

  const fileUrl = 'file:///C:/Users/YUNUS/source/BerberApp/instagram-content/mockup.html';
  await page.goto(fileUrl, { waitUntil: 'networkidle0' });
  await new Promise(r => setTimeout(r, 1500)); // fontların yüklenmesi için bekle

  // Sahne 1 — Bildirim Overlay
  const scene1 = await page.$('.iphone');
  if (scene1) {
    const box1 = await scene1.boundingBox();
    // Biraz padding ekle
    await page.screenshot({
      path: path.join(OUT, 'mockup-sahne1-bildirim.png'),
      clip: {
        x: Math.max(0, box1.x - 40),
        y: Math.max(0, box1.y - 40),
        width: box1.width + 80,
        height: box1.height + 80,
      },
    });
    console.log('✓ mockup-sahne1-bildirim.png');
  }

  // Sahne 2 — WhatsApp Mesajları
  const allPhones = await page.$$('.iphone');
  if (allPhones.length >= 2) {
    const box2 = await allPhones[1].boundingBox();
    await page.screenshot({
      path: path.join(OUT, 'mockup-sahne2-whatsapp.png'),
      clip: {
        x: Math.max(0, box2.x - 40),
        y: Math.max(0, box2.y - 40),
        width: box2.width + 80,
        height: box2.height + 80,
      },
    });
    console.log('✓ mockup-sahne2-whatsapp.png');
  }

  // Tam sayfa (her ikisi de yan yana/alt alta)
  await page.screenshot({
    path: path.join(OUT, 'mockup-tam.png'),
    fullPage: true,
  });
  console.log('✓ mockup-tam.png');

  await browser.close();
  console.log(`\n✅ Mockup görseller hazır: ${OUT}`);
})();
