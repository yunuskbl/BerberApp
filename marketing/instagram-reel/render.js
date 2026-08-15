const { chromium } = require('playwright-core');
const path = require('path');
const fs = require('fs');

(async () => {
  const dir = __dirname;
  const framesDir = path.join(dir, 'frames');
  fs.mkdirSync(framesDir, { recursive: true });

  const browser = await chromium.launch({
    executablePath: '/opt/pw-browsers/chromium-1194/chrome-linux/chrome',
    args: ['--no-sandbox', '--force-device-scale-factor=1'],
  });
  const page = await browser.newPage({ viewport: { width: 1080, height: 1920 } });
  await page.goto('file://' + path.join(dir, 'logo-anim.html'));
  await page.evaluate(() => document.fonts.ready);

  const fps = 30, durSec = 3.6, total = Math.round(fps * durSec);
  for (let f = 0; f < total; f++) {
    const t = f / fps;
    await page.evaluate((tt) => window.seek(tt), t);
    await page.screenshot({ path: path.join(framesDir, `f${String(f).padStart(4, '0')}.png`) });
    if (f % 30 === 0) console.log(`frame ${f}/${total}`);
  }
  await browser.close();
  console.log('done', total, 'frames');
})();
