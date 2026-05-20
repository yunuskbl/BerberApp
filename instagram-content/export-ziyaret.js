const puppeteer = require('puppeteer');
const path = require('path');

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 600, height: 900, deviceScaleFactor: 2 });
  await page.goto('file:///C:/Users/YUNUS/source/BerberApp/instagram-content/mockup-ziyaret.html', { waitUntil: 'networkidle0' });
  await new Promise(r => setTimeout(r, 1500));

  const el = await page.$('.iphone');
  const box = await el.boundingBox();
  await page.screenshot({
    path: path.join(__dirname, 'export', 'mockup-ziyaret-tamamlandi.png'),
    clip: { x: box.x - 40, y: box.y - 40, width: box.width + 80, height: box.height + 80 },
  });
  await browser.close();
  console.log('✅ export/mockup-ziyaret-tamamlandi.png');
})();
