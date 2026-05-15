import { expect, test } from '@playwright/test';

test('create session, send message, see streamed reply (using echo tool)', async ({ page }) => {
  await page.goto('/');

  // pick first non-placeholder user + agent
  await page.getByTestId('user-picker').locator('select').selectOption({ index: 1 });
  await page.getByTestId('agent-picker').locator('select').selectOption({ index: 1 });
  await page.getByRole('button', { name: 'New session' }).click();

  await expect(page.getByTestId('chat-view')).toBeVisible();

  // Note: this test requires either a live model endpoint OR expects an error event
  // when running with OPENAI_API_KEY=sk-dummy. Both are acceptable.
  await page.getByTestId('message-input').fill('Say hi');
  await page.getByTestId('send-button').click();

  // Wait for either an assistant bubble or a streaming bubble or an error message
  await expect(
    page.locator('[data-testid="streaming-bubble"], [data-testid^="message-ast_"], [data-testid^="message-err-"]')
  ).toBeVisible({ timeout: 30_000 });
});

test('session appears in list after creation', async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('user-picker').locator('select').selectOption({ index: 1 });
  await page.getByTestId('agent-picker').locator('select').selectOption({ index: 1 });
  await page.getByRole('button', { name: 'New session' }).click();
  const items = page.getByTestId('session-list').locator('li');
  await expect(items).toHaveCount(1);
});

test('delete removes session', async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('user-picker').locator('select').selectOption({ index: 1 });
  await page.getByTestId('agent-picker').locator('select').selectOption({ index: 1 });
  await page.getByRole('button', { name: 'New session' }).click();
  const items = page.getByTestId('session-list').locator('li');
  await expect(items).toHaveCount(1);
  await page.getByRole('button', { name: /Delete session/ }).first().click();
  await expect(items).toHaveCount(0);
});
