const puppeteer = require('puppeteer');
const path = require('path');

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const ids = [
    { id: 'kurulum',  file: 'highlight-kurulum.png'  },
    { id: 'randevu',  file: 'highlight-randevu.png'  },
    { id: 'nedir',    file: 'highlight-nedir.png'    },
    { id: 'nasil',    file: 'highlight-nasil.png'    },
    { id: 'yorumlar', file: 'highlight-yorumlar.png' },
  ];

  const fileUrl = `file:///${path.resolve(__dirname, 'highlights.html').replace(/\\/g, '/')}`;

  for (const { id, file } of ids) {
    const page = await browser.newPage();
    await page.setViewport({ width: 1080, height: 1080, deviceScaleFactor: 2 });
    await page.goto(fileUrl, { waitUntil: 'domcontentloaded' });
    await new Promise(r => setTimeout(r, 300));

    const el = await page.$(`#${id}`);
    await el.screenshot({ path: path.join(__dirname, file) });
    console.log(`${file} saved.`);
    await page.close();
  }

  await browser.close();
  console.log('All highlight covers done!');
})();
