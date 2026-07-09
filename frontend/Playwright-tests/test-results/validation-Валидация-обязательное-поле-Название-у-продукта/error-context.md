# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: validation.spec.js >> Валидация >> обязательное поле "Название" у продукта
- Location: tests\validation.spec.js:35:7

# Error details

```
Error: page.goto: net::ERR_CONNECTION_REFUSED at http://127.0.0.1:5500/frontend/index_v14.html
Call log:
  - navigating to "http://127.0.0.1:5500/frontend/index_v14.html", waiting until "load"

```

# Test source

```ts
  1  | import { expect } from '@playwright/test';
  2  | import { FRONTEND_URL } from '../Tests/helpers.js';
  3  | 
  4  | export class BasePage {
  5  |   constructor(page) {
  6  |     this.page = page;
  7  |   }
  8  | 
  9  |   async goto() {
> 10 |     await this.page.goto(FRONTEND_URL);
     |                     ^ Error: page.goto: net::ERR_CONNECTION_REFUSED at http://127.0.0.1:5500/frontend/index_v14.html
  11 |   }
  12 | 
  13 |   async switchToProductsTab() {
  14 |     await this.page.click('#tabProductsBtn');
  15 |   }
  16 | 
  17 |   async switchToDishesTab() {
  18 |     await this.page.click('#tabDishesBtn');
  19 |   }
  20 | 
  21 |   async waitForDialogAndAccept(expectedMessage) {
  22 |     const dialog = await this.page.waitForEvent('dialog');
  23 |     if (expectedMessage) {
  24 |       expect(dialog.message()).toContain(expectedMessage);
  25 |     }
  26 |     await dialog.accept();
  27 |     return dialog;
  28 |   }
  29 | }
```