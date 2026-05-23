const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');
const { execSync } = require('child_process');

const FPS          = 30;
const DURATION     = 16;
const FRAME_MS     = 1000 / FPS;
const TOTAL_FRAMES = FPS * DURATION;
const FRAME_DIR    = path.join(__dirname, 'frames-reel');
const W = 1080, H = 1920;   // 9:16 Reels format

if (!fs.existsSync(FRAME_DIR)) fs.mkdirSync(FRAME_DIR);

(async () => {
  console.log('Launching Chrome...');
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });

  const url = 'file:///' + path.resolve(__dirname, 'reel-ad.html').replace(/\\/g, '/');
  await page.goto(url, { waitUntil: 'domcontentloaded' });
  await new Promise(r => setTimeout(r, 200));

  console.log(`Capturing ${TOTAL_FRAMES} frames (${W}x${H}) at virtual ${FPS}fps...`);

  for (let i = 0; i < TOTAL_FRAMES; i++) {
    await page.evaluate((ms) => window._vTick(ms), FRAME_MS);
    await page.screenshot({
      path: path.join(FRAME_DIR, `frame-${String(i).padStart(4,'0')}.png`),
      clip: { x: 0, y: 0, width: W, height: H }
    });
    if (i % 30 === 0) process.stdout.write(`\r  ${i+1}/${TOTAL_FRAMES} (${Math.round(i/TOTAL_FRAMES*100)}%)`);
  }

  console.log('\nEncoding MP4...');
  await browser.close();

  const output = path.join(__dirname, 'reel-ad.mp4');
  execSync(
    `ffmpeg -y -framerate ${FPS} -i "${FRAME_DIR}/frame-%04d.png" -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p -vf "scale=${W}:${H}" "${output}"`,
    { stdio: 'inherit' }
  );

  fs.readdirSync(FRAME_DIR).forEach(f => fs.unlinkSync(path.join(FRAME_DIR, f)));
  fs.rmdirSync(FRAME_DIR);
  console.log(`\nDone → reel-ad.mp4  (${W}x${H}, ${DURATION}s)`);
})();
