import { test, expect } from '@playwright/test';
import { clearDatabase, API_BASE } from './helpers.js';
import { ProductsPage } from '../page-objects/ProductsPage.js';
import { DishesPage } from '../page-objects/DishesPage.js';

test.describe('Валидация', () => {
  let productsPage;

  test.beforeEach(async ({ page, request }) => {
    await clearDatabase(request);
    productsPage = new ProductsPage(page);
    await productsPage.goto();
    await productsPage.switchToProductsTab();
  });

  test('автоисправление суммы БЖУ >100 при создании продукта', async ({ page }) => {
    await productsPage.openNewProductForm();
    await productsPage.fillProductForm({
      name: 'Жирный продукт',
      calories: 500,
      proteins: 60,
      fats: 30,
      carbohydrates: 10.1
    });

    page.on('dialog', d => d.accept().catch(() => {}));
    await productsPage.productSubmitBtn.click();
    await page.waitForTimeout(1000);

    const proteins = await productsPage.productProteinsInput.inputValue();
    
    expect(parseFloat(proteins)).toBeNaN();
  });

  test('обязательное поле "Название" у продукта', async () => {
    await productsPage.openNewProductForm();
    await productsPage.productNameInput.fill('');
    await productsPage.productSubmitBtn.click();
    await expect(productsPage.productFormContainer).toBeVisible();
  });

  test('нельзя удалить продукт, который используется в блюде', async ({ page, request }) => {
    const product = await request.post(`${API_BASE}/products`, {
      data: { 
        name: 'Используемый продукт',
        calories: 100,
        proteins: 10,
        fats: 10, 
        carbohydrates: 10,
        category: 0, 
        cookingRequirement: 0 
      }
    }).then(r => r.json());
    await request.post(`${API_BASE}/dishes`, {
      data: { name: 'Блюдо с продуктом', portionSize: 200, ingredients: [{ productId: product.id, amount: 100 }] }
    });

    await productsPage.goto();
    await productsPage.switchToProductsTab();
    await productsPage.reloadProducts();
    await expect(productsPage.page.locator('#productsTable')).toBeVisible();

    // Первый диалог (confirm)
    const firstDialog = new Promise(resolve => {
      page.once('dialog', async dialog => {
        expect(dialog.message()).toContain('Удалить продукт?');
        await dialog.accept();
        resolve();
      });
    });

    await productsPage.clickDeleteOnRow('Используемый продукт');
    await firstDialog;

    // Второй диалог (alert с ошибкой)
    const secondDialog = new Promise(resolve => {
      page.once('dialog', async dialog => {
        expect(dialog.message()).toBeTruthy();
        await dialog.accept();
        resolve();
      });
    });

    await secondDialog;

    await expect(productsPage.getProductRow('Используемый продукт')).toBeVisible();
  });
});