import { test, expect } from '@playwright/test';

test('создание нового продукта', async ({ page }) => {
  await page.goto('http://127.0.0.1:5500/frontend/index_v14.html');
  
  await page.click('#tabProductsBtn');
  await expect(page.locator('#productsSection')).toBeVisible();
  
  await page.click('#newProductBtn');
  await expect(page.locator('#productFormContainer')).toBeVisible();
  
  await page.fill('#productName', 'Тестовый продукт');
  await page.fill('#productCalories', '250');
  await page.fill('#productProteins', '20');
  await page.fill('#productFats', '15');
  await page.fill('#productCarbohydrates', '5');
  await page.fill('#productDescription', 'Описание тестового продукта');
  await page.selectOption('#productCategory', '1');
  await page.selectOption('#productCooking', '1');
  await page.check('input[type="checkbox"][value="1"]');
  
  await page.fill('#productPhotoUrl', 'https://example.com/photo.jpg');
  await page.click('#addProductPhotoBtn');
  await expect(page.locator('#productPhotoList img')).toHaveCount(1);
  
  // Ждём ЛЮБОЙ запрос к API (не только POST, любой метод, любой статус)
  const responsePromise = page.waitForResponse(
    resp => resp.url().includes('/api/products'),
    { timeout: 15000 }
  );
  await page.click('#productForm button[type="submit"]');
  const response = await responsePromise;
  
  console.log('===== ОТВЕТ ОТ API =====');
  console.log('Статус:', response.status());
  console.log('Метод:', response.request().method());
  console.log('URL:', response.request().url());
  
  // Если нужно, смотрим тело ответа
  if (response.status() !== 200 && response.status() !== 201) {
    console.log('Тело ответа:', await response.text());
  }
  
  // Даём время на обновление списка
  await page.waitForTimeout(1500);
  
  // Ищем строку с продуктом в любом месте таблицы
  const productRow = page.locator('#productsTable tbody tr', { hasText: 'Тестовый продукт' });
  await expect(productRow).toBeVisible({ timeout: 10000 });
});