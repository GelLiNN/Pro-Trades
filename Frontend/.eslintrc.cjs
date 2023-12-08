/** @type {import("eslint").Linter.Config} */
module.exports = {
  env: {
    browser: true,
    es2020: true,
    node: true,
  },
  ignorePatterns: ['dist', '!*.cjs'],
  root: true,
  settings: {
    react: {
      version: 'detect',
    },
  },

  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react/recommended',
    'plugin:react/jsx-runtime',
    'plugin:react-hooks/recommended',
    'plugin:prettier/recommended',
  ],
  plugins: ['react-refresh'],
  rules: {
    'prettier/prettier': ['error'],

    'react/jsx-sort-props': ['error', {ignoreCase: true}],

    'react-refresh/only-export-components': ['warn'],
  },
}
