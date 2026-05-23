const puppeteer = require('puppeteer');
const path = require('path');

// Capture key frames: 0ms, 900ms, 1500ms, 2200ms, 3500ms
const keyframes = [0, 900, 1500, 2300, 4000];

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  for (const ms of keyframes) {
    const page = await browser.newPage();
    await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 2 });
    const url = 'file:///' + path.resolve(__dirname, 'stats-animated.html').replace(/\\/g, '/');
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await new Promise(r => setTimeout(r, 100));

    await page.evaluate((t) => {
      document.getAnimations().forEach(a => { a.currentTime = t; });
    }, ms);

    await new Promise(r => setTimeout(r, 50));

    await page.screenshot({
      path: path.join(__dirname, `preview-${ms}ms.png`),
      clip: { x: 0, y: 0, width: 1080, height: 1080 }
    });
    console.log(`preview-${ms}ms.png saved`);
    await page.close();
  }

  await browser.close();
})();
