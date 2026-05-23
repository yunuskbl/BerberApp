const puppeteer = require('puppeteer');
const path = require('path');

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 2 });
  const url = 'file:///' + path.resolve(__dirname, 'stats-post.html').replace(/\\/g, '/');
  await page.goto(url, { waitUntil: 'domcontentloaded' });
  await new Promise(r => setTimeout(r, 400));
  await page.screenshot({
    path: path.join(__dirname, 'stats-post.png'),
    clip: { x: 0, y: 0, width: 1080, height: 1080 }
  });
  console.log('stats-post.png saved.');
  await browser.close();
})();
