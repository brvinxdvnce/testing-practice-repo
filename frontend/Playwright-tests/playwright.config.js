export default {
  use: {
    baseURL: '',   // порт, где будет отдаваться статика
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  testDir: 'tests',
  timeout: 10000,
};