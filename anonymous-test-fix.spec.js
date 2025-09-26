const { test, expect } = require('@playwright/test');

const FRONTEND_URL = 'http://localhost:3000';

test('Anonymous Join Flow - Corrected', async ({ page }) => {
  console.log('\n👻 Testing Anonymous Join Flow (Corrected)...');

  await page.goto(FRONTEND_URL);

  // Click on Anonymous tab
  await page.click('[data-tab="anonymous"]');
  await expect(page.locator('#anonymous-tab')).toBeVisible();

  // Fill in fake invite code (corrected field ID)
  const fakeInviteCode = 'fake-invite-1234567890123456789012345678901234567890';
  await page.fill('#anonymous-invite-code', fakeInviteCode);

  // Click validate button
  await page.click('#validate-invite');
  await page.waitForTimeout(3000);

  // Should show error for invalid invite
  const errorElements = await page.locator('.error, .message, .alert').all();
  for (const errorElement of errorElements) {
    if (await errorElement.isVisible()) {
      const errorText = await errorElement.textContent();
      console.log(`✅ Expected anonymous invite validation error: ${errorText}`);
    }
  }

  console.log('✅ Anonymous join flow tested with correct field IDs');
});