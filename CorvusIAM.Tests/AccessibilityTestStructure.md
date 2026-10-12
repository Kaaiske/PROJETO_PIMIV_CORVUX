# Automated Accessibility Test Structure (a11y)

This document provides a structure and example on how to setup automated accessibility tests using Playwright and Axe for the CorvusIAM project.

## Setup Instructions

1.  **Initialize Node.js project (if not already present):**
    ```bash
    npm init -y
    ```
2.  **Install Playwright and Axe-Core for Playwright:**
    ```bash
    npm install -D @playwright/test @axe-core/playwright
    ```
3.  **Initialize Playwright configuration:**
    ```bash
    npx playwright install
    ```

## Example Test Script (`accessibility.spec.js`)

Create a directory `tests/a11y` in the root of your Node project, and add the following test script:

```javascript
const { test, expect } = require('@playwright/test');
const AxeBuilder = require('@axe-core/playwright').default;

test.describe('Accessibility Tests for CorvusIAM', () => {
    
    test('Homepage should not have any automatically detectable accessibility issues', async ({ page }) => {
        // Replace with the actual local or staging URL of the CorvusIAM application
        await page.goto('https://localhost:5001/');
        
        // Wait for main content to load
        await page.waitForSelector('main');

        // Run axe accessibility checks
        const accessibilityScanResults = await new AxeBuilder({ page })
            .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
            .analyze();

        // Expect 0 violations
        expect(accessibilityScanResults.violations).toEqual([]);
    });
    
    test('Login page should not have any accessibility issues', async ({ page }) => {
        await page.goto('https://localhost:5001/Auth/Login');
        
        // Inject and run axe checks
        const accessibilityScanResults = await new AxeBuilder({ page })
            .include('form') // We can scope down the analysis if needed
            .analyze();

        expect(accessibilityScanResults.violations).toEqual([]);
    });
});
```

## Running the Tests

To run the automated tests in a headless browser, run:
```bash
npx playwright test
```

## Continuous Integration

This script can be easily integrated into a CI/CD pipeline (e.g., GitHub Actions, Azure DevOps). Ensure that your pipeline has an instance of your application running locally before executing `npx playwright test`. If violations are found, the build step will fail, preventing inaccessible code from being deployed.
