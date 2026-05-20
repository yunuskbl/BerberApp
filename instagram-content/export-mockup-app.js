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
  await page.setViewport({ width: 1600, height: 900, deviceScaleFactor: 2 });

  const fileUrl = 'file:///C:/Users/YUNUS/source/BerberApp/instagram-content/mockup-app.html';
  await page.goto(fileUrl, { waitUntil: 'networkidle0' });
  await new Promise(r => setTimeout(r, 2000)); // fontlar + animasyonlar

  const phones = await page.$$('.iphone');
  console.log(`Bulunan iPhone sayısı: ${phones.length}`);

  const labels = [
    'app-s1-salonlar-bildirim',
    'app-s2-hizmet-bildirim',
    'app-s3-takvim-bildirim',
    'app-s4-form-bildirim',
    'app-s5-onay',
    'app-s6-degerlendirme',
    'app-s7-whatsapp',
  ];

  for (let i = 0; i < phones.length; i++) {
    const box = await phones[i].boundingBox();
    const outFile = path.join(OUT, `mockup-${labels[i] || `sahne${i+1}`}.png`);
    await page.screenshot({
      path: outFile,
      clip: {
        x: Math.max(0, box.x - 40),
        y: Math.max(0, box.y - 40),
        width: box.width + 80,
        height: box.height + 80,
      },
    });
    console.log(`✓ ${path.basename(outFile)}`);
  }

  // Tam sayfa
  await page.screenshot({
    path: path.join(OUT, 'mockup-app-tam.png'),
    fullPage: true,
  });
  console.log('✓ mockup-app-tam.png');

  await browser.close();
  console.log(`\n✅ Hazır: ${OUT}`);
})();
