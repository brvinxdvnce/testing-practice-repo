import { test, expect } from '@playwright/test';
import { clearDatabase, createTestProduct, API_BASE } from './helpers.js';
import { DishesPage } from '../page-objects/DishesPage.js';

test.describe('Блюда', () => {
  let dishesPage;

  test.beforeEach(async ({ page, request }) => {
    await clearDatabase(request);
    await createTestProduct(request, 'Курица', { calories: 165, proteins: 31, fats: 3.6, carbohydrates: 0, category: 1 });
    await createTestProduct(request, 'Рис', { calories: 130, proteins: 2.7, fats: 0.3, carbohydrates: 28, category: 5 });
    dishesPage = new DishesPage(page);
    await dishesPage.goto();
    await dishesPage.switchToDishesTab();
    await expect(page.locator('#dishesSection')).toBeVisible();
    await dishesPage.reloadDishes();
  });

  test('создание блюда с ингредиентами и авторасчётом КБЖУ', async () => {
    await dishesPage.openNewDishForm();
    await dishesPage.fillDishForm({ name: 'Курица с рисом', portionSize: 300, category: 2 });
    await dishesPage.addIngredient('Курица', 150);
    await dishesPage.addIngredient('Рис', 100);

    const calories = await dishesPage.dishCaloriesInput.inputValue();
    expect(parseFloat(calories)).toBeCloseTo(377.5, 0);

    await dishesPage.submitDishForm();
    await dishesPage.reloadDishes();
    
    const caloriesInTable = await dishesPage.getDishRow('Курица с рисом')
      .locator('td:nth-child(2)')
      .textContent();
    expect(parseFloat(caloriesInTable)).toBeCloseTo(377.5, 1);
  });

  test('создание блюда с ручным вводом КБЖУ (переопределение авторасчёта)', async ({ page }) => {
    await dishesPage.openNewDishForm();
    await dishesPage.fillDishForm({ name: 'Омлет', portionSize: 200 });
    await dishesPage.addIngredient('Курица', 100);

    await dishesPage.dishCaloriesInput.fill('500');
    await dishesPage.dishProteinsInput.fill('30');

    await dishesPage.submitDishForm();
    await dishesPage.reloadDishes();

    const row = dishesPage.getDishRow('Омлет');
    await expect(row).toBeVisible();
    await expect(row.locator('td:nth-child(2)')).toHaveText('500');
    await expect(row.locator('td:nth-child(3)')).toHaveText('30');
  });

  test('редактирование блюда (добавление ингредиента)', async () => {
    await dishesPage.openNewDishForm();
    await dishesPage.fillDishForm({ name: 'Пустое блюдо', portionSize: 100, category: 0 });
    await dishesPage.submitDishForm();
    await dishesPage.reloadDishes();

    await expect(dishesPage.getDishRow('Пустое блюдо')).toBeVisible();
    
    await dishesPage.clickEditOnRow('Пустое блюдо');
    await dishesPage.addIngredient('Рис', 200);
    await dishesPage.submitDishForm();
    await dishesPage.reloadDishes();
    
    await expect(dishesPage.getDishRow('Пустое блюдо').locator('td:nth-child(2)'))
      .toHaveText("260", { timeout: 5000 });
  });

  test('удаление блюда', async ({ page, request }) => {
    await request.post(`${API_BASE}/dishes`, {
      data: { name: 'Удаляемое блюдо', portionSize: 150, category: 0, ingredients: [] }
    });
    await dishesPage.reloadDishes();

    page.on('dialog', dialog => dialog.accept().catch(() => {}));
    
    const row = dishesPage.getDishRow('Удаляемое блюдо');
    await row.locator('button:has-text("Удалить")').click();

    await dishesPage.reloadDishes();
    await expect(dishesPage.getDishRow('Удаляемое блюдо')).not.toBeVisible({ timeout: 5000 });
  });

  test('просмотр деталей блюда (ингредиенты, фото)', async ({ page, request }) => {
    const product = await createTestProduct(request, 'Помидор', { calories: 18 });
    await request.post(`${API_BASE}/dishes`, {
      data: {
        name: 'Салат',
        portionSize: 200,
        category: 4,
        ingredients: [{ productId: product.id, amount: 150 }],
        photos: ['/uploads/test.jpg']
      }
    });

    await dishesPage.reloadDishes();
    await expect(dishesPage.getDishRow('Салат')).toBeVisible();

    await dishesPage.clickViewOnRow('Салат');
    await expect(dishesPage.dishDetailsContainer).toBeVisible();
    await expect(dishesPage.dishDetailsContainer).toContainText('Помидор');
    await expect(dishesPage.dishDetailsContainer.locator('img')).toHaveCount(1);
  });

   test('фильтрация блюд по категории "Суп"', async () => {
    await dishesPage.openNewDishForm();
    await dishesPage.fillDishForm({ name: 'Борщ', portionSize: 300, category: 5 });  //Суп
    await dishesPage.submitDishForm();
    
    await dishesPage.openNewDishForm();
    await dishesPage.fillDishForm({ name: 'Стейк', portionSize: 200, category: 2 });  //Второе
    await dishesPage.submitDishForm();
    
    await dishesPage.reloadDishes();
    await expect(dishesPage.getDishRow('Борщ')).toBeVisible();
    await expect(dishesPage.getDishRow('Стейк')).toBeVisible();

    await dishesPage.categoryFilter.selectOption('5');
    await dishesPage.applyFiltersBtn.click();
    
    await expect(dishesPage.dishRows).toHaveCount(1, { timeout: 5000 });
    await expect(dishesPage.getDishRow('Борщ')).toBeVisible();
    await expect(dishesPage.getDishRow('Стейк')).not.toBeVisible();
  });

  test('сортировка блюд по калориям по убыванию', async ({ page, request }) => {
    await request.post(`${API_BASE}/dishes`, { data: { name: 'Низкокал', portionSize: 100, calories: 50, ingredients: [] } });
    await request.post(`${API_BASE}/dishes`, { data: { name: 'Высококал', portionSize: 100, calories: 200, ingredients: [] } });
    await dishesPage.reloadDishes();

    await dishesPage.sortSelect.selectOption('calories');
    await dishesPage.sortDescCheckbox.check();
    await dishesPage.applyFiltersBtn.click();
    await expect(dishesPage.dishRows.first().locator('td:first-child')).toHaveText('Высококал', { timeout: 5000 });
  });   
});