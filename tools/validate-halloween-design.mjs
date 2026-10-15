// Design-only consistency checks. Does not import content into Unity or read player saves.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const gameData = read('unity/Assets/Scripts/Core/GameData.cs');
const depthData = read('unity/Assets/Scripts/Core/DepthCatalog.cs');
const design = read('docs/halloween-expansion-design.md');
const retrofitDesign = read('docs/depth-evolution-design.md');

function familyRows(markdown) {
  let section = '';
  return markdown.split('\n').flatMap(line => {
    if (/^#{2,3} /.test(line)) section = line.replace(/^#+ /, '');
    if (!/^\| [A-Z][a-z]+ \| [A-Z][a-z]+ \| [A-Z][a-z]+ \|/.test(line)) return [];
    const cells = line.split('|').slice(1, -1).map(cell => cell.trim());
    assert.equal(cells.length, 6, `Malformed family row: ${line}`);
    assert.ok(cells.every(Boolean), `Empty family field: ${line}`);
    return [{ section, names: cells.slice(0, 3), detail: cells.slice(3) }];
  });
}

const originalLines = [...gameData.matchAll(
  /Line\("([a-z]+)"[\s\S]*?new\[\] \{ ((?:"[a-z]+"(?:, )?)+) \}, new\[\] \{[^}]*\}\)/g
)].map(match => ({ base: match[1], ids: [...match[2].matchAll(/"([a-z]+)"/g)].map(m => m[1]) }));
assert.equal(originalLines.length, 54, 'Runtime baseline changed; re-audit this design.');
const originalForms = originalLines.flatMap(line => line.ids);
assert.equal(originalForms.length, 159);
assert.deepEqual(originalLines.filter(line => line.ids.length === 2).map(line => line.base),
  ['countipillar', 'doublit', 'mirrowl']);

const worlds = [...depthData.matchAll(
  /new DepthWorld \{ ParentId="[^"]+",Id="[^"]+",Name="([^"]+)",Level=(\d+)[\s\S]*?Species=new\[\] \{([\s\S]*?)\n\s*\}\}/g
)].map(match => ({
  name: match[1], level: Number(match[2]),
  species: [...match[3].matchAll(/Mon\("([A-Za-z]+)","([A-Z]+)","[^"]*"(,true)?\)/g)]
    .map(mon => ({ name: mon[1], element: mon[2], giant: Boolean(mon[3]) })),
}));
assert.equal(worlds.length, 8);
assert.ok(worlds.every(world => world.species.length === 11));
assert.equal(worlds.flatMap(world => world.species).filter(mon => mon.giant).length, 8);

const retrofits = familyRows(retrofitDesign);
assert.equal(retrofits.length, 88);
for (const world of worlds) {
  const rows = retrofits.filter(row => row.section === world.name);
  assert.deepEqual(rows.map(row => row.names[0]), world.species.map(mon => mon.name),
    `Missing, reordered or extra family in ${world.name}`);
  rows.forEach((row, index) => assert.equal(row.detail[0], world.species[index].element));
  const expectedThreshold = `| ${world.name} | ${world.level} / ${world.level + 8} | ${world.level + 4} / ${world.level + 10} |`;
  assert.ok(retrofitDesign.includes(expectedThreshold), `Incorrect thresholds: ${world.name}`);
  assert.ok(world.level + 10 <= 99, 'Evolution must remain reachable at max level.');
}

const newFamilies = familyRows(design);
assert.equal(newFamilies.length, 30);
for (const element of ['PUMPKIN', 'GHOST']) {
  const families = newFamilies.filter(row => row.section.includes(element));
  assert.equal(families.length, 15, `Expected 15 ${element} families`);
  for (const role of ['攻击', '防御', '均衡']) {
    assert.equal(families.filter(row => row.detail[0] === role).length, 5);
  }
}
assert.ok(newFamilies.some(row => row.names.join('/') === 'Pipkin/Vinejack/Crownkin'));
assert.ok(newFamilies.some(row => row.names.join('/') === 'Tickaboo/Penduloom/Chronophantom'));

const designedIds = [...retrofits, ...newFamilies].flatMap(row => row.names.map(name => name.toLowerCase()));
const allIds = [...originalForms, ...designedIds];
assert.ok(allIds.every(id => /^[a-z]+$/.test(id)));
assert.equal(allIds.length, 513);
assert.equal(new Set(allIds).size, 513, 'Form ID collision with existing or proposed content');
assert.equal(originalLines.length + retrofits.length + newFamilies.length, 172);
assert.equal(retrofits.length * 2 + newFamilies.length * 3, 266);

const assets = [
  'spooky-pumpkin-patch-concept.png', 'haunted-house-concept.png',
  'pipkin-evolution-concept.png', 'tickaboo-evolution-concept.png',
];
const assetNotes = read('docs/halloween-concept-assets.md');
for (const asset of assets) {
  const bytes = fs.readFileSync(path.join(root, 'docs/images', asset));
  assert.equal(bytes.subarray(0, 8).toString('hex'), '89504e470d0a1a0a');
  assert.ok(design.includes(`images/${asset}`));
  assert.ok(assetNotes.includes(`docs/images/${asset}`));
}
assert.equal((assetNotes.match(/Final prompt:/g) || []).length, 4);

console.log('Design catalog PASS: 30 new families, 88 exact retrofits, 8 giants included.');
console.log('Target 172 families / 513 unique forms; 266 additions; four concept PNGs present.');
console.log('This validates design consistency only, not Unity implementation or gameplay balance.');
