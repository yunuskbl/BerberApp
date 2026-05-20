/**
 * ayarlıyo favicon üretici
 * Çıktı: admin-panel/public/favicon.ico + favicon-192.png + favicon-512.png
 */
const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const PUBLIC = path.join(__dirname, '..', 'admin-panel', 'public');

const SVG_HTML = `<!DOCTYPE html>
<html>
<head><meta charset="UTF-8">
<style>*{margin:0;padding:0}html,body{width:512px;height:512px;overflow:hidden}</style>
</head>
<body>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="512" height="512">
  <rect width="512" height="512" rx="110" fill="#111827"/>
  <text x="128" y="370" font-family="Arial" font-size="305" font-weight="900" fill="white">a</text>
  <circle cx="400" cy="128" r="52" fill="white"/>
</svg>
</body>
</html>`;

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  });
  const page = await browser.newPage();

  // 512x512 render
  await page.setViewport({ width: 512, height: 512, deviceScaleFactor: 1 });
  await page.setContent(SVG_HTML, { waitUntil: 'domcontentloaded' });
  await new Promise(r => setTimeout(r, 500));

  const png512 = await page.screenshot({ type: 'png' });
  fs.writeFileSync(path.join(PUBLIC, 'favicon-512.png'), png512);
  console.log('✓ favicon-512.png');

  // 192x192
  await page.setViewport({ width: 192, height: 192, deviceScaleFactor: 1 });
  await page.setContent(SVG_HTML.replace(/512px/g, '192px').replace(/512px/g, '192px'), { waitUntil: 'domcontentloaded' });
  const png192 = await page.screenshot({ type: 'png' });
  fs.writeFileSync(path.join(PUBLIC, 'favicon-192.png'), png192);
  console.log('✓ favicon-192.png');

  // 32x32 → favicon için
  await page.setViewport({ width: 32, height: 32, deviceScaleFactor: 4 }); // 128px @ 4x
  await page.setContent(SVG_HTML.replace(/512px/g,'32px').replace(/width="512" height="512"/g,'width="32" height="32"').replace(/rx="110"/,'rx="7"').replace(/font-size="300"/,'font-size="20"').replace(/x="130" y="365"/,'x="8" y="24"').replace(/r="52"/,'r="4"').replace(/cx="400" cy="130"/,'cx="26" cy="8"'), { waitUntil: 'domcontentloaded' });
  const png32 = await page.screenshot({ type: 'png' });
  fs.writeFileSync(path.join(PUBLIC, 'favicon-32.png'), png32);
  console.log('✓ favicon-32.png');

  await browser.close();

  // favicon.ico yerine PNG kullan (modern tarayıcılar PNG destekler)
  // 32px PNG'yi favicon.png olarak da kaydet
  fs.copyFileSync(path.join(PUBLIC, 'favicon-512.png'), path.join(PUBLIC, 'favicon.png'));
  console.log('✓ favicon.png (512px)');

  console.log('\n✅ Favicon görseller hazır:', PUBLIC);
  console.log('   Sıradaki adım: index.html güncelle + build et');
})();
