import { test } from 'node:test';
import assert from 'node:assert/strict';
import { makeRng, numberWord, generateFormulaPuzzle, checkFormula,
  generateMakeTenPuzzle, checkMakeTen, findMakeTenPair } from '../js/puzzles.js';

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

test('formula prompt reads numbers as words', () => {
  const p = generateFormulaPuzzle(makeRng(7), { max: 10 });
  const cap = s => s[0].toUpperCase() + s.slice(1);
  assert.equal(p.prompt, `${cap(numberWord(p.a))} plus what makes ${numberWord(p.sum)}?`);
});
