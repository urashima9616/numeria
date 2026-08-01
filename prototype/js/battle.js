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
