// 把 prototype/js/sprites.js 的像素字符网格 + 背景场景导出为 PNG,
// 供 Unity 工程使用。零依赖:PNG 编码用 Node 内置 zlib 手写。
// 用法: node tools/export-sprites.mjs
import { deflateSync } from 'node:zlib';
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ADDMANDER, DUPLIROCK, GEM, SHIELD, COUNTIPILLAR, DOUBLIT, SUMDRAKE } from '../prototype/js/sprites.js';


const root = join(dirname(fileURLToPath(import.meta.url)), '..');

// ---- 最小 PNG 编码器(RGBA8,无滤波) ----
const CRC_TABLE = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c >>> 0;
  }
  return t;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (const b of buf) c = CRC_TABLE[(c ^ b) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

function encodePng(width, height, rgba) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;  // bit depth
  ihdr[9] = 6;  // color type RGBA
  // raw scanlines: filter byte 0 + row data
  const raw = Buffer.alloc(height * (1 + width * 4));
  for (let y = 0; y < height; y++) {
    raw[y * (1 + width * 4)] = 0;
    rgba.copy(raw, y * (1 + width * 4) + 1, y * width * 4, (y + 1) * width * 4);
  }
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

function hexToRgba(hex) {
  const h = hex.replace('#', '');
  const full = h.length === 3 ? h.split('').map(c => c + c).join('') : h;
  return [parseInt(full.slice(0, 2), 16), parseInt(full.slice(2, 4), 16), parseInt(full.slice(4, 6), 16), 255];
}

// ---- 精灵:字符网格 → PNG ----
function spriteToPng(sprite) {
  const rows = sprite.map;
  const w = rows[0].length, h = rows.length;
  const rgba = Buffer.alloc(w * h * 4);
  rows.forEach((row, y) => {
    for (let x = 0; x < w; x++) {
      const color = sprite.palette[row[x]];
      if (!color) continue;
      const [r, g, b, a] = hexToRgba(color);
      const i = (y * w + x) * 4;
      rgba[i] = r; rgba[i + 1] = g; rgba[i + 2] = b; rgba[i + 3] = a;
    }
  });
  return encodePng(w, h, rgba);
}

// ---- 背景:重放 sprites.js paintBackground 的绘制命令 ----
function paintBackgroundPng() {
  const W = 480, H = 270;
  const rgba = Buffer.alloc(W * H * 4);
  const rect = (c, x, y, w, h) => {
    const [r, g, b] = hexToRgba(c);
    const x0 = Math.max(0, Math.round(x)), y0 = Math.max(0, Math.round(y));
    const x1 = Math.min(W, Math.round(x + w)), y1 = Math.min(H, Math.round(y + h));
    for (let yy = y0; yy < y1; yy++)
      for (let xx = x0; xx < x1; xx++) {
        const i = (yy * W + xx) * 4;
        rgba[i] = r; rgba[i + 1] = g; rgba[i + 2] = b; rgba[i + 3] = 255;
      }
  };

  rect('#9fd4ef', 0, 0, W, 60);
  rect('#aedcf3', 0, 60, W, 50);
  rect('#c2e6f6', 0, 110, W, 40);
  rect('#fff3b0', 40, 26, 22, 22);
  rect('#ffe97a', 44, 30, 14, 14);
  const cloud = (x, y, s) => {
    rect('#ffffff', x, y + 4 * s, 30 * s, 6 * s);
    rect('#ffffff', x + 6 * s, y, 16 * s, 8 * s);
    rect('#eef7fc', x + 2 * s, y + 8 * s, 26 * s, 2 * s);
  };
  cloud(120, 28, 1); cloud(300, 48, 1.4); cloud(410, 20, 0.8);
  for (let i = 0; i < W; i += 4) {
    const h = 26 + 18 * Math.abs(Math.sin(i * 0.02)) + 8 * Math.abs(Math.sin(i * 0.053 + 2));
    rect('#8fa8c8', i, 150 - h, 4, h);
  }
  for (let i = 0; i < W; i += 4) {
    const h = 16 + 12 * Math.abs(Math.sin(i * 0.03 + 5));
    rect('#a9bdd6', i, 150 - h, 4, h);
  }
  const tree = (x, base, c) => {
    rect(c, x + 3, base - 4, 4, 4);
    for (let lvl = 0; lvl < 3; lvl++) {
      const w = 14 - lvl * 4, y = base - 8 - lvl * 6;
      rect(c, x + (10 - w) / 2, y, w, 6);
    }
  };
  for (let x = -6; x < W; x += 26) tree(x, 152, '#3e7a4c');
  for (let x = 8; x < W; x += 34) tree(x, 156, '#2f5f3b');
  rect('#7cb342', 0, 152, W, 40);
  rect('#66a234', 0, 192, W, 44);
  rect('#558b2f', 0, 236, W, 34);
  const tuft = (x, y, c) => { rect(c, x, y, 2, 4); rect(c, x + 3, y - 2, 2, 6); rect(c, x + 6, y, 2, 4); };
  for (let i = 0; i < 26; i++) {
    const x = (i * 97 + 13) % W, y = 160 + ((i * 53) % 96);
    tuft(x, y, i % 2 ? '#8fce5a' : '#4c7f28');
    if (i % 5 === 0) { rect('#ffffff', x + 12, y + 1, 2, 2); rect('#ffd24a', x + 12, y - 1, 2, 2); }
  }
  return encodePng(W, H, rgba);
}

// ---- 山脉战斗背景:灰蓝色调,巨岩替代树线 ----
function paintMountainBackgroundPng() {
  const W = 480, H = 270;
  const rgba = Buffer.alloc(W * H * 4);
  const rect = (c, x, y, w, h) => {
    const [r, g, b] = hexToRgba(c);
    const x0 = Math.max(0, Math.round(x)), y0 = Math.max(0, Math.round(y));
    const x1 = Math.min(W, Math.round(x + w)), y1 = Math.min(H, Math.round(y + h));
    for (let yy = y0; yy < y1; yy++)
      for (let xx = x0; xx < x1; xx++) {
        const i = (yy * W + xx) * 4;
        rgba[i] = r; rgba[i + 1] = g; rgba[i + 2] = b; rgba[i + 3] = 255;
      }
  };

  // 天空:灰蓝色带
  rect('#aebfd2', 0, 0, W, 60);
  rect('#bccadb', 0, 60, W, 50);
  rect('#cbd7e4', 0, 110, W, 40);
  // 苍白的太阳
  rect('#f2ecd8', 40, 26, 22, 22);
  rect('#e8e0c4', 44, 30, 14, 14);
  // 云
  const cloud = (x, y, s) => {
    rect('#ffffff', x, y + 4 * s, 30 * s, 6 * s);
    rect('#ffffff', x + 6 * s, y, 16 * s, 8 * s);
  };
  cloud(140, 24, 1); cloud(330, 44, 1.3);
  // 两层大山(比森林版更高更近)
  for (let i = 0; i < W; i += 4) {
    const h = 46 + 34 * Math.abs(Math.sin(i * 0.014)) + 12 * Math.abs(Math.sin(i * 0.047 + 2));
    rect('#5f7189', i, 150 - h, 4, h);
  }
  for (let i = 0; i < W; i += 4) {
    const h = 28 + 22 * Math.abs(Math.sin(i * 0.024 + 5));
    rect('#7a8ba3', i, 150 - h, 4, h);
    if (i % 28 === 0) rect('#e8eef4', i, 150 - h, 4, 3); // 雪顶
  }
  // 巨岩尖(替代树线)
  const crag = (x, base, h, c) => {
    for (let lvl = 0; lvl < h; lvl += 3) {
      const w = Math.max(2, 12 - lvl);
      rect(c, x + (12 - w) / 2, base - lvl - 3, w, 3);
    }
  };
  for (let x = -4; x < W; x += 30) crag(x, 154, 12, '#6b7684');
  for (let x = 12; x < W; x += 38) crag(x, 158, 9, '#59636f');
  // 高山草甸(灰绿)
  rect('#8a9a6a', 0, 152, W, 40);
  rect('#76885c', 0, 192, W, 44);
  rect('#647851', 0, 236, W, 34);
  // 碎石与稀疏草
  for (let i = 0; i < 22; i++) {
    const x = (i * 103 + 17) % W, y = 162 + ((i * 47) % 92);
    if (i % 3 === 0) { rect('#9aa4ae', x, y, 5, 4); rect('#7d8894', x + 1, y + 1, 3, 2); }
    else { rect('#94a674', x, y, 2, 4); rect('#94a674', x + 3, y - 2, 2, 6); }
  }
  return encodePng(W, H, rgba);
}

// ---- 导出 ----
const spriteDir = join(root, 'unity/Assets/Resources/Art/Sprites');
const bgDir = join(root, 'unity/Assets/Resources/Art/Backgrounds');
mkdirSync(spriteDir, { recursive: true });
mkdirSync(bgDir, { recursive: true });

const sprites = {
  addmander: ADDMANDER, duplirock: DUPLIROCK, gem: GEM, shield: SHIELD,
  countipillar: COUNTIPILLAR, doublit: DOUBLIT, sumdrake: SUMDRAKE,
};
for (const [name, sprite] of Object.entries(sprites)) {
  const path = join(spriteDir, `${name}.png`);
  writeFileSync(path, spriteToPng(sprite));
  console.log(`wrote ${path} (${sprite.map[0].length}x${sprite.map.length})`);
}
writeFileSync(join(bgDir, 'forest-battle.png'), paintBackgroundPng());
console.log(`wrote ${join(bgDir, 'forest-battle.png')} (480x270)`);
writeFileSync(join(bgDir, 'mountain-battle.png'), paintMountainBackgroundPng());
console.log(`wrote ${join(bgDir, 'mountain-battle.png')} (480x270)`);
