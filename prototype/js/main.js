import { PLAYER, ENEMY } from './data.js';
import { createBattle, startPlayerTurn, useSkill, breakShield, enemyTurn } from './battle.js';
import { generateFormulaPuzzle, checkFormula, makeRng,
  generateMakeTenPuzzle, checkMakeTen, findMakeTenPair } from './puzzles.js';
import { drawSprite, paintBackground, ADDMANDER, DUPLIROCK, GEM, SHIELD } from './sprites.js';

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

function popDamage(targetEl, text, color) {
  const r = targetEl.getBoundingClientRect();
  const d = document.createElement('div');
  d.className = 'dmg-pop';
  d.textContent = text;
  d.style.color = color;
  d.style.left = r.left + r.width / 2 - 14 + 'px';
  d.style.top = r.top - 6 + 'px';
  document.body.appendChild(d);
  setTimeout(() => d.remove(), 850);
}

function shakeScreen() {
  const b = document.querySelector('.battle');
  b.classList.remove('shake');
  void b.offsetWidth;
  b.classList.add('shake');
}

async function throwProjectile(fromEl, toEl, glyph) {
  const a = fromEl.getBoundingClientRect(), b = toEl.getBoundingClientRect();
  const p = document.createElement('div');
  p.className = 'projectile';
  p.textContent = glyph;
  p.style.left = a.left + a.width / 2 + 'px';
  p.style.top = a.top + 'px';
  document.body.appendChild(p);
  void p.offsetWidth;
  p.style.left = b.left + b.width / 2 - 20 + 'px';
  p.style.top = b.top + b.height / 2 - 20 + 'px';
  await sleep(400);
  p.remove();
}

async function shatterShield() {
  const sh = $('enemy-shield');
  const r = sh.getBoundingClientRect();
  sh.classList.add('shatter');
  for (let i = 0; i < 6; i++) {
    const s = document.createElement('div');
    s.className = 'shard';
    s.style.left = r.left + r.width / 2 + 'px';
    s.style.top = r.top + 'px';
    s.style.setProperty('--dx', (i - 2.5) * 26 + 'px');
    document.body.appendChild(s);
    setTimeout(() => s.remove(), 650);
  }
  await sleep(500);
  sh.classList.remove('shatter');
}

export async function endPlayerTurn() {
  if (state.outcome) return showOutcome();
  await sleep(600);
  const turn = enemyTurn(state);
  if (turn.skipped) {
    speak('Shield stun! The enemy misses a turn!');
    setLog('💫 Shield stun! Enemy misses a turn!');
    await sleep(900);
    startPlayerTurn(state);
    renderAll();
    setLog('Your turn! +2 ⚡ — double hit ready!');
    setActionsEnabled(true);
    return;
  }
  setLog(`${state.enemy.name} attacks!`);
  await animate($('enemy-sprite'), 'attack');
  const { dmg } = turn;
  popDamage($('player-sprite'), `-${dmg}`, '#ff6b6b');
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
  speak(state.outcome === 'win' ? 'You win! Addmander got five experience points!' : "Oh no! Let's try again!");
  const banner = document.createElement('div');
  banner.className = 'banner';
  banner.innerHTML = state.outcome === 'win'
    ? '<div>🎉 YOU WIN!</div><div style="font-size:22px">Addmander got <b style="color:#ffe082">+5 XP</b>!</div><button id="btn-again">Play Again</button>'
    : '<div>💤 Addmander fainted…</div><button id="btn-again">Try Again</button>';
  document.querySelector('.battle').appendChild(banner);
  banner.querySelector('#btn-again').addEventListener('click', () => location.reload());
}

$('btn-tackle').addEventListener('click', async () => {
  setActionsEnabled(false);
  await animate($('player-sprite'), 'attack');
  const { dmg } = useSkill(state, 'tackle');
  popDamage($('enemy-sprite'), `-${dmg}`, '#ffd24a');
  await animate($('enemy-sprite'), 'hit');
  renderAll();
  setLog(`Tackle hits for ${dmg}!`);
  await endPlayerTurn();
});

import { createSpeaker } from './speech.js';
export const speak = createSpeaker();
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

      // 点按即提交;超过 8px 阈值才进入拖拽,松手落在空格内也提交。
      // 手指点按常带 1-2px 抖动,没有阈值会吞掉点击。
      c.addEventListener('pointerdown', e => {
        c.setPointerCapture(e.pointerId);
        const startX = e.clientX, startY = e.clientY;
        let moved = false;
        const move = ev => {
          if (!moved && Math.hypot(ev.clientX - startX, ev.clientY - startY) < 8) return;
          moved = true;
          c.classList.add('dragging');
          c.style.left = ev.clientX - 36 + 'px';
          c.style.top = ev.clientY - 36 + 'px';
        };
        const up = ev => {
          c.removeEventListener('pointermove', move);
          c.removeEventListener('pointerup', up);
          c.removeEventListener('pointercancel', up);
          c.classList.remove('dragging');
          c.style.left = c.style.top = '';
          if (ev.type === 'pointercancel') return;
          if (!moved) { submit(value, c); return; }
          const r = slot.getBoundingClientRect();
          if (ev.clientX >= r.left && ev.clientX <= r.right && ev.clientY >= r.top && ev.clientY <= r.bottom) {
            submit(value, c);
          }
        };
        c.addEventListener('pointermove', move);
        c.addEventListener('pointerup', up);
        c.addEventListener('pointercancel', up);
      });
    }
  });
}

$('btn-formula').addEventListener('click', async () => {
  setActionsEnabled(false);
  const correct = await runFormulaPuzzle();
  await animate($('player-sprite'), 'attack');
  await throwProjectile($('player-sprite'), $('enemy-sprite'), correct ? '🔥' : '✨');
  const { dmg, powered } = useSkill(state, 'flame-formula', { correct });
  popDamage($('enemy-sprite'), `-${dmg}`, powered ? '#ff9d3a' : '#ffd24a');
  if (powered) shakeScreen();
  await animate($('enemy-sprite'), 'hit');
  renderAll();
  setLog(powered ? `🔥 Flame Formula! ${dmg} damage!` : `Flame fizzles… still ${dmg} damage!`);
  await endPlayerTurn();
});

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
        picked = [];
        for (const el of tray.children) el.classList.remove('picked', 'selected');
        const [hi] = findMakeTenPair(p.hand, p.target);
        tray.children[hi].classList.add('selected'); // 提示:高亮正确对的其中一颗
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
    await shatterShield();
    breakShield(state);
    renderAll();
    shakeScreen();
    setLog('💥 Shield shattered! Enemy stunned — next hit deals double damage!');
    await animate($('enemy-sprite'), 'hit');
  } else {
    setLog('The shield holds…');
  }
  await endPlayerTurn();
});

export { state, $, animate, setActionsEnabled };

paintBackground($('bg'));
drawSprite($('player-sprite'), ADDMANDER);
drawSprite($('enemy-sprite'), DUPLIROCK);
drawSprite($('icon-gem'), GEM);
drawSprite($('icon-shield'), SHIELD);

renderAll();
setTimeout(() => speak('A wild Duplirock appeared! It has a number shield!'), 400);
