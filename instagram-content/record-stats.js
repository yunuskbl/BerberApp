const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const FPS = 30;
const DURATION = 11; // seconds
const TOTAL_FRAMES = FPS * DURATION;
const FRAME_DIR = path.join(__dirname, 'frames');

if (!fs.existsSync(FRAME_DIR)) fs.mkdirSync(FRAME_DIR);

(async () => {
  console.log('Launching Chrome...');
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-web-security']
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 1 });

  const url = 'file:///' + path.resolve(__dirname, 'stats-animated.html').replace(/\\/g, '/');
  await page.goto(url, { waitUntil: 'domcontentloaded' });

  // Wait for initial render + animations to start
  await new Promise(r => setTimeout(r, 80));

  console.log(`Capturing ${TOTAL_FRAMES} frames at ${FPS}fps (real-time)...`);

  const frameInterval = 1000 / FPS; // ~33ms per frame

  for (let i = 0; i < TOTAL_FRAMES; i++) {
    const framePath = path.join(FRAME_DIR, `frame-${String(i).padStart(4, '0')}.png`);
    const t0 = Date.now();

    await page.screenshot({
      path: framePath,
      clip: { x: 0, y: 0, width: 1080, height: 1080 }
    });

    if (i % 15 === 0) {
      process.stdout.write(`\r  Frame ${i + 1}/${TOTAL_FRAMES} (${((i / TOTAL_FRAMES) * 100).toFixed(0)}%)`);
    }

    // Throttle to target FPS
    const elapsed = Date.now() - t0;
    const wait = frameInterval - elapsed;
    if (wait > 0) await new Promise(r => setTimeout(r, wait));
  }

  console.log('\nAll frames captured!');
  await browser.close();

  console.log('Running ffmpeg...');
  const { execSync } = require('child_process');
  const outputPath = path.join(__dirname, 'stats-animated.mp4');

  execSync(`ffmpeg -y -framerate ${FPS} -i "${FRAME_DIR}/frame-%04d.png" -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -vf "scale=1080:1080" "${outputPath}"`, {
    stdio: 'inherit'
  });

  console.log(`\nDone! Output: ${outputPath}`);

  // Cleanup frames
  fs.readdirSync(FRAME_DIR).forEach(f => fs.unlinkSync(path.join(FRAME_DIR, f)));
  fs.rmdirSync(FRAME_DIR);
  console.log('Frames cleaned up.');
})();
