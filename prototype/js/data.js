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
