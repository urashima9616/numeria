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
