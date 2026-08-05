export function createBattle(playerDef, enemyDef) {
  return {
    player: { ...playerDef, hp: playerDef.maxHp },
    enemy: { ...enemyDef, hp: enemyDef.maxHp, shielded: enemyDef.shield != null,
      breakBonusReady: false, skipTurns: 0 },
    gems: 2, maxGems: 8,
    outcome: null,
  };
}

export function startPlayerTurn(state) {
  state.gems = Math.min(state.maxGems, state.gems + 2);
}

export function damageToEnemy(state, base) {
  if (state.enemy.shielded) return Math.max(1, Math.floor(base / 2));
  if (state.enemy.breakBonusReady) return base * 2;
  return base;
}

export function useSkill(state, skillId, { correct = true } = {}) {
  const skill = state.player.skills.find(s => s.id === skillId);
  if (state.gems < skill.cost) throw new Error('not enough gems');
  state.gems -= skill.cost;
  const powered = skill.type !== 'formula' || correct;
  const breakBonusApplied = state.enemy.shield != null &&
    !state.enemy.shielded && state.enemy.breakBonusReady;
  const dmg = damageToEnemy(state, powered ? skill.power : skill.basePower);
  state.enemy.hp = Math.max(0, state.enemy.hp - dmg);
  if (breakBonusApplied) {
    state.enemy.breakBonusReady = false;
    state.enemy.shielded = state.enemy.hp > 0;
  }
  if (state.enemy.hp === 0) state.outcome = 'win';
  return { dmg, powered, breakBonusApplied };
}

export function breakShield(state) {
  if (state.enemy.shield == null || !state.enemy.shielded) return;
  state.enemy.shielded = false;
  state.enemy.breakBonusReady = true;
  state.enemy.skipTurns = 1;
}

export function enemyTurn(state) {
  if (state.enemy.skipTurns > 0) {
    state.enemy.skipTurns--;
    return { dmg: 0, skipped: true };
  }
  const dmg = state.enemy.attackPower;
  state.player.hp = Math.max(0, state.player.hp - dmg);
  if (state.player.hp === 0) state.outcome = 'lose';
  return { dmg, skipped: false };
}
