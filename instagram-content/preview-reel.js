const puppeteer = require('puppeteer');
const path = require('path');

const W = 1080, H = 1920;

async function captureAt(browser, targetMs, outName) {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  const url = 'file:///' + path.resolve(__dirname, 'reel-ad.html').replace(/\\/g, '/');
  await page.goto(url, { waitUntil: 'domcontentloaded' });
  await new Promise(r => setTimeout(r, 200));

  const FRAME_MS = 1000 / 30;
  const frames = Math.round(targetMs / FRAME_MS);
  for (let i = 0; i < frames; i++) {
    await page.evaluate((ms) => window._vTick(ms), FRAME_MS);
  }

  const outPath = path.join(__dirname, `${outName}.png`);
  await page.screenshot({ path: outPath, clip: { x:0, y:0, width:W, height:H } });
  console.log(`${outName}.png  (vt=${targetMs}ms)`);
  await page.close();
}

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox']
  });

  await captureAt(browser, 2500,  'reel-s1');
  await captureAt(browser, 8000,  'reel-s2');
  await captureAt(browser, 13500, 'reel-s3');

  await browser.close();
})();
