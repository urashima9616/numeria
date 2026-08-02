// 把 AI 生成图的纯色幕布背景抠成透明:PNG 解码 → 从边缘泛洪填充
// 匹配背景色 → 去除边缘色晕 → 输出 RGBA PNG。零依赖。
// 用法: node tools/key-out-bg.mjs <input.png> <output.png>
import { deflateSync, inflateSync } from 'node:zlib';
import { readFileSync, writeFileSync } from 'node:fs';

const [, , inPath, outPath] = process.argv;
if (!inPath || !outPath) { console.error('usage: key-out-bg.mjs <in> <out>'); process.exit(1); }

// ---- PNG 解码(8bit, colorType 2/6, 非隔行) ----
function decodePng(buf) {
  let off = 8;
  let w, h, colorType;
  const idats = [];
  while (off < buf.length) {
    const len = buf.readUInt32BE(off);
    const type = buf.toString('ascii', off + 4, off + 8);
    const data = buf.subarray(off + 8, off + 8 + len);
    if (type === 'IHDR') {
      w = data.readUInt32BE(0); h = data.readUInt32BE(4);
      if (data[8] !== 8) throw new Error('only 8-bit supported');
      colorType = data[9];
      if (colorType !== 2 && colorType !== 6) throw new Error('only RGB/RGBA supported');
      if (data[12] !== 0) throw new Error('interlaced not supported');
    } else if (type === 'IDAT') idats.push(data);
    else if (type === 'IEND') break;
    off += 12 + len;
  }
  const bpp = colorType === 6 ? 4 : 3;
  const stride = w * bpp;
  const raw = inflateSync(Buffer.concat(idats));
  const out = Buffer.alloc(w * h * 4);
  const prev = Buffer.alloc(stride);
  const cur = Buffer.alloc(stride);
  for (let y = 0; y < h; y++) {
    const filter = raw[y * (stride + 1)];
    raw.copy(cur, 0, y * (stride + 1) + 1, (y + 1) * (stride + 1));
    for (let x = 0; x < stride; x++) {
      const a = x >= bpp ? cur[x - bpp] : 0;
      const b = prev[x];
      const c = x >= bpp ? prev[x - bpp] : 0;
      let v = cur[x];
      switch (filter) {
        case 1: v = (v + a) & 0xff; break;
        case 2: v = (v + b) & 0xff; break;
        case 3: v = (v + ((a + b) >> 1)) & 0xff; break;
        case 4: {
          const p = a + b - c;
          const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
          v = (v + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c)) & 0xff;
          break;
        }
      }
      cur[x] = v;
    }
    for (let x = 0; x < w; x++) {
      const si = x * bpp, di = (y * w + x) * 4;
      out[di] = cur[si]; out[di + 1] = cur[si + 1]; out[di + 2] = cur[si + 2];
      out[di + 3] = bpp === 4 ? cur[si + 3] : 255;
    }
    cur.copy(prev);
  }
  return { w, h, rgba: out };
}

// ---- PNG 编码(RGBA, filter 0) ----
const CRC_TABLE = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    t[n] = c >>> 0;
  }
  return t;
})();
const crc32 = buf => {
  let c = 0xffffffff;
  for (const b of buf) c = CRC_TABLE[(c ^ b) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
};
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}
function encodePng(w, h, rgba) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8; ihdr[9] = 6;
  const raw = Buffer.alloc(h * (1 + w * 4));
  for (let y = 0; y < h; y++) {
    raw[y * (1 + w * 4)] = 0;
    rgba.copy(raw, y * (1 + w * 4) + 1, y * w * 4, (y + 1) * w * 4);
  }
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0)),
  ]);
}

// ---- 抠图 ----
const { w, h, rgba } = decodePng(readFileSync(inPath));
const px = i => [rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2]];
const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1], a[2] - b[2]);

// 背景色 = 四角均值
const corners = [0, w - 1, (h - 1) * w, h * w - 1].map(px);
const bg = [0, 1, 2].map(ch => corners.reduce((s, c) => s + c[ch], 0) / 4);
const TOL = 90;

// 从边缘泛洪填充
const visited = new Uint8Array(w * h);
const queue = [];
for (let x = 0; x < w; x++) { queue.push(x, (h - 1) * w + x); }
for (let y = 0; y < h; y++) { queue.push(y * w, y * w + w - 1); }
while (queue.length) {
  const i = queue.pop();
  if (visited[i]) continue;
  visited[i] = 1;
  if (dist(px(i), bg) > TOL) continue;
  rgba[i * 4 + 3] = 0;
  visited[i] = 2;
  const x = i % w, y = (i / w) | 0;
  if (x > 0) queue.push(i - 1);
  if (x < w - 1) queue.push(i + 1);
  if (y > 0) queue.push(i - w);
  if (y < h - 1) queue.push(i + w);
}

// 去色晕:与透明区相邻且仍偏背景色的像素,两轮
for (let pass = 0; pass < 2; pass++) {
  const kill = [];
  for (let y = 0; y < h; y++)
    for (let x = 0; x < w; x++) {
      const i = y * w + x;
      if (rgba[i * 4 + 3] === 0) continue;
      const nearTransparent =
        (x > 0 && rgba[(i - 1) * 4 + 3] === 0) || (x < w - 1 && rgba[(i + 1) * 4 + 3] === 0) ||
        (y > 0 && rgba[(i - w) * 4 + 3] === 0) || (y < h - 1 && rgba[(i + w) * 4 + 3] === 0);
      if (nearTransparent && dist(px(i), bg) < TOL * 1.9) kill.push(i);
    }
  for (const i of kill) rgba[i * 4 + 3] = 0;
}

writeFileSync(outPath, encodePng(w, h, rgba));
const cleared = rgba.filter((_, i) => i % 4 === 3 && rgba[i] === 0).length;
console.log(`keyed ${inPath} -> ${outPath} (bg rgb ${bg.map(Math.round)}, ${w}x${h})`);
