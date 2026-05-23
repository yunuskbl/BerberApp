const puppeteer = require('puppeteer');
const path = require('path');

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const file = path.resolve(__dirname, 'carousel-1.html');
  const fileUrl = `file:///${file.replace(/\\/g, '/')}`;

  for (let n = 1; n <= 5; n++) {
    const page = await browser.newPage();
    await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 2 });
    await page.goto(`${fileUrl}?n=${n}`, { waitUntil: 'domcontentloaded' });
    await new Promise(r => setTimeout(r, 500));
    await page.screenshot({
      path: path.join(__dirname, `slide-${n}.png`),
      fullPage: false,
      clip: { x: 0, y: 0, width: 1080, height: 1080 }
    });
    console.log(`Slide ${n} saved.`);
    await page.close();
  }

  await browser.close();
  console.log('Done!');
})();
