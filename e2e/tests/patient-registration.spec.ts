import { test, expect } from '@playwright/test';

test.describe('Patient Registration Feature (M1)', () => {
  test('should register a patient through the 4-step wizard and receive generated MRN', async ({ page }) => {
    // Intercept backend API calls with mock responses if backend is offline in headless e2e
    await page.route('**/api/v1/geo/divisions', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 1, code: '30', nameEn: 'Dhaka', nameBn: 'ঢাকা' },
          { id: 2, code: '20', nameEn: 'Chattogram', nameBn: 'চট্টগ্রাম' }
        ])
      });
    });

    await page.route('**/api/v1/geo/districts*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 1, divisionId: 1, code: '26', nameEn: 'Dhaka', nameBn: 'ঢাকা' }
        ])
      });
    });

    await page.route('**/api/v1/geo/upazilas*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 1, districtId: 1, nameEn: 'Dhanmondi', nameBn: 'ধানমন্ডি' }
        ])
      });
    });

    await page.route('**/api/v1/patients/check-duplicate', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          hasDuplicateWarning: false
        })
      });
    });

    await page.route('**/api/v1/patients', async route => {
      if (route.request().method() === 'POST') {
        const payload = route.request().postDataJSON();
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '88888888-8888-8888-8888-888888888888',
            hospitalId: '11111111-1111-1111-1111-111111111111',
            mrn: '2026-000001',
            displayName: `${payload.firstNameEn} ${payload.lastNameEn}`,
            phonePrimary: payload.phonePrimary,
            gender: payload.gender,
            bloodGroup: payload.bloodGroup,
            createdAt: new Date().toISOString()
          })
        });
      } else {
        await route.continue();
      }
    });

    // Navigate to registration page
    await page.goto('/patients/register');

    // Step 1: Demographics
    await expect(page.locator('h1.wizard-title')).toContainText('Register New Patient');
    await page.fill('input[formControlName="firstNameEn"]', 'Tariq');
    await page.fill('input[formControlName="lastNameEn"]', 'Hasan');
    await page.fill('input[formControlName="firstNameBn"]', 'তারিক');
    await page.fill('input[formControlName="lastNameBn"]', 'হাসান');
    await page.fill('input[formControlName="dateOfBirth"]', '1995-05-15');
    await page.selectOption('select[formControlName="gender"]', '1'); // Male
    await page.selectOption('select[formControlName="bloodGroup"]', '7'); // O+

    // Move to Step 2
    await page.click('button:has-text("Next Step")');

    // Step 2: Contact & Location
    await page.fill('input[formControlName="phonePrimary"]', '01711223344');
    await page.selectOption('select[formControlName="divisionId"]', { label: 'Dhaka (ঢাকা)' });
    await page.selectOption('select[formControlName="districtId"]', { label: 'Dhaka (ঢাকা)' });
    await page.selectOption('select[formControlName="upazilaId"]', { label: 'Dhanmondi (ধানমন্ডি)' });
    await page.fill('input[formControlName="addressLine"]', 'Road 27, House 42');

    // Move to Step 3
    await page.click('button:has-text("Next Step")');

    // Step 3: Identity & Emergency Contact
    await page.fill('input[formControlName="nationalId"]', '1995123456789');
    await page.fill('input[formControlName="name"]', 'Nasreen Hasan');
    await page.selectOption('select[formControlName="relationship"]', 'Spouse');
    await page.fill('input[formControlName="phone"]', '01811223344');

    // Move to Step 4: Review
    await page.click('button:has-text("Next Step")');

    // Verify Review Step is displayed
    await expect(page.locator('.review-title')).toBeVisible();
    await expect(page.locator('.review-card')).toContainText('Tariq Hasan');
    await expect(page.locator('.review-card')).toContainText('01711223344');

    // Submit registration
    await page.click('button:has-text("Complete Registration")');

    // Verify Success Modal and Assigned MRN
    await expect(page.locator('.modal-title')).toContainText('Patient Successfully Registered!');
    await expect(page.locator('.mrn-box-value')).toHaveText('2026-000001');
  });
});
