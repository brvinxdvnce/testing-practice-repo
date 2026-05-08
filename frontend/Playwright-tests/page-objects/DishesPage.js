import { BasePage } from './BasePage';

export class DishesPage extends BasePage {
  constructor(page) {
    super(page);
    this.dishesTable = page.locator('#dishesTable tbody');
    this.dishRows = this.dishesTable.locator('tr');
    this.searchInput = page.locator('#dishSearch');
    this.categoryFilter = page.locator('#dishCategoryFilter');
    this.flagsFilter = page.locator('#dishFlagsFilter');
    this.sortSelect = page.locator('#dishSortBy');
    this.sortDescCheckbox = page.locator('#dishSortDesc');
    this.applyFiltersBtn = page.locator('#applyDishFilters');
    this.resetFiltersBtn = page.locator('#resetDishFilters');
    this.newDishBtn = page.locator('#newDishBtn');
    this.dishFormContainer = page.locator('#dishFormContainer');
    this.dishForm = page.locator('#dishForm');
    this.dishIdInput = page.locator('#dishId');
    this.dishNameInput = page.locator('#dishName');
    this.dishPortionSizeInput = page.locator('#dishPortionSize');
    this.dishCategorySelect = page.locator('#dishCategory');
    this.dishCaloriesInput = page.locator('#dishCalories');
    this.dishProteinsInput = page.locator('#dishProteins');
    this.dishFatsInput = page.locator('#dishFats');
    this.dishCarbohydratesInput = page.locator('#dishCarbohydrates');
    this.dishSubmitBtn = page.locator('#dishForm button[type="submit"]');
    this.cancelDishFormBtn = page.locator('#cancelDishForm');
    this.ingredientProductSelect = page.locator('#ingredientProductSelect');
    this.ingredientAmountInput = page.locator('#ingredientAmount');
    this.addIngredientBtn = page.locator('#addIngredientBtn');
    this.ingredientsList = page.locator('#ingredientsList');
    this.dishDetailsContainer = page.locator('#dishDetailsContainer');
    this.flagVeganCheckbox = page.locator('#dishForm input[type="checkbox"][value="1"]');
    this.flagGlutenFreeCheckbox = page.locator('#dishForm input[type="checkbox"][value="2"]');
    this.flagSugarFreeCheckbox = page.locator('#dishForm input[type="checkbox"][value="4"]');
  }


  async openNewDishForm() {
      await this.newDishBtn.click();
      await this.page.waitForTimeout(500);
  }

  async selectIngredient(productName) {
      await this.page.waitForTimeout(500);
      try {
          const option = this.ingredientProductSelect.locator('option', { hasText: productName });
          const value = await option.getAttribute('value');
          await this.ingredientProductSelect.selectOption(value);
      } catch (e) {
          await this.ingredientProductSelect.selectOption({ index: 1 });
      }
  }

  async fillDishForm({ name, portionSize, category, calories, proteins, fats, carbohydrates, flags = [] }) {
    if (name) await this.dishNameInput.fill(name);
    if (portionSize !== undefined) await this.dishPortionSizeInput.fill(String(portionSize));
    if (category !== undefined) await this.dishCategorySelect.selectOption(String(category));
    if (calories !== undefined) await this.dishCaloriesInput.fill(String(calories));
    if (proteins !== undefined) await this.dishProteinsInput.fill(String(proteins));
    if (fats !== undefined) await this.dishFatsInput.fill(String(fats));
    if (carbohydrates !== undefined) await this.dishCarbohydratesInput.fill(String(carbohydrates));
    await this.flagVeganCheckbox.uncheck();
    await this.flagGlutenFreeCheckbox.uncheck();
    await this.flagSugarFreeCheckbox.uncheck();
    for (const flag of flags) {
      if (flag === 1) await this.flagVeganCheckbox.check();
      if (flag === 2) await this.flagGlutenFreeCheckbox.check();
      if (flag === 4) await this.flagSugarFreeCheckbox.check();
    }
  }

  async addIngredient(productName, amount) {
    await this.selectIngredient(productName);
    await this.ingredientAmountInput.fill(String(amount));
    await this.addIngredientBtn.click();
    await this.page.waitForTimeout(300);
  }

  async submitDishForm() {
    const responsePromise = this.page.waitForResponse(
      resp => resp.url().includes('/api/dishes') && (resp.request().method() === 'POST' || resp.request().method() === 'PUT'),
      { timeout: 5000 }
    );
    await this.dishSubmitBtn.click();
    return responsePromise;
  }

  getDishRow(name) {
    return this.dishesTable.locator('tr', { hasText: name }).first();
  }

  async clickEditOnRow(name) {
    const row = this.getDishRow(name);
    await row.waitFor({ state: 'visible' });
    await row.locator('button:has-text("Редактировать")').click();
    await this.dishFormContainer.waitFor({ state: 'visible' });
  }

  async clickDeleteOnRow(name) {
  const row = this.getDishRow(name);
  await row.waitFor({ state: 'visible' });
  const deleteBtn = row.locator('button:has-text("Удалить")');

  // Вешаем обработчик диалога до клика, чтобы Playwright не подвис
  const dialogPromise = this.page.waitForEvent('dialog');
  await deleteBtn.click();
  const dialog = await dialogPromise;

  if (dialog.message().includes('Удалить блюдо')) {
    await dialog.accept();
  } else {
    await dialog.dismiss();
  }
}

  async clickViewOnRow(name) {
    const row = this.getDishRow(name);
    await row.waitFor({ state: 'visible' });
    await row.locator('button:has-text("Просмотр")').click();
    await this.dishDetailsContainer.waitFor({ state: 'visible' });
  }

  async reloadDishes() {
    const respPromise = this.page.waitForResponse(resp => resp.url().includes('/api/dishes') && resp.request().method() === 'GET');
    await this.applyFiltersBtn.click();
    await respPromise;
  }
}