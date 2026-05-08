import { BasePage } from './BasePage';

export class ProductsPage extends BasePage {
  constructor(page) {
    super(page);
    this.productsTable = page.locator('#productsTable tbody');
    this.productRows = this.productsTable.locator('tr');
    this.searchInput = page.locator('#productSearch');
    this.categoryFilter = page.locator('#productCategoryFilter');
    this.cookingFilter = page.locator('#productCookingFilter');
    this.flagsFilter = page.locator('#productFlagsFilter');
    this.sortSelect = page.locator('#productSortBy');
    this.applyFiltersBtn = page.locator('#applyProductFilters');
    this.resetFiltersBtn = page.locator('#resetProductFilters');
    this.newProductBtn = page.locator('#newProductBtn');
    this.productFormContainer = page.locator('#productFormContainer');
    this.productForm = page.locator('#productForm');
    this.productIdInput = page.locator('#productId');
    this.productNameInput = page.locator('#productName');
    this.productCaloriesInput = page.locator('#productCalories');
    this.productProteinsInput = page.locator('#productProteins');
    this.productFatsInput = page.locator('#productFats');
    this.productCarbohydratesInput = page.locator('#productCarbohydrates');
    this.productDescriptionInput = page.locator('#productDescription');
    this.productCategorySelect = page.locator('#productCategory');
    this.productCookingSelect = page.locator('#productCooking');
    this.productSubmitBtn = page.locator('#productForm button[type="submit"]');
    this.cancelProductFormBtn = page.locator('#cancelProductForm');
    this.productDetailsContainer = page.locator('#productDetailsContainer');
    this.flagVeganCheckbox = page.locator('#productForm input[type="checkbox"][value="1"]');
    this.flagGlutenFreeCheckbox = page.locator('#productForm input[type="checkbox"][value="2"]');
    this.flagSugarFreeCheckbox = page.locator('#productForm input[type="checkbox"][value="4"]');
  }

  async openNewProductForm() {
    await this.newProductBtn.click();
    await this.productFormContainer.waitFor({ state: 'visible' });
  }

  async fillProductForm({ name, calories, proteins, fats, carbohydrates, description, category, cooking, flags = [] }) {
    if (name) await this.productNameInput.fill(name);
    if (calories !== undefined) await this.productCaloriesInput.fill(String(calories));
    if (proteins !== undefined) await this.productProteinsInput.fill(String(proteins));
    if (fats !== undefined) await this.productFatsInput.fill(String(fats));
    if (carbohydrates !== undefined) await this.productCarbohydratesInput.fill(String(carbohydrates));
    if (description !== undefined) await this.productDescriptionInput.fill(description);
    if (category !== undefined) await this.productCategorySelect.selectOption(String(category));
    if (cooking !== undefined) await this.productCookingSelect.selectOption(String(cooking));
    await this.flagVeganCheckbox.uncheck();
    await this.flagGlutenFreeCheckbox.uncheck();
    await this.flagSugarFreeCheckbox.uncheck();
    for (const flag of flags) {
      if (flag === 1) await this.flagVeganCheckbox.check();
      if (flag === 2) await this.flagGlutenFreeCheckbox.check();
      if (flag === 4) await this.flagSugarFreeCheckbox.check();
    }
  }

  async submitProductForm() {
    const responsePromise = this.page.waitForResponse(
      resp => resp.url().includes('/api/products') && (resp.request().method() === 'POST' || resp.request().method() === 'PUT')
    );
    await this.productSubmitBtn.click();
    return responsePromise;
  }

  getProductRow(name) {
    return this.productsTable.locator('tr', { hasText: name }).first();
  }

  async clickEditOnRow(name) {
    const row = this.getProductRow(name);
    await row.waitFor({ state: 'visible' });
    await row.locator('button:has-text("Редактировать")').click();
    await this.productFormContainer.waitFor({ state: 'visible' });
  }

  async clickDeleteOnRow(name) {
    const row = this.getProductRow(name);
    await row.waitFor({ state: 'visible' });
    await row.locator('button:has-text("Удалить")').click();
  }

  async reloadProducts() {
    const respPromise = this.page.waitForResponse(resp => resp.url().includes('/api/products') && resp.request().method() === 'GET');
    await this.applyFiltersBtn.click();
    await respPromise;
  }
}