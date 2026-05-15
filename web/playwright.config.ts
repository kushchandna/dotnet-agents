import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  retries: 0,
  timeout: 60_000,
  use: { baseURL: 'http://localhost:5173', trace: 'on-first-retry' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../src/DotnetAgents.Api -- --config ../samples/config.example.json',
      url: 'http://localhost:5000/health',
      reuseExistingServer: true,
      timeout: 60_000,
      env: { ASPNETCORE_URLS: 'http://localhost:5000', DUMMY: 'x', OPENAI_API_KEY: 'sk-dummy', COMPAT_KEY: 'x' },
    },
    { command: 'npm run dev', url: 'http://localhost:5173', reuseExistingServer: true, timeout: 60_000 }
  ]
});
