import { expect, test } from '@playwright/test';

test('loads with empty state', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('app')).toBeVisible();
  await expect(page.getByTestId('empty-state')).toBeVisible();
});

test('user picker has users from API', async ({ page }) => {
  await page.goto('/');
  const select = page.getByTestId('user-picker').locator('select');
  const optionCount = await select.locator('option').count();
  expect(optionCount).toBeGreaterThan(1); // placeholder + ≥1 real user
});
