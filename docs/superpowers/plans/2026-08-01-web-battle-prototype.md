# Numeria Web 战斗原型 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 一个可在 iPad Safari 直接游玩的 HTML5 战斗原型,验证"咒语算式拖水晶 + 凑十破盾 + 零惩罚重试 + 英文语音"这套核心数学玩法好不好玩。

**Architecture:** 纯前端、零构建、零依赖。游戏逻辑(谜题生成、战斗状态机)是纯 JS 模块,用 Node 内置 test runner 做 TDD;UI 层(DOM + CSS 动画 + Pointer Events 拖拽)只做人工浏览器验证。语音用浏览器内置 Web Speech API。

**Tech Stack:** Vanilla ES Modules / CSS / HTML,`node --test`(Node ≥18 内置,无需安装任何包),Web Speech API(`speechSynthesis`)。

## Global Constraints

- **零依赖、零构建**:不引入任何 npm 包或打包器;`python3 -m http.server` 或任何静态服务器即可运行
- **零惩罚不变量**:答错谜题绝不扣玩家 HP,技能仍以 `basePower` 释放;每道题允许引导重试一次
- **数字范围**:所有数值(HP、伤害、题目)≤ 20;玩家/敌人 HP 恒为 10,用十格阵渲染
- **触屏优先**:拖拽用 Pointer Events(iPad Safari 兼容),按钮点击目标 ≥ 60px
- **语音文案**:英文,数字用英文单词读出("Three plus what makes seven?")
- **随机数可注入**:所有生成函数第一个参数是 `rng`(返回 [0,1) 的函数),测试用 `makeRng(seed)` 保证确定性
- 提交信息末尾带 `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>` 与 Claude-Session 链接(见会话规范)

## File Structure

```
~/Projects/numeria/               # git 仓库根(Task 1 初始化)
  package.json                    # {"type":"module"} 仅为 ES module 测试
  docs/…                          # 已有设计文档
  prototype/
    index.html                    # 战斗页入口
    css/battle.css                # 布局、十格阵、水晶、动画
    js/
      data.js                     # 数灵/技能/敌人数值定义
      puzzles.js                  # 谜题生成与校验(纯逻辑,TDD)
      battle.js                   # 战斗状态机(纯逻辑,TDD)
      speech.js                   # Web Speech 薄封装
      main.js                     # UI 控制器:DOM ↔ battle/puzzles
    tests/
      puzzles.test.js
      battle.test.js
```

---

### Task 1: 仓库初始化与测试跑道

**Files:**
- Create: `package.json`(仓库根)
- Create: `prototype/tests/smoke.test.js`(临时,Task 2 删除)
- Create: `.gitignore`

**Interfaces:**
- Produces: 可用的 `node --test prototype/tests/` 命令;git 仓库(main 分支)

- [ ] **Step 1: git init 并提交已有设计文档**

```bash
cd ~/Projects/numeria && git init -b main
printf '.DS_Store\nnode_modules/\n' > .gitignore
git add .gitignore docs/
git commit -m "docs: add Numeria game design doc (md + html)"
```

- [ ] **Step 2: 建 package.json 与冒烟测试**

`package.json`:
```json
{
  "name": "numeria",
  "private": true,
  "type": "module",
  "scripts": { "test": "node --test prototype/tests/" }
}
```

`prototype/tests/smoke.test.js`:
```js
import { test } from 'node:test';
import assert from 'node:assert/strict';

test('test runner works', () => {
  assert.equal(1 + 1, 2);
});
```

- [ ] **Step 3: 运行测试确认通过**

Run: `cd ~/Projects/numeria && npm test`
Expected: `pass 1`

- [ ] **Step 4: Commit**

```bash
git add package.json prototype/tests/smoke.test.js
git commit -m "chore: scaffold test runner (node --test, zero deps)"
```

---

### Task 2: puzzles.js — 数字英文单词 + 咒语算式谜题(TDD)

**Files:**
- Create: `prototype/js/puzzles.js`
- Create: `prototype/tests/puzzles.test.js`
- Delete: `prototype/tests/smoke.test.js`

**Interfaces:**
- Produces:
  - `makeRng(seed: number): () => number` — 确定性 LCG
  - `numberWord(n: number): string` — 0..20 → 'zero'..'twenty'
  - `generateFormulaPuzzle(rng, {max=10}): {a, missing, sum, candidates: number[], prompt: string}` — `a + □ = sum`,`candidates` 4 个含正确答案,`prompt` 形如 `"Three plus what makes seven?"`
  - `checkFormula(puzzle, answer: number): boolean`

- [ ] **Step 1: 写失败测试**

`prototype/tests/puzzles.test.js`:
```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { makeRng, numberWord, generateFormulaPuzzle, checkFormula } from '../js/puzzles.js';

test('makeRng is deterministic in [0,1)', () => {
  const a = makeRng(42), b = makeRng(42);
  for (let i = 0; i < 10; i++) {
    const v = a();
    assert.equal(v, b());
    assert.ok(v >= 0 && v < 1);
  }
});

test('numberWord covers 0..20', () => {
  assert.equal(numberWord(0), 'zero');
  assert.equal(numberWord(7), 'seven');
  assert.equal(numberWord(13), 'thirteen');
  assert.equal(numberWord(20), 'twenty');
});

test('formula puzzle is consistent and answerable', () => {
  for (let seed = 1; seed <= 50; seed++) {
    const p = generateFormulaPuzzle(makeRng(seed), { max: 10 });
    assert.equal(p.a + p.missing, p.sum);
    assert.ok(p.sum <= 10 && p.a >= 1 && p.missing >= 1);
    assert.equal(p.candidates.length, 4);
    assert.ok(p.candidates.includes(p.missing));
    assert.equal(new Set(p.candidates).size, 4);
    for (const c of p.candidates) assert.ok(c >= 0 && c <= 10);
    assert.ok(checkFormula(p, p.missing));
    for (const c of p.candidates) if (c !== p.missing) assert.ok(!checkFormula(p, c));
  }
});

test('formula prompt reads numbers as words', () => {
  const p = generateFormulaPuzzle(makeRng(7), { max: 10 });
  const cap = s => s[0].toUpperCase() + s.slice(1);
  assert.equal(p.prompt, `${cap(numberWord(p.a))} plus what makes ${numberWord(p.sum)}?`);
});
```

- [ ] **Step 2: 运行确认失败**

Run: `npm test`
Expected: FAIL,`Cannot find module … puzzles.js`

- [ ] **Step 3: 最小实现**

`prototype/js/puzzles.js`:
```js
export function makeRng(seed) {
  let s = seed >>> 0;
  return () => {
    s = (s * 1664525 + 1013904223) >>> 0;
    return s / 2 ** 32;
  };
}

const WORDS = ['zero','one','two','three','four','five','six','seven','eight','nine','ten',
  'eleven','twelve','thirteen','fourteen','fifteen','sixteen','seventeen','eighteen','nineteen','twenty'];

export function numberWord(n) { return WORDS[n]; }

const cap = s => s[0].toUpperCase() + s.slice(1);
const pick = (rng, lo, hi) => lo + Math.floor(rng() * (hi - lo + 1)); // inclusive

function shuffle(rng, arr) {
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
  return arr;
}

export function generateFormulaPuzzle(rng, { max = 10 } = {}) {
  const sum = pick(rng, 3, max);
  const a = pick(rng, 1, sum - 1);
  const missing = sum - a;
  const candidates = new Set([missing]);
  while (candidates.size < 4) {
    const offset = pick(rng, 1, 3) * (rng() < 0.5 ? -1 : 1);
    const c = missing + offset;
    if (c >= 0 && c <= max) candidates.add(c);
  }
  return {
    a, missing, sum,
    candidates: shuffle(rng, [...candidates]),
    prompt: `${cap(numberWord(a))} plus what makes ${numberWord(sum)}?`,
  };
}

export function checkFormula(puzzle, answer) { return answer === puzzle.missing; }
```

- [ ] **Step 4: 运行确认通过,删除冒烟测试**

Run: `npm test` → Expected: 全部 PASS
```bash
rm prototype/tests/smoke.test.js
```

- [ ] **Step 5: Commit**

```bash
git add -A prototype
git commit -m "feat: formula puzzle generator with deterministic rng and spoken prompts"
```

---

### Task 3: puzzles.js — 凑十谜题(TDD)

**Files:**
- Modify: `prototype/js/puzzles.js`(追加)
- Modify: `prototype/tests/puzzles.test.js`(追加)

**Interfaces:**
- Produces:
  - `generateMakeTenPuzzle(rng, {target=10, handSize=4}): {target, hand: number[], prompt: string}` — `hand` 中**恰好一对**数字和为 `target`
  - `checkMakeTen(puzzle, i: number, j: number): boolean` — 判断手牌下标 i、j 是否凑成 target
  - `findMakeTenPair(hand: number[], target: number): [number, number] | null` — 提示用

- [ ] **Step 1: 追加失败测试**

追加到 `prototype/tests/puzzles.test.js`:
```js
import { generateMakeTenPuzzle, checkMakeTen, findMakeTenPair } from '../js/puzzles.js';

test('make-ten puzzle has exactly one valid pair', () => {
  for (let seed = 1; seed <= 50; seed++) {
    const p = generateMakeTenPuzzle(makeRng(seed), { target: 10, handSize: 4 });
    assert.equal(p.hand.length, 4);
    let pairs = 0;
    for (let i = 0; i < 4; i++)
      for (let j = i + 1; j < 4; j++)
        if (p.hand[i] + p.hand[j] === 10) pairs++;
    assert.equal(pairs, 1, `seed ${seed} hand ${p.hand}`);
    const [i, j] = findMakeTenPair(p.hand, 10);
    assert.ok(checkMakeTen(p, i, j));
    assert.equal(p.prompt, 'Pick two crystals that make ten!');
  }
});

test('findMakeTenPair returns null when no pair', () => {
  assert.equal(findMakeTenPair([1, 2, 3, 4], 10), null);
});
```

- [ ] **Step 2: 运行确认失败**

Run: `npm test`
Expected: FAIL,`generateMakeTenPuzzle` 未导出

- [ ] **Step 3: 最小实现**

追加到 `prototype/js/puzzles.js`(`pick`/`shuffle`/`numberWord` 已在文件内):
```js
export function findMakeTenPair(hand, target) {
  for (let i = 0; i < hand.length; i++)
    for (let j = i + 1; j < hand.length; j++)
      if (hand[i] + hand[j] === target) return [i, j];
  return null;
}

export function generateMakeTenPuzzle(rng, { target = 10, handSize = 4 } = {}) {
  const a = pick(rng, 1, target - 1);
  const hand = [a, target - a];
  while (hand.length < handSize) {
    const d = pick(rng, 1, target - 1);
    // 干扰项不得与现有任何牌凑成 target,避免出现第二组解
    if (hand.every(h => h + d !== target)) hand.push(d);
  }
  return { target, hand: shuffle(rng, hand), prompt: `Pick two crystals that make ${numberWord(target)}!` };
}

export function checkMakeTen(puzzle, i, j) {
  return i !== j && puzzle.hand[i] + puzzle.hand[j] === puzzle.target;
}
```

- [ ] **Step 4: 运行确认通过**

Run: `npm test` → Expected: 全部 PASS

- [ ] **Step 5: Commit**

```bash
git add prototype/js/puzzles.js prototype/tests/puzzles.test.js
git commit -m "feat: make-ten puzzle with exactly-one-pair guarantee"
```

---

### Task 4: data.js + battle.js — 战斗状态机(TDD)

**Files:**
- Create: `prototype/js/data.js`
- Create: `prototype/js/battle.js`
- Create: `prototype/tests/battle.test.js`

**Interfaces:**
- Consumes: 无(纯数据与状态)
- Produces:
  - `data.js`:`PLAYER`(Addmander,maxHp 10,skills:`tackle`{cost 0, power 2, type 'basic'} 与 `flame-formula`{cost 3, power 5, basePower 2, type 'formula'})、`ENEMY`(Duplirock,maxHp 10,attackPower 2,shield 10)
  - `battle.js`:
    - `createBattle(playerDef, enemyDef): state` — state 含 `player{name,maxHp,hp,skills}`、`enemy{name,maxHp,hp,attackPower,shield,shielded,vulnerableTurns}`、`gems`、`maxGems`、`outcome`
    - `startPlayerTurn(state)` — +2 宝石,上限 `maxGems`(8)
    - `damageToEnemy(state, base): number` — 易伤 ×2;有盾减半(向下取整,至少 1)
    - `useSkill(state, skillId, {correct=true}): {dmg, powered}` — 扣宝石、按 correct 选 power/basePower、结算伤害与 outcome;宝石不足抛 `Error('not enough gems')`
    - `breakShield(state)` — `shielded=false`,`vulnerableTurns=2`
    - `enemyTurn(state): {dmg}` — 敌方攻击玩家、易伤回合数递减、结算 outcome

- [ ] **Step 1: 写失败测试**

`prototype/tests/battle.test.js`:
```js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { PLAYER, ENEMY } from '../js/data.js';
import { createBattle, startPlayerTurn, damageToEnemy, useSkill, breakShield, enemyTurn } from '../js/battle.js';

const fresh = () => createBattle(PLAYER, ENEMY);

test('initial state', () => {
  const s = fresh();
  assert.equal(s.player.hp, 10);
  assert.equal(s.enemy.hp, 10);
  assert.equal(s.gems, 2);
  assert.equal(s.enemy.shielded, true);
  assert.equal(s.outcome, null);
});

test('gems gain +2 capped at 8', () => {
  const s = fresh();
  s.gems = 7;
  startPlayerTurn(s);
  assert.equal(s.gems, 8);
});

test('shield halves damage (floor, min 1)', () => {
  const s = fresh();
  assert.equal(damageToEnemy(s, 5), 2);
  assert.equal(damageToEnemy(s, 1), 1);
});

test('vulnerability doubles damage and overrides shield state', () => {
  const s = fresh();
  breakShield(s);
  assert.equal(s.enemy.shielded, false);
  assert.equal(s.enemy.vulnerableTurns, 2);
  assert.equal(damageToEnemy(s, 5), 10);
});

test('formula skill: correct uses power, wrong uses basePower (zero punishment)', () => {
  const s1 = fresh();
  s1.gems = 3; s1.enemy.shielded = false;
  const r1 = useSkill(s1, 'flame-formula', { correct: true });
  assert.equal(r1.dmg, 5);
  assert.equal(r1.powered, true);
  assert.equal(s1.enemy.hp, 5);
  assert.equal(s1.gems, 0);

  const s2 = fresh();
  s2.gems = 3; s2.enemy.shielded = false;
  const hpBefore = s2.player.hp;
  const r2 = useSkill(s2, 'flame-formula', { correct: false });
  assert.equal(r2.dmg, 2);
  assert.equal(r2.powered, false);
  assert.equal(s2.player.hp, hpBefore); // 答错绝不扣玩家血
});

test('not enough gems throws', () => {
  const s = fresh();
  s.gems = 1;
  assert.throws(() => useSkill(s, 'flame-formula'), /not enough gems/);
});

test('enemy turn damages player and ticks vulnerability', () => {
  const s = fresh();
  breakShield(s);
  const { dmg } = enemyTurn(s);
  assert.equal(dmg, 2);
  assert.equal(s.player.hp, 8);
  assert.equal(s.enemy.vulnerableTurns, 1);
});

test('outcomes: win and lose', () => {
  const s = fresh();
  s.enemy.shielded = false; s.enemy.hp = 2;
  useSkill(s, 'tackle');
  assert.equal(s.outcome, 'win');

  const s2 = fresh();
  s2.player.hp = 2;
  enemyTurn(s2);
  assert.equal(s2.outcome, 'lose');
});
```

- [ ] **Step 2: 运行确认失败**

Run: `npm test`
Expected: FAIL,`Cannot find module … data.js`

- [ ] **Step 3: 最小实现**

`prototype/js/data.js`:
```js
export const PLAYER = {
  id: 'addmander', name: 'Addmander', emoji: '🦎', maxHp: 10,
  skills: [
    { id: 'tackle', name: 'Tackle', cost: 0, power: 2, type: 'basic' },
    { id: 'flame-formula', name: 'Flame Formula', cost: 3, power: 5, basePower: 2, type: 'formula' },
  ],
};

export const ENEMY = {
  id: 'duplirock', name: 'Duplirock', emoji: '🪨', maxHp: 10,
  attackPower: 2, shield: 10,
};
```

`prototype/js/battle.js`:
```js
export function createBattle(playerDef, enemyDef) {
  return {
    player: { ...playerDef, hp: playerDef.maxHp },
    enemy: { ...enemyDef, hp: enemyDef.maxHp, shielded: enemyDef.shield != null, vulnerableTurns: 0 },
    gems: 2, maxGems: 8,
    outcome: null,
  };
}

export function startPlayerTurn(state) {
  state.gems = Math.min(state.maxGems, state.gems + 2);
}

export function damageToEnemy(state, base) {
  if (state.enemy.vulnerableTurns > 0) return base * 2;
  if (state.enemy.shielded) return Math.max(1, Math.floor(base / 2));
  return base;
}

export function useSkill(state, skillId, { correct = true } = {}) {
  const skill = state.player.skills.find(s => s.id === skillId);
  if (state.gems < skill.cost) throw new Error('not enough gems');
  state.gems -= skill.cost;
  const powered = skill.type !== 'formula' || correct;
  const dmg = damageToEnemy(state, powered ? skill.power : skill.basePower);
  state.enemy.hp = Math.max(0, state.enemy.hp - dmg);
  if (state.enemy.hp === 0) state.outcome = 'win';
  return { dmg, powered };
}

export function breakShield(state) {
  state.enemy.shielded = false;
  state.enemy.vulnerableTurns = 2;
}

export function enemyTurn(state) {
  const dmg = state.enemy.attackPower;
  state.player.hp = Math.max(0, state.player.hp - dmg);
  if (state.enemy.vulnerableTurns > 0) state.enemy.vulnerableTurns--;
  if (state.player.hp === 0) state.outcome = 'lose';
  return { dmg };
}
```

- [ ] **Step 4: 运行确认通过**

Run: `npm test` → Expected: 全部 PASS

- [ ] **Step 5: Commit**

```bash
git add prototype/js/data.js prototype/js/battle.js prototype/tests/battle.test.js
git commit -m "feat: battle state machine with shield, vulnerability and zero-punishment skills"
```

---

### Task 5: 战斗场景静态 UI(index.html + battle.css)

**Files:**
- Create: `prototype/index.html`
- Create: `prototype/css/battle.css`

**Interfaces:**
- Produces: 后续任务依赖的 DOM id:`#enemy-sprite` `#enemy-hp` `#enemy-shield` `#player-sprite` `#player-hp` `#gem-count` `#actions` `#btn-tackle` `#btn-formula` `#btn-shield` `#overlay` `#battle-log`
- HP 十格阵渲染函数放在 Task 6 的 main.js;本任务先手写 10 个格子占位验证样式

- [ ] **Step 1: 写 index.html**

`prototype/index.html`:
```html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no">
<title>Numeria — Battle Prototype</title>
<link rel="stylesheet" href="css/battle.css">
</head>
<body>
<main class="battle">
  <section class="side enemy">
    <div class="plate">
      <span class="name">Duplirock</span>
      <div class="tenframe" id="enemy-hp"></div>
      <div class="shield" id="enemy-shield">🛡️ <b>10</b></div>
    </div>
    <div class="sprite" id="enemy-sprite">🪨</div>
  </section>

  <section class="side player">
    <div class="sprite" id="player-sprite">🦎</div>
    <div class="plate">
      <span class="name">Addmander</span>
      <div class="tenframe" id="player-hp"></div>
      <div class="gems">⚡ <b id="gem-count">2</b></div>
    </div>
  </section>

  <div id="battle-log" class="log">A wild Duplirock appeared!</div>

  <nav id="actions" class="actions">
    <button id="btn-tackle">👊 Tackle</button>
    <button id="btn-formula">🔥 Flame Formula <small>⚡×3</small></button>
    <button id="btn-shield">🛡️ Break Shield</button>
  </nav>

  <div id="overlay" class="overlay hidden"></div>
</main>
<script type="module" src="js/main.js"></script>
</body>
</html>
```

- [ ] **Step 2: 写 battle.css**

`prototype/css/battle.css`:
```css
* { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
html, body { height: 100%; overflow: hidden; }
body {
  font-family: -apple-system, "Segoe UI", sans-serif;
  background: linear-gradient(180deg, #a8d8ea 0%, #c8e6c9 55%, #7cb342 55.2%, #558b2f 100%);
  touch-action: manipulation; user-select: none; -webkit-user-select: none;
}
.battle { position: relative; height: 100%; max-width: 900px; margin: 0 auto; padding: 16px; display: flex; flex-direction: column; }

.side { display: flex; align-items: center; gap: 16px; }
.side.enemy { justify-content: flex-end; margin-top: 8px; }
.side.player { justify-content: flex-start; margin-top: auto; }

.sprite { font-size: 72px; line-height: 1; filter: drop-shadow(0 6px 4px rgba(0,0,0,.25)); }
.sprite.hit { animation: hit .4s; }
.sprite.attack { animation: lunge .35s; }
@keyframes hit { 25% { transform: translateX(-8px); filter: brightness(2); } 75% { transform: translateX(8px); } }
@keyframes lunge { 40% { transform: translate(24px, -12px) scale(1.08); } }

.plate { background: rgba(255,255,255,.92); border: 3px solid #33691e; border-radius: 14px; padding: 10px 14px; min-width: 210px; }
.plate .name { font-weight: 800; font-size: 17px; color: #263238; }

.tenframe { display: grid; grid-template-columns: repeat(5, 24px); grid-auto-rows: 24px; gap: 4px; margin: 8px 0 4px; }
.tenframe i { border: 2px solid #33691e; border-radius: 5px; background: #fff; transition: background .3s; }
.tenframe i.on { background: #66bb6a; }

.shield { margin-top: 4px; font-weight: 700; color: #1565c0; }
.shield.broken { display: none; }
.gems { margin-top: 4px; font-weight: 700; color: #ef6c00; font-size: 18px; }

.log { position: absolute; left: 50%; top: 44%; transform: translate(-50%, -50%);
  background: rgba(38,50,56,.88); color: #fff; padding: 10px 22px; border-radius: 999px;
  font-size: 17px; font-weight: 600; white-space: nowrap; }

.actions { display: flex; gap: 12px; margin-top: 14px; }
.actions button {
  flex: 1; min-height: 64px; font-size: 18px; font-weight: 800; border-radius: 16px;
  border: 3px solid #33691e; background: #fffde7; color: #263238; cursor: pointer;
}
.actions button:active { transform: scale(.96); }
.actions button:disabled { opacity: .4; }
.actions button:focus-visible { outline: 3px solid #ef6c00; outline-offset: 2px; }
.actions small { display: block; font-size: 12px; color: #ef6c00; }

/* ---- 谜题遮罩(Task 7 使用,先备好样式) ---- */
.overlay { position: absolute; inset: 0; background: rgba(20,30,40,.86); border-radius: 12px;
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 24px; z-index: 10; }
.overlay.hidden { display: none; }
.overlay .prompt { color: #fff; font-size: 26px; font-weight: 800; text-align: center; padding: 0 20px; }
.overlay .equation { display: flex; align-items: center; gap: 14px; font-size: 44px; font-weight: 900; color: #ffe082; }
.overlay .slot { width: 76px; height: 76px; border: 4px dashed #ffe082; border-radius: 16px;
  display: flex; align-items: center; justify-content: center; background: rgba(255,255,255,.08); }
.overlay .slot.filled { border-style: solid; background: rgba(255,224,130,.25); }
.crystals { display: flex; gap: 18px; }
.crystal { width: 72px; height: 72px; border-radius: 22px; display: flex; align-items: center; justify-content: center;
  font-size: 32px; font-weight: 900; color: #fff; touch-action: none; cursor: grab;
  background: radial-gradient(circle at 30% 25%, #7e57c2, #4527a0); border: 3px solid #b39ddb;
  box-shadow: 0 6px 14px rgba(0,0,0,.4); }
.crystal.selected { transform: scale(1.15); border-color: #ffe082; }
.crystal.dragging { position: fixed; z-index: 20; pointer-events: none; transform: scale(1.2); }
.crystal.correct { animation: burst .5s forwards; }
@keyframes burst { to { transform: scale(1.8); opacity: 0; } }
.hint { display: flex; flex-direction: column; align-items: center; gap: 10px; color: #fff; font-size: 16px; }
.hint .tenframe i { border-color: #ffe082; background: rgba(255,255,255,.12); }
.hint .tenframe i.on { background: #ffb300; }
.hint .tenframe i.need { background: rgba(102,187,106,.65); }

.banner { position: absolute; inset: 0; z-index: 30; display: flex; flex-direction: column;
  align-items: center; justify-content: center; gap: 14px; background: rgba(20,30,40,.92);
  color: #fff; font-size: 40px; font-weight: 900; border-radius: 12px; }
.banner button { font-size: 20px; padding: 14px 34px; border-radius: 14px; border: 3px solid #ffe082;
  background: #ef6c00; color: #fff; font-weight: 800; min-height: 60px; }

@media (prefers-reduced-motion: reduce) { * { animation: none !important; transition: none !important; } }
```

- [ ] **Step 3: 浏览器人工验证**

Run: `cd ~/Projects/numeria/prototype && python3 -m http.server 8765` 后打开 `http://localhost:8765`
Expected: 敌我双方布局正确(敌上右、我下左),名牌、盾牌标志、宝石计数、三个大按钮可见;此时十格阵为空(main.js 未建)属正常;无横向滚动

- [ ] **Step 4: Commit**

```bash
git add prototype/index.html prototype/css/battle.css
git commit -m "feat: battle scene static UI with ten-frame plates and puzzle overlay styles"
```

---

### Task 6: main.js — 回合流转 + 普攻 + 敌方回合动画

**Files:**
- Create: `prototype/js/main.js`

**Interfaces:**
- Consumes: `battle.js` 全部导出、`data.js` 的 `PLAYER`/`ENEMY`、Task 5 的 DOM id
- Produces: `renderAll()`、`setLog(text)`、`sleep(ms)`、`endPlayerTurn()` —— Task 7/8 在此文件内继续扩展

- [ ] **Step 1: 写实现**

`prototype/js/main.js`:
```js
import { PLAYER, ENEMY } from './data.js';
import { createBattle, startPlayerTurn, useSkill, breakShield, enemyTurn } from './battle.js';

const $ = id => document.getElementById(id);
export const sleep = ms => new Promise(r => setTimeout(r, ms));

const state = createBattle(PLAYER, ENEMY);

function renderTenframe(el, hp, maxHp) {
  el.innerHTML = '';
  for (let i = 0; i < maxHp; i++) {
    const cell = document.createElement('i');
    if (i < hp) cell.classList.add('on');
    el.appendChild(cell);
  }
}

export function renderAll() {
  renderTenframe($('player-hp'), state.player.hp, state.player.maxHp);
  renderTenframe($('enemy-hp'), state.enemy.hp, state.enemy.maxHp);
  $('gem-count').textContent = state.gems;
  $('enemy-shield').classList.toggle('broken', !state.enemy.shielded);
  const formulaSkill = state.player.skills.find(s => s.id === 'flame-formula');
  $('btn-formula').disabled = state.gems < formulaSkill.cost;
  $('btn-shield').disabled = !state.enemy.shielded;
}

export function setLog(text) { $('battle-log').textContent = text; }

function setActionsEnabled(on) {
  for (const b of $('actions').querySelectorAll('button')) b.disabled = !on;
  if (on) renderAll(); // 恢复宝石/护盾相关的禁用逻辑
}

async function animate(el, cls) {
  el.classList.remove(cls);
  void el.offsetWidth; // 强制 reflow 以便动画可重复触发
  el.classList.add(cls);
  await sleep(450);
  el.classList.remove(cls);
}

export async function endPlayerTurn() {
  if (state.outcome) return showOutcome();
  await sleep(600);
  setLog(`${state.enemy.name} attacks!`);
  await animate($('enemy-sprite'), 'attack');
  const { dmg } = enemyTurn(state);
  await animate($('player-sprite'), 'hit');
  renderAll();
  setLog(`${state.player.name} took ${dmg} damage!`);
  if (state.outcome) return showOutcome();
  await sleep(500);
  startPlayerTurn(state);
  renderAll();
  setLog('Your turn! +2 ⚡');
  setActionsEnabled(true);
}

export function showOutcome() {
  const banner = document.createElement('div');
  banner.className = 'banner';
  banner.innerHTML = state.outcome === 'win'
    ? '<div>🎉 YOU WIN!</div><button id="btn-again">Play Again</button>'
    : '<div>💤 Addmander fainted…</div><button id="btn-again">Try Again</button>';
  document.querySelector('.battle').appendChild(banner);
  banner.querySelector('#btn-again').addEventListener('click', () => location.reload());
}

$('btn-tackle').addEventListener('click', async () => {
  setActionsEnabled(false);
  await animate($('player-sprite'), 'attack');
  const { dmg } = useSkill(state, 'tackle');
  await animate($('enemy-sprite'), 'hit');
  renderAll();
  setLog(`Tackle hits for ${dmg}!`);
  await endPlayerTurn();
});

// Task 7 接管 btn-formula,Task 8 接管 btn-shield
export { state, $, animate, setActionsEnabled };

renderAll();
```

- [ ] **Step 2: 浏览器人工验证**

Run: 静态服务器打开页面,反复点 Tackle
Expected: 我方冲撞动画 → 敌方受击闪白 → 敌方十格阵掉格 → 敌方反击 → 我方掉格 → 宝石 +2;有盾时 Tackle 伤害减半(掉 1 格);打空敌方十格阵出现 WIN 横幅,Play Again 可重开;把自己血耗尽出现 fainted 横幅

- [ ] **Step 3: 回归测试**

Run: `npm test` → Expected: 全部 PASS

- [ ] **Step 4: Commit**

```bash
git add prototype/js/main.js
git commit -m "feat: battle UI loop with tackle, enemy turn and win/lose banners"
```

---

### Task 7: 咒语算式遮罩 — 拖拽水晶 + 零惩罚重试

**Files:**
- Modify: `prototype/js/main.js`(追加谜题遮罩逻辑并接管 `#btn-formula`)

**Interfaces:**
- Consumes: `puzzles.js` 的 `generateFormulaPuzzle`/`checkFormula`/`makeRng`;main.js 已有的 `state` `$` `animate` `setLog` `setActionsEnabled` `endPlayerTurn` `renderAll` `sleep`
- Produces: `runFormulaPuzzle(): Promise<boolean>` — 弹出遮罩,resolve 是否最终答对(含一次重试);`speak(text)` 占位(Task 9 实现真语音,本任务先定义空函数避免报错)

**交互约定(5 岁触屏)**:拖拽水晶到空格 **或** 点水晶再点空格,两种都支持;答错 → 水晶抖动 + 十格阵提示(a 个亮块、还需的格子闪绿)+ 语音重读 → 允许再选一次;第二次也错 → resolve(false)(技能以 basePower 释放,绝不扣玩家血)

- [ ] **Step 1: 追加实现**

追加到 `prototype/js/main.js`(`speak` 先为占位):
```js
import { generateFormulaPuzzle, checkFormula, makeRng } from './puzzles.js';

export let speak = text => {}; // Task 9 替换为 Web Speech
const rng = makeRng(Date.now() % 2 ** 31);

function buildHintTenframe(a, sum) {
  const tf = document.createElement('div');
  tf.className = 'tenframe';
  for (let i = 0; i < 10; i++) {
    const cell = document.createElement('i');
    if (i < a) cell.classList.add('on');
    else if (i < sum) cell.classList.add('need');
    tf.appendChild(cell);
  }
  return tf;
}

export function runFormulaPuzzle() {
  return new Promise(resolve => {
    const p = generateFormulaPuzzle(rng, { max: 10 });
    let attempts = 0;
    const overlay = $('overlay');
    overlay.classList.remove('hidden');
    overlay.innerHTML = `
      <div class="prompt">${p.prompt}</div>
      <div class="equation"><span>${p.a}</span><span>+</span>
        <span class="slot" id="slot"></span><span>=</span><span>${p.sum}</span></div>
      <div class="crystals" id="crystals"></div>
      <div class="hint" id="hint"></div>`;
    speak(p.prompt);

    const slot = overlay.querySelector('#slot');
    const tray = overlay.querySelector('#crystals');

    function finish(correct) {
      setTimeout(() => { overlay.classList.add('hidden'); overlay.innerHTML = ''; resolve(correct); }, correct ? 600 : 400);
    }

    function submit(value, crystalEl) {
      attempts++;
      slot.textContent = value;
      slot.classList.add('filled');
      if (checkFormula(p, value)) {
        crystalEl.classList.add('correct');
        speak('Great job!');
        finish(true);
      } else if (attempts === 1) {
        // 零惩罚引导重试:十格阵提示 + 重读题目
        slot.textContent = ''; slot.classList.remove('filled');
        crystalEl.remove();
        const hint = overlay.querySelector('#hint');
        hint.innerHTML = '<span>Let\'s count together!</span>';
        hint.appendChild(buildHintTenframe(p.a, p.sum));
        speak(`Hmm, not quite. ${p.prompt}`);
      } else {
        speak('Nice try! Your move still works!');
        finish(false);
      }
    }

    for (const value of p.candidates) {
      const c = document.createElement('div');
      c.className = 'crystal';
      c.textContent = value;
      tray.appendChild(c);

      // 方式一:点选即提交(最低门槛)
      c.addEventListener('click', () => submit(value, c));

      // 方式二:Pointer 拖拽到空格
      c.addEventListener('pointerdown', e => {
        c.setPointerCapture(e.pointerId);
        let moved = false;
        const move = ev => {
          moved = true;
          c.classList.add('dragging');
          c.style.left = ev.clientX - 36 + 'px';
          c.style.top = ev.clientY - 36 + 'px';
        };
        const up = ev => {
          c.removeEventListener('pointermove', move);
          c.removeEventListener('pointerup', up);
          if (!moved) return; // 纯点击交给 click 处理
          c.classList.remove('dragging');
          c.style.left = c.style.top = '';
          const r = slot.getBoundingClientRect();
          if (ev.clientX >= r.left && ev.clientX <= r.right && ev.clientY >= r.top && ev.clientY <= r.bottom) {
            submit(value, c);
          }
        };
        c.addEventListener('pointermove', move);
        c.addEventListener('pointerup', up);
      });
    }
  });
}

$('btn-formula').addEventListener('click', async () => {
  setActionsEnabled(false);
  const correct = await runFormulaPuzzle();
  await animate($('player-sprite'), 'attack');
  const { dmg, powered } = useSkill(state, 'flame-formula', { correct });
  await animate($('enemy-sprite'), 'hit');
  renderAll();
  setLog(powered ? `🔥 Flame Formula! ${dmg} damage!` : `Flame fizzles… still ${dmg} damage!`);
  await endPlayerTurn();
});
```

注意:Task 6 中 `btn-formula` 没有注册过监听器,此处是唯一接管点。`Date.now()` 作种子在浏览器端使用(测试仍用固定种子)。

- [ ] **Step 2: 浏览器人工验证**

Expected:
1. 宝石 ≥3 时点 Flame Formula → 遮罩浮现算式与 4 颗水晶
2. 拖水晶到空格 与 直接点水晶 都能作答
3. 答对 → 水晶爆裂动画 → 大伤害(有盾减半);答错一次 → 十格阵提示出现(前 a 格绿、到 sum 闪橙)可再试;两次都错 → 技能仍打出 basePower 伤害,玩家 HP 不因答错变化
4. 宝石 <3 时按钮禁用

- [ ] **Step 3: 回归测试**

Run: `npm test` → Expected: 全部 PASS

- [ ] **Step 4: Commit**

```bash
git add prototype/js/main.js
git commit -m "feat: spell-formula overlay with crystal drag, tap fallback and zero-punishment retry"
```

---

### Task 8: 凑十破盾遮罩

**Files:**
- Modify: `prototype/js/main.js`(追加并接管 `#btn-shield`)

**Interfaces:**
- Consumes: `puzzles.js` 的 `generateMakeTenPuzzle`/`checkMakeTen`/`findMakeTenPair`;`battle.js` 的 `breakShield`
- Produces: `runMakeTenPuzzle(): Promise<boolean>` — 选两颗水晶凑 10;答错一次给提示(正确对之一高亮脉动)再试;两次失败 resolve(false)(盾不破,不受罚,回合照常)

- [ ] **Step 1: 追加实现**

追加到 `prototype/js/main.js`:
```js
import { generateMakeTenPuzzle, checkMakeTen, findMakeTenPair } from './puzzles.js';

export function runMakeTenPuzzle() {
  return new Promise(resolve => {
    const p = generateMakeTenPuzzle(rng, { target: 10, handSize: 4 });
    let attempts = 0;
    let picked = []; // 选中的下标
    const overlay = $('overlay');
    overlay.classList.remove('hidden');
    overlay.innerHTML = `
      <div class="prompt">${p.prompt}</div>
      <div class="equation"><span class="slot" id="pick-a"></span><span>+</span>
        <span class="slot" id="pick-b"></span><span>=</span><span>${p.target}</span></div>
      <div class="crystals" id="crystals"></div>`;
    speak(p.prompt);
    const tray = overlay.querySelector('#crystals');

    function finish(ok) {
      setTimeout(() => { overlay.classList.add('hidden'); overlay.innerHTML = ''; resolve(ok); }, ok ? 600 : 400);
    }

    function refreshSlots() {
      overlay.querySelector('#pick-a').textContent = picked[0] != null ? p.hand[picked[0]] : '';
      overlay.querySelector('#pick-b').textContent = picked[1] != null ? p.hand[picked[1]] : '';
    }

    function judge() {
      attempts++;
      const [i, j] = picked;
      if (checkMakeTen(p, i, j)) {
        speak('Shield break!');
        for (const el of tray.children) el.classList.add('correct');
        finish(true);
      } else if (attempts === 1) {
        speak(`Hmm, ${p.hand[i]} plus ${p.hand[j]} is not ten. Try again!`);
        const [hi] = findMakeTenPair(p.hand, p.target);
        tray.children[hi].classList.add('selected'); // 提示:高亮正确对的其中一颗
        picked = [];
        for (const el of tray.children) el.classList.remove('picked');
        refreshSlots();
      } else {
        speak('Nice try! The shield holds for now.');
        finish(false);
      }
    }

    p.hand.forEach((value, idx) => {
      const c = document.createElement('div');
      c.className = 'crystal';
      c.textContent = value;
      c.addEventListener('click', () => {
        if (picked.includes(idx)) return;
        picked.push(idx);
        c.classList.add('picked', 'selected');
        refreshSlots();
        if (picked.length === 2) setTimeout(judge, 350);
      });
      tray.appendChild(c);
    });
  });
}

$('btn-shield').addEventListener('click', async () => {
  setActionsEnabled(false);
  const ok = await runMakeTenPuzzle();
  if (ok) {
    breakShield(state);
    renderAll();
    setLog('💥 Shield shattered! Double damage for 2 turns!');
    await animate($('enemy-sprite'), 'hit');
  } else {
    setLog('The shield holds…');
  }
  await endPlayerTurn();
});
```

- [ ] **Step 2: 浏览器人工验证**

Expected:
1. 点 Break Shield → 出现目标 10 与 4 颗水晶,依次点两颗填入两个空格
2. 凑对 → 全体水晶爆裂,盾图标消失,之后两回合我方伤害翻倍(Tackle 打 4)
3. 凑错一次 → 语音鼓励 + 一颗正确水晶高亮,可重选;再错 → 盾保持,回合继续,玩家无损失
4. 两回合后易伤消失(伤害恢复正常)

- [ ] **Step 3: 回归测试 + Commit**

Run: `npm test` → PASS
```bash
git add prototype/js/main.js
git commit -m "feat: make-ten shield break overlay with guided retry"
```

---

### Task 9: speech.js — 英文语音旁白

**Files:**
- Create: `prototype/js/speech.js`
- Modify: `prototype/js/main.js`(把占位 `speak` 换成真实现)

**Interfaces:**
- Produces: `createSpeaker(): (text: string) => void` — 使用 `speechSynthesis`,en-US、rate 0.85;不支持时返回 no-op;每次发声前 `cancel()` 避免排队堆积

- [ ] **Step 1: 写 speech.js**

`prototype/js/speech.js`:
```js
export function createSpeaker() {
  if (typeof speechSynthesis === 'undefined') return () => {};
  return text => {
    speechSynthesis.cancel();
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'en-US';
    u.rate = 0.85;
    u.pitch = 1.1;
    speechSynthesis.speak(u);
  };
}
```

- [ ] **Step 2: 接入 main.js**

把 Task 7 中的占位:
```js
export let speak = text => {}; // Task 9 替换为 Web Speech
```
替换为:
```js
import { createSpeaker } from './speech.js';
export const speak = createSpeaker();
```

- [ ] **Step 3: 浏览器人工验证**

Expected: 弹谜题时朗读题目("Three plus what makes seven?");答对读 "Great job!";重试时重读题目;iPad Safari 实机(或 Mac Safari 响应式模式)出声。注:iOS 要求首次用户手势后才允许发声——本原型所有语音都发生在点击之后,天然满足

- [ ] **Step 4: 回归测试 + Commit**

Run: `npm test` → PASS
```bash
git add prototype/js/speech.js prototype/js/main.js
git commit -m "feat: english voice narration via Web Speech API"
```

---

### Task 10: 收尾 — 开场白、胜利经验值演出、iPad 实测清单

**Files:**
- Modify: `prototype/js/main.js`(胜利演出 + 开场语音)
- Modify: `prototype/index.html`(标题微调可选)

**Interfaces:**
- Consumes: 前述全部
- Produces: 完整可玩原型 + README 运行说明

- [ ] **Step 1: 胜利演出加经验数字**

把 Task 6 的 `showOutcome()` 中 win 分支:
```js
    ? '<div>🎉 YOU WIN!</div><button id="btn-again">Play Again</button>'
```
替换为:
```js
    ? '<div>🎉 YOU WIN!</div><div style="font-size:22px">Addmander got <b style="color:#ffe082">+5 XP</b>!</div><button id="btn-again">Play Again</button>'
```
并在 `showOutcome` 开头加:
```js
  speak(state.outcome === 'win' ? 'You win! Addmander got five experience points!' : 'Oh no! Let\'s try again!');
```

- [ ] **Step 2: 开场旁白**

`main.js` 末尾 `renderAll();` 后追加:
```js
setTimeout(() => speak('A wild Duplirock appeared! It has a number shield!'), 400);
```
(若 iOS 因无手势拦下这句,静默失败即可,不影响游戏。)

- [ ] **Step 3: 写 README**

`prototype/README.md`:
```markdown
# Numeria Battle Prototype

在 iPad Safari 里验证核心数学玩法的一次性原型。

## 运行

​```bash
cd prototype && python3 -m http.server 8765
​```

Mac 与 iPad 同一 Wi-Fi 下,iPad Safari 访问 `http://<Mac的IP>:8765`。

## 测试

​```bash
npm test   # 仓库根目录,Node ≥ 18
​```
```

- [ ] **Step 4: iPad 实测清单(人工)**

- 横屏布局不破,无横向滚动
- 拖拽水晶流畅跟手;点选回退可用
- 语音在触摸后正常发声
- 5 岁实测:能否独立完成一场战斗(观察记录,不打断)

- [ ] **Step 5: 回归测试 + Commit**

Run: `npm test` → PASS
```bash
git add -A prototype
git commit -m "feat: victory XP flourish, opening narration and prototype README"
```

---

## Self-Review 记录

- **Spec 覆盖**:咒语算式拖水晶(T7)、凑十破盾(T8)、能量宝石(T4/T6)、零惩罚重试(T4 测试 + T7/T8)、十格阵 HP(T5/T6)、英文语音(T9)、iPad 触屏(T5/T7/T10)——原型验证目标全覆盖。进化/宝箱/地图/收服不在本原型范围(见设计文档路线图)。
- **占位符**:无 TBD;T7 的 `speak` 占位是显式接口,T9 完成替换。
- **类型一致性**:`useSkill(state, skillId, {correct})` 的返回 `{dmg, powered}` 在 T4 定义、T6/T7 消费一致;`runFormulaPuzzle`/`runMakeTenPuzzle` 均 resolve boolean。
