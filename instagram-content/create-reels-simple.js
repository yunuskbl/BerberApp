/**
 * ayarlıyo Reels — Sade slideshow (altyazısız)
 * Altyazı CapCut'ta veya ElevenLabs ses + CapCut ile eklenecek
 */

const { execSync } = require('child_process');
const path = require('path');
const fs   = require('fs');

const FFMPEG = 'C:\\Users\\YUNUS\\AppData\\Local\\Microsoft\\WinGet\\Packages\\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\\ffmpeg-8.1.1-full_build\\bin\\ffmpeg.exe';
const SCREENS = path.join(__dirname, 'export');
const OUT     = path.join(__dirname, 'export');
const TMP     = path.join(__dirname, 'export', '_tmp');

if (!fs.existsSync(TMP)) fs.mkdirSync(TMP);

// Her sahne: [dosya, süre_sn]
const scenes = [
  ['mockup-app-s1-salonlar-bildirim.png', 3],
  ['mockup-app-s2-hizmet-bildirim.png',   3],
  ['mockup-app-s3-takvim-bildirim.png',   3],
  ['mockup-app-s4-form-bildirim.png',     3],
  ['mockup-app-s5-onay.png',              3],
  ['mockup-app-s6-whatsapp.png',          4],
];

const W = 1080, H = 1920, FPS = 30;

// 1. Her görsel için ayrı clip üret
console.log('📸 Sahneler oluşturuluyor...');
const clipList = [];

scenes.forEach(([file, dur], i) => {
  const inp  = path.join(SCREENS, file);
  const clip = path.join(TMP, `clip${i}.mp4`);
  clipList.push(clip);

  const cmd = [
    `"${FFMPEG}"`,
    `-y -loop 1 -i "${inp}"`,
    `-vf "scale=${W}:${H}:force_original_aspect_ratio=decrease,pad=${W}:${H}:(ow-iw)/2:(oh-ih)/2:black,setsar=1,fps=${FPS}"`,
    `-t ${dur}`,
    `-c:v libx264 -preset fast -pix_fmt yuv420p`,
    `"${clip}"`
  ].join(' ');

  execSync(cmd, { stdio: 'pipe' });
  console.log(`  ✓ Sahne ${i + 1}/${scenes.length}`);
});

// 2. concat list dosyası
const listFile = path.join(TMP, 'list.txt');
fs.writeFileSync(listFile,
  clipList.map(c => `file '${c.replace(/\\/g, '/')}'`).join('\n')
);

// 3. Concat → final video
const withAudio = process.argv.includes('--audio');
const AUDIO = path.join(__dirname, 'voice.mp3');
const hasAudio = withAudio && fs.existsSync(AUDIO);
const outFile = path.join(OUT, hasAudio ? 'reels-sesli.mp4' : 'reels.mp4');

console.log('🎬 Birleştiriliyor...');

const concatCmd = [
  `"${FFMPEG}"`,
  `-y -f concat -safe 0 -i "${listFile.replace(/\\/g, '/')}"`,
  hasAudio ? `-i "${AUDIO}"` : '',
  hasAudio ? `-map 0:v -map 1:a -shortest` : '',
  `-c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p`,
  `-movflags +faststart`,
  `"${outFile}"`
].filter(Boolean).join(' ');

execSync(concatCmd, { stdio: 'inherit' });

// 4. Temizle
clipList.forEach(c => fs.unlinkSync(c));
fs.unlinkSync(listFile);
fs.rmdirSync(TMP);

const totalDur = scenes.reduce((s, [, d]) => s + d, 0);
console.log(`\n✅ Video hazır: ${outFile}`);
console.log(`   ${totalDur} saniye | ${W}×${H} | 9:16 Reels formatı`);
if (!hasAudio) {
  console.log('\n📌 Ses eklemek için:');
  console.log('   1. voice.mp3 dosyasını buraya kaydet');
  console.log('   2. node create-reels-simple.js --audio');
}
