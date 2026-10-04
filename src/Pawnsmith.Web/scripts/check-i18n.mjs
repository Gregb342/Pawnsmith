// The two catalogues must have exactly the same keys, none of them empty
// (§I.0, chapter 10 of the bible). Run by `npm run lint`, so the CI refuses a
// label added in one language only. No dependency: twenty lines of Node.
import { readFileSync } from 'node:fs';

const load = (language) => JSON.parse(readFileSync(new URL(`../src/i18n/locales/${language}.json`, import.meta.url), 'utf8'));

function leaves(node, prefix = '') {
  return Object.entries(node).flatMap(([key, value]) =>
    typeof value === 'object' && value !== null ? leaves(value, `${prefix}${key}.`) : [[`${prefix}${key}`, value]],
  );
}

const [fr, en] = ['fr', 'en'].map((language) => new Map(leaves(load(language))));
const problems = [
  ...[...fr.keys()].filter((key) => !en.has(key)).map((key) => `missing in en: ${key}`),
  ...[...en.keys()].filter((key) => !fr.has(key)).map((key) => `missing in fr: ${key}`),
  ...[...fr, ...en].filter(([, value]) => typeof value !== 'string' || value.trim() === '').map(([key]) => `empty: ${key}`),
];

if (problems.length > 0) {
  console.error(problems.join('\n'));
  process.exit(1);
}

console.log(`i18n: ${fr.size} keys, the same in fr and en.`);
