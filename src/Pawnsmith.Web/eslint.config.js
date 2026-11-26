import js from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import globals from 'globals';
import tseslint from 'typescript-eslint';

/**
 * ESLint, in its "flat config" form — the single configuration file that
 * replaced the old cascade of `.eslintrc` files. It is a plain ES module,
 * evaluated as code, so there is no hidden merge order to work out: the array
 * below is applied in the order it is written.
 *
 * What this catches that `tsc` does not, which is the whole reason it is here:
 * the type checker proves the code is consistent, not that it is correct
 * React. The rules-of-hooks check is the one class of bug a type checker
 * structurally cannot see — a hook called inside a condition or a loop
 * compiles perfectly and misbehaves at runtime, intermittently.
 */
export default tseslint.config(
  {
    // Build output and dependencies are not ours to lint.
    ignores: ['dist/**', 'node_modules/**'],
  },

  js.configs.recommended,

  // The type-aware preset, not the plain one. It costs a slower run because it
  // asks the TypeScript compiler for real type information, and it buys the
  // rules that need it — a floating promise, an unnecessary `await`, a
  // condition that is always true. Those are the ones worth having.
  ...tseslint.configs.recommendedTypeChecked,

  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,

      // MEN-013 (DEC-116): the front shows other people's text — a project
      // name, a clause, a catalogue entry, possibly from an imported archive.
      // React escapes every string it inserts; this prop is the one way to
      // skip that escaping and insert raw HTML, so it is refused outright.
      // A core ESLint rule, matched on the JSX syntax tree: no plugin needed.
      'no-restricted-syntax': [
        'error',
        {
          selector: "JSXAttribute[name.name='dangerouslySetInnerHTML']",
          message: 'Raw HTML is forbidden (MEN-013, DEC-116): render text and let React escape it.',
        },
      ],
    },
  },

  {
    // These files configure or check the tooling and are not part of the
    // application, so they sit outside the type-aware programme. The scripts
    // run under Node, whose globals (process, console) they use.
    files: ['eslint.config.js', 'scripts/**/*.mjs'],
    ...tseslint.configs.disableTypeChecked,
    languageOptions: {
      globals: globals.node,
    },
  },
);
