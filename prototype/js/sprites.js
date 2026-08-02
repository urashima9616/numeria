// 逐像素手绘资产:map 为字符串网格,palette 把字符映射为颜色,'.' 为透明。
// 渲染到 1:1 尺寸的 canvas,交给 CSS `image-rendering: pixelated` 放大保持锐利。

export function drawSprite(canvas, sprite) {
  const rows = sprite.map;
  canvas.width = rows[0].length;
  canvas.height = rows.length;
  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  rows.forEach((row, y) => {
    for (let x = 0; x < row.length; x++) {
      const color = sprite.palette[row[x]];
      if (!color) continue;
      ctx.fillStyle = color;
      ctx.fillRect(x, y, 1, 1);
    }
  });
}

export const ADDMANDER = {
  palette: {
    O: '#5d2a12', R: '#e8703a', r: '#f59b5e', C: '#ffe3b3',
    W: '#ffffff', P: '#3b2b20', F: '#ffd24a', f: '#ff5a2e',
  },
  map: [
    '.......F........',
    '......FfF.......',
    '.....OOfOO......',
    '....ORRRRRO.....',
    '...ORRRRRRRO....',
    '..ORWPRRRWPRO...',
    '..ORWPRRRWPRO...',
    '..ORRRRRRRRRO...',
    '..ORrRPPPRrRO...',
    '...ORRRRRRRO....',
    '..OORCCCCCROO...',
    '.ORRRCCCCCRRRO..',
    'F.ORRCCCCCRRO...',
    'fF.ORRCCCRRO....',
    'Ff..OROOOORO....',
    '.....OO..OO.....',
  ],
};

export const DUPLIROCK = {
  palette: {
    G: '#3d4148', S: '#7f8791', s: '#aeb6c0',
    W: '#ffffff', P: '#2e3338', M: '#7fae5a',
  },
  map: [
    '................',
    '....GSSSSSG.....',
    '..GGSsssssSGG...',
    '.GSsssssssssSG..',
    '.GSsWPsssWPsSG..',
    'GSssWPsssWPssSG.',
    'GSsssssssssssSG.',
    'GSsssGGGGGsssSG.',
    'GSsssssssssssSG.',
    '.GSsMsssssMsSG..',
    '.GSsssssssssSG..',
    '..GSSsssssSSG...',
    '...GSSSSSSSG....',
    '..GG.GGGGG.GG...',
    '.GSSG.....GSSG..',
    '..GG.......GG...',
  ],
};

export const GEM = {
  palette: { O: '#8a5a00', Y: '#ffc82e', y: '#ffe792', W: '#fff7d6' },
  map: [
    '...OOOO...',
    '..OYYYYO..',
    '.OYWyYYYO.',
    'OYyWyYYYYO',
    'OYYyYYYYYO',
    'OYYYYYYYYO',
    '.OYYYYYYO.',
    '..OYYYYO..',
    '...OYYO...',
    '....OO....',
  ],
};

export const SHIELD = {
  palette: { O: '#20406e', B: '#3f7fc4', b: '#8fc0ee', W: '#e8f3ff' },
  map: [
    'OOOOOOOOOO',
    'OBBbWWbBBO',
    'OBbBWWBbBO',
    'OBBBWWBBBO',
    'OBWWWWWWBO',
    'OBWWWWWWBO',
    'OBBBWWBBBO',
    '.OBBWWBBO.',
    '..OBBBBO..',
    '...OOOO...',
  ],
};

export const COUNTIPILLAR = {
  palette: {
    O: '#2e4a24', B: '#79c25a', b: '#a5d98a',
    E: '#ffffff', P: '#2e3338', R: '#ff9d9d', A: '#5d4037',
  },
  map: [
    '................',
    '................',
    '...........A..A.',
    '...........A..A.',
    '..........OOOOO.',
    '.........OBbbbBO',
    '....OOO..OBEPEPO',
    '...OBBBO.OBBBBBO',
    '..OBbBBOOBBRBBO.',
    '.OBBBBBOBBBBBBO.',
    '.OBBBBOOBBBBBO..',
    '..OOOO..OOOOO...',
    '..O..O...O..O...',
    '.OO.OO...OO.OO..',
    '................',
    '................',
  ],
};

export const DOUBLIT = {
  palette: {
    G: '#3d4148', S: '#8f979f', s: '#b8bec6',
    W: '#ffffff', P: '#2e3338', M: '#7fae5a', R: '#d98a8a',
  },
  map: [
    '................',
    '..GGG.....GGG...',
    '.GsssG...GsssG..',
    'GsssssG.GsssssG.',
    'GsWPssG.GsWPssG.',
    'GsWPssGGGsWPssG.',
    'GssssssGssssssG.',
    'GsRssssssssRssG.',
    '.GssMssssssssG..',
    '.GssssssMssssG..',
    '..GsssssssssG...',
    '...GGGGGGGGG....',
    '..GG.GG.GG.GG...',
    '.GssG.....GssG..',
    '..GG.......GG...',
    '................',
  ],
};

export const SUMDRAKE = {
  palette: {
    O: '#5d2a12', R: '#d84f2a', r: '#f0764a', C: '#ffe3b3',
    W: '#ffffff', P: '#3b2b20', F: '#ffd24a', f: '#ff5a2e', w: '#8a3a1c',
  },
  map: [
    '....O......O....',
    '....OO....OO....',
    '.....OOOOOO.....',
    '....ORRRRRRO....',
    '...ORRRRRRRRO...',
    '..ORWPRRRRWPO...',
    '..ORWPRRRRWPO...',
    '..ORRRRRRRRRRO..',
    '..ORrRPPPPRrRO..',
    'w..ORRRRRRRRO..w',
    'wwOORCCCCCCROOww',
    'wwORRRCCCCRRROww',
    '.wORRRCCCCRRROw.',
    'F..ORRRRRRRRO..F',
    'fF..OROOOORO..Ff',
    '.....OO..OO.....',
  ],
};

export const TILE_GRASS = {
  palette: { G: '#7cb342', L: '#8fce5a', D: '#66a234' },
  map: [
    'GGGGGGGGGGGGGGGG',
    'GGLGGGGGGGGDGGGG',
    'GGGGGGGLGGGGGGGG',
    'GGGGDGGGGGGGGGLG',
    'GLGGGGGGGGDGGGGG',
    'GGGGGGGGGGGGGGGG',
    'GGGGGGLGGGGGGDGG',
    'GGDGGGGGGGGGGGGG',
    'GGGGGGGGGLGGGGGG',
    'GLGGGDGGGGGGGGGG',
    'GGGGGGGGGGGGLGGG',
    'GGGGGGGGDGGGGGGG',
    'GGLGGGGGGGGGGGDG',
    'GGGGGGGLGGGGGGGG',
    'GGDGGGGGGGGLGGGG',
    'GGGGGGGGGGGGGGGG',
  ],
};

export const TILE_TREE = {
  palette: { G: '#7cb342', L: '#8fce5a', D: '#66a234', F: '#2f6b3c', f: '#3e8a4e', O: '#5d4037' },
  map: [
    'GGGGGGGGGGGGGGGG',
    'GGGGGGGffGGGGGGG',
    'GGGGGGffffGGGGGG',
    'GGGGGFffffFGGGGG',
    'GGGGGffffffGGGGL',
    'GGGGFffffffFGGGG',
    'GGGFffffffffFGGG',
    'GGGGffffffffGGGG',
    'GGFffffffffffFGG',
    'GFffffffffffffFG',
    'GGFFffffffffFFGG',
    'GGGGGGOOGGGGGGGG',
    'GLGGGGOOGGGGDGGG',
    'GGGGGGOOGGGGGGGG',
    'GGGGGDGGGGLGGGGG',
    'GGGGGGGGGGGGGGGG',
  ],
};

export const TILE_BUSH = {
  palette: { G: '#7cb342', L: '#8fce5a', B: '#3e7a4c', l: '#5aa968' },
  map: [
    'GGGGGGGGGGGGGGGG',
    'GGGGGGGGGGGGGGGG',
    'GGBGGGBGGGBGGBGG',
    'GGBGGBBGGGBGGBGG',
    'GBBBGBBGBBBGBBBG',
    'GBBBBBBBBBBBBBBG',
    'GBlBBBBlBBBBlBBG',
    'BBBBlBBBBBlBBBBB',
    'BBBBBBBlBBBBBlBB',
    'BBlBBBBBBBlBBBBB',
    'BBBBBlBBBBBBBBBB',
    'GBBBBBBBlBBBBBGG',
    'GGBBlBBBBBBBBGGG',
    'GGGBBBBBlBBGGGGG',
    'GGGGGGGGGGGGGGGG',
    'GGGGGGGGGGGGGGGG',
  ],
};

export const TILE_CHEST = {
  palette: { G: '#7cb342', L: '#8fce5a', W: '#8d5a2b', w: '#5d3a18', Y: '#ffd24a' },
  map: [
    'GGGGGGGGGGGGGGGG',
    'GGGGGGGGGGGGGGGG',
    'GGGGGGGGGGGGGGGG',
    'GGGwwwwwwwwwwGGG',
    'GGwWWWWWWWWWWwGG',
    'GGwWWWWWWWWWWwGG',
    'GGwwwwwwwwwwwwGG',
    'GGwWWWYYWWWWWwGG',
    'GGwWWWYYWWWWWwGG',
    'GGwWWWWWWWWWWwGG',
    'GGwWWWWWWWWWWwGG',
    'GGwWWWWWWWWWWwGG',
    'GGwwwwwwwwwwwwGG',
    'GGGGGGGGGGGGGGGG',
    'GGLGGGGGGGGDGGGG',
    'GGGGGGGGGGGGGGGG',
  ],
};

export const TILE_PORTAL = {
  palette: { G: '#7cb342', P: '#7e57c2', p: '#b39ddb', E: '#ede7f6' },
  map: [
    'GGGGGGGGGGGGGGGG',
    'GGGGGPPPPPPGGGGG',
    'GGGPPppppppPPGGG',
    'GGPPpEEEEEEpPPGG',
    'GGPpEEEEEEEEpPGG',
    'GPPpEEEEEEEEpPPG',
    'GPpEEEEEEEEEEpPG',
    'GPpEEEEEEEEEEpPG',
    'GPpEEEEEEEEEEpPG',
    'GPPpEEEEEEEEpPPG',
    'GGPpEEEEEEEEpPGG',
    'GGPPpEEEEEEpPPGG',
    'GGGPPppppppPPGGG',
    'GGGGGPPPPPPGGGGG',
    'GGGGGGGGGGGGGGGG',
    'GGGGGGGGGGGGGGGG',
  ],
};

// 像素风战斗背景:固定内部分辨率,CSS 拉伸铺满。
export function paintBackground(canvas) {
  const W = 480, H = 270;
  canvas.width = W; canvas.height = H;
  const ctx = canvas.getContext('2d');
  const rect = (c, x, y, w, h) => { ctx.fillStyle = c; ctx.fillRect(x, y, w, h); };

  // 天空色带
  rect('#9fd4ef', 0, 0, W, 60);
  rect('#aedcf3', 0, 60, W, 50);
  rect('#c2e6f6', 0, 110, W, 40);

  // 太阳
  rect('#fff3b0', 40, 26, 22, 22);
  rect('#ffe97a', 44, 30, 14, 14);

  // 像素云
  const cloud = (x, y, s) => {
    rect('#ffffff', x, y + 4 * s, 30 * s, 6 * s);
    rect('#ffffff', x + 6 * s, y, 16 * s, 8 * s);
    rect('#eef7fc', x + 2 * s, y + 8 * s, 26 * s, 2 * s);
  };
  cloud(120, 28, 1); cloud(300, 48, 1.4); cloud(410, 20, 0.8);

  // 远山
  ctx.fillStyle = '#8fa8c8';
  for (let i = 0; i < W; i += 4) {
    const h = 26 + 18 * Math.abs(Math.sin(i * 0.02)) + 8 * Math.abs(Math.sin(i * 0.053 + 2));
    ctx.fillRect(i, 150 - h, 4, h);
  }
  ctx.fillStyle = '#a9bdd6';
  for (let i = 0; i < W; i += 4) {
    const h = 16 + 12 * Math.abs(Math.sin(i * 0.03 + 5));
    ctx.fillRect(i, 150 - h, 4, h);
  }

  // 树林剪影(两层)
  const tree = (x, base, c) => {
    rect(c, x + 3, base - 4, 4, 4);
    for (let lvl = 0; lvl < 3; lvl++) {
      const w = 14 - lvl * 4, y = base - 8 - lvl * 6;
      rect(c, x + (10 - w) / 2, y, w, 6);
    }
  };
  for (let x = -6; x < W; x += 26) tree(x, 152, '#3e7a4c');
  for (let x = 8; x < W; x += 34) tree(x, 156, '#2f5f3b');

  // 草地色带
  rect('#7cb342', 0, 152, W, 40);
  rect('#66a234', 0, 192, W, 44);
  rect('#558b2f', 0, 236, W, 34);

  // 草丛与小花
  const tuft = (x, y, c) => { rect(c, x, y, 2, 4); rect(c, x + 3, y - 2, 2, 6); rect(c, x + 6, y, 2, 4); };
  for (let i = 0; i < 26; i++) {
    const x = (i * 97 + 13) % W, y = 160 + ((i * 53) % 96);
    tuft(x, y, i % 2 ? '#8fce5a' : '#4c7f28');
    if (i % 5 === 0) { rect('#ffffff', x + 12, y + 1, 2, 2); rect('#ffd24a', x + 12, y - 1, 2, 2); }
  }
}
