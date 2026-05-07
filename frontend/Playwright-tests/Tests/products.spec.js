import { test, expect } from '@playwright/test';
import { clearDatabase, createTestProduct } from './helpers.js';
import { ProductsPage } from '../page-objects/ProductsPage';

test.describe('Продукты', () => {
  let productsPage;

  test.beforeEach(async ({ page, request }) => {
    await clearDatabase(request);
    productsPage = new ProductsPage(page);
    await productsPage.goto();
    await productsPage.switchToProductsTab();
    // просто убедимся, что секция видна
    await expect(page.locator('#productsSection')).toBeVisible();
    await productsPage.reloadProducts();
  });

  test('создание продукта со всеми полями', async () => {
    await productsPage.openNewProductForm();
    await productsPage.fillProductForm({
      name: 'Морковь',
      calories: 41,
      proteins: 1,
      fats: 0.2,
      carbohydrates: 9.6,
      description: 'Сладкая морковь',
      category: 2,
      cooking: 0,
      flags: [1]
    });

    await productsPage.submitProductForm();
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Морковь')).toBeVisible({ timeout: 5000 });
  });

  test('редактирование продукта', async ({ request }) => {
    await createTestProduct(request, 'Старое имя');
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Старое имя')).toBeVisible();

    await productsPage.clickEditOnRow('Старое имя');
    await productsPage.productNameInput.fill('Новое имя');
    await productsPage.productCaloriesInput.fill('999');

    await productsPage.submitProductForm();
    await productsPage.reloadProducts();
    const row = productsPage.getProductRow('Новое имя');
    await expect(row).toBeVisible({ timeout: 5000 });
    await expect(row.locator('td:nth-child(2)')).toHaveText('999');
  });

test('удаление продукта', async ({ page, request }) => {
  await createTestProduct(request, 'Удаляемый продукт');
  await productsPage.reloadProducts();
  await expect(productsPage.getProductRow('Удаляемый продукт')).toBeVisible();

  const dialogPromise = new Promise(resolve => {
    page.once('dialog', async dialog => {
      expect(dialog.message()).toContain('Удалить продукт?');
      await dialog.accept();
      resolve();
    });
  });

  await productsPage.clickDeleteOnRow('Удаляемый продукт');
  await dialogPromise;
  await productsPage.reloadProducts();
  await expect(productsPage.getProductRow('Удаляемый продукт')).not.toBeVisible({ timeout: 5000 });
});

  test('фильтрация по категории "Мясной"', async ({ request }) => {
    await createTestProduct(request, 'Говядина', { category: 1 });
    await createTestProduct(request, 'Огурец', { category: 2 });
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Огурец')).toBeVisible();

    await productsPage.categoryFilter.selectOption('1');
    await productsPage.applyFiltersBtn.click();
    await expect(productsPage.productRows).toHaveCount(1, { timeout: 5000 });
    await expect(productsPage.getProductRow('Говядина')).toBeVisible();
  });

  test('фильтрация по флагу "Веган"', async ({ request }) => {
    await createTestProduct(request, 'Веган продукт', { flags: 1 });
    await createTestProduct(request, 'Не веган', { flags: 0 });
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Не веган')).toBeVisible();

    await productsPage.flagsFilter.selectOption('1');
    await productsPage.applyFiltersBtn.click();
    await expect(productsPage.productRows).toHaveCount(1, { timeout: 5000 });
    await expect(productsPage.getProductRow('Веган продукт')).toBeVisible();
  });

  test('сортировка по калориям (по возрастанию)', async ({ request }) => {
    await createTestProduct(request, 'Низкокалорийный', { calories: 50 });
    await createTestProduct(request, 'Высококалорийный', { calories: 200 });
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Высококалорийный')).toBeVisible();

    await productsPage.sortSelect.selectOption('calories');
    await productsPage.applyFiltersBtn.click();
    await expect(productsPage.productRows.first().locator('td:first-child')).toHaveText('Низкокалорийный', { timeout: 5000 });
  });

  test('сброс фильтров', async ({ request }) => {
    await createTestProduct(request, 'Фильтр');
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Фильтр')).toBeVisible();

    await productsPage.searchInput.fill('несуществующее');
    await productsPage.applyFiltersBtn.click();
    await expect(productsPage.productRows).toHaveCount(0, { timeout: 5000 });

    await productsPage.resetFiltersBtn.click();
    await productsPage.reloadProducts();
    await expect(productsPage.getProductRow('Фильтр')).toBeVisible({ timeout: 5000 });
    await expect(productsPage.searchInput).toHaveValue('');
  });
});