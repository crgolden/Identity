const { defineConfig } = require('eslint/config');
const globals = require('globals');
const sonar = require('./eslint-sonar.config.cjs');

module.exports = defineConfig(
  {
    ignores: ['**/node_modules/', '**/wwwroot/lib/', '**/bin/', '**/obj/'],
  },
  {
    files: ['Identity/wwwroot/js/**/*.js'],
    languageOptions: { sourceType: 'script', globals: { ...globals.browser, grecaptcha: 'readonly' } },
  },
  ...sonar,
);
