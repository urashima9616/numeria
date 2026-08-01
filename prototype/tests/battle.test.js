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
