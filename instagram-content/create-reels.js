/**
 * ayarlıyo — Reels Video Creator
 * Kullanım:
 *   node create-reels.js           (ses olmadan)
 *   node create-reels.js --audio   (voice.mp3 ile sesli)
 */

const ffmpeg = require('fluent-ffmpeg');
const ffmpegPath = require('ffmpeg-static');
const path = require('path');
const fs   = require('fs');

// Sistem FFmpeg varsa onu kullan, yoksa npm binary
const sysFfmpeg = 'C:\\Users\\YUNUS\\AppData\\Local\\Microsoft\\WinGet\\Packages\\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\\ffmpeg-8.1.1-full_build\\bin\\ffmpeg.exe';
ffmpeg.setFfmpegPath(fs.existsSync(sysFfmpeg) ? sysFfmpeg : ffmpegPath);

const OUT     = path.join(__dirname, 'export');
const SCREENS = path.join(__dirname, 'export');
const AUDIO   = path.join(__dirname, 'voice.mp3');
const withAudio = process.argv.includes('--audio');

// 9:16 Reels formatı
const W = 1080;
const H = 1920;
const FPS = 30;
const FONT = 'C\\\\:/Windows/Fonts/arialbd.ttf'; // bold arial

// Her sahne: [dosya, süre_sn, altyazi_satir1, altyazi_satir2?]
const scenes = [
  ['mockup-app-s1-salonlar-bildirim.png', 3,  'Salonunu ayarliyo\'ya bagla.'],
  ['mockup-app-s2-hizmet-bildirim.png',   3,  'Musteri hizmetini seciyor.'],
  ['mockup-app-s3-takvim-bildirim.png',   3,  'Tarihini belirliyor.'],
  ['mockup-app-s4-form-bildirim.png',     3,  'Bilgilerini giriyor.'],
  ['mockup-app-s5-onay.png',              3,  'Randevu onayland!'],
  ['mockup-app-s6-whatsapp.png',          4,  'WhatsApp\'a otomatik bildirim.', 'ayarliyo.'],
];

function drawtext(text, y, size = 52) {
  const safe = text.replace(/'/g, "’").replace(/:/g, '\\:').replace(/\\/g, '/');
  return (
    `drawtext=fontfile='${FONT}':` +
    `text='${safe}':` +
    `fontcolor=white:fontsize=${size}:` +
    `box=1:boxcolor=black@0.6:boxborderw=16:` +
    `x=(w-text_w)/2:y=${y}`
  );
}

function sceneFilter(i, file, dur, line1, line2) {
  const dt1 = drawtext(line1, line2 ? 'h-200' : 'h-140');
  const dt2 = line2 ? drawtext(line2, 'h-130', 44) : null;

  const chain =
    `scale=${W}:${H}:force_original_aspect_ratio=decrease,` +
    `pad=${W}:${H}:(ow-iw)/2:(oh-ih)/2:black,` +
    `setsar=1,fps=${FPS},trim=duration=${dur},` +
    dt1 + (dt2 ? ',' + dt2 : '');

  return `[${i}:v]${chain}[v${i}]`;
}

function buildFilter() {
  const parts = scenes.map((s, i) =>
    sceneFilter(i, s[0], s[1], s[2], s[3])
  );
  const labels = scenes.map((_, i) => `[v${i}]`).join('');
  parts.push(`${labels}concat=n=${scenes.length}:v=1:a=0[vout]`);
  return parts.join('; ');
}

async function build() {
  const outName = withAudio ? 'reels-sesli.mp4' : 'reels.mp4';
  const outFile = path.join(OUT, outName);
  const totalDur = scenes.reduce((s, sc) => s + sc[1], 0);

  const cmd = ffmpeg();

  scenes.forEach(([file]) =>
    cmd.input(path.join(SCREENS, file)).inputOptions(['-loop 1'])
  );

  if (withAudio && fs.existsSync(AUDIO)) {
    cmd.input(AUDIO);
    console.log('🎙  Ses eklendi:', AUDIO);
  }

  const hasAudio = withAudio && fs.existsSync(AUDIO);

  cmd
    .complexFilter(buildFilter(), 'vout')
    .outputOptions([
      '-map [vout]',
      hasAudio ? `-map ${scenes.length}:a` : '-an',
      '-c:v libx264',
      '-preset fast',
      '-crf 20',
      '-pix_fmt yuv420p',
      `-t ${totalDur}`,
      '-movflags +faststart',
      `-r ${FPS}`,
    ].filter(Boolean))
    .output(outFile)
    .on('start', () => console.log('▶  FFmpeg baslatildi...'))
    .on('progress', p => {
      if (p.percent) process.stdout.write(`\r  %${Math.round(p.percent)} tamamlandi`);
    })
    .on('end', () => {
      console.log(`\n✅ Video hazir: ${outFile}`);
      console.log(`   Sure: ${totalDur} sn | ${W}x${H} 9:16 Reels`);
    })
    .on('error', err => console.error('\n❌ Hata:', err.message))
    .run();
}

build();
