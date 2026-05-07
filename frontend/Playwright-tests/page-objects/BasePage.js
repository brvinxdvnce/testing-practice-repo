import { expect } from '@playwright/test';
import { FRONTEND_URL } from '../Tests/helpers.js';

export class BasePage {
  constructor(page) {
    this.page = page;
  }

  async goto() {
    await this.page.goto(FRONTEND_URL);
  }

  async switchToProductsTab() {
    await this.page.click('#tabProductsBtn');
  }

  async switchToDishesTab() {
    await this.page.click('#tabDishesBtn');
  }

  async waitForDialogAndAccept(expectedMessage) {
    const dialog = await this.page.waitForEvent('dialog');
    if (expectedMessage) {
      expect(dialog.message()).toContain(expectedMessage);
    }
    await dialog.accept();
    return dialog;
  }
}