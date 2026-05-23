const puppeteer = require('puppeteer');
const path = require('path');
(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox']
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 2 });
  const url = 'file:///' + path.resolve(__dirname, 'stats-animated.html').replace(/\\/g, '/');
  await page.goto(url, { waitUntil: 'domcontentloaded' });
  // Wait 1.8 seconds real time — counter for %47 should be mid-count
  await new Promise(r => setTimeout(r, 1800));
  await page.screenshot({ path: path.join(__dirname, 'check-counter.png'), clip: { x:0,y:0,width:1080,height:1080 } });
  await browser.close();
  console.log('done');
})();
