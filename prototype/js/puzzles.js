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
