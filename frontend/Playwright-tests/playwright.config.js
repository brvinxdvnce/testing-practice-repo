export default {
  use: {
    baseURL: '',   // порт, где будет отдаваться статика
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
//   webServer: [
//     {
//       command: 'npx http-server ../frontend -p 5500', // сервер для статики
//       port: 5500,
//       reuseExistingServer: !process.env.CI,
//     },
//     {
//       command: 'dotnet run --project ../backend --urls=http://localhost:5006',
//       port: 5006,
//       reuseExistingServer: !process.env.CI,
//     }
//   ],
  testDir: 'tests',
  timeout: 10000,
};