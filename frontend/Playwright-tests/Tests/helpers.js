// helpers.js
const API_BASE = 'http://localhost:5006/api';
const FRONTEND_URL = 'http://127.0.0.1:5500/frontend/index_v14.html';

// В helpers.js добавьте обработку ошибок в clearDatabase, чтобы один упавший запрос не рушил всё
export async function clearDatabase(request) {
  try {
    const dishesResp = await request.get(`${API_BASE}/dishes`, { timeout: 2000 });
    const dishes = await dishesResp.json();
    for (const dish of dishes) {
      await request.delete(`${API_BASE}/dishes/${dish.id}`).catch(() => {});
    }
    const productsResp = await request.get(`${API_BASE}/products`, { timeout: 2000 });
    const products = await productsResp.json();
    for (const product of products) {
      await request.delete(`${API_BASE}/products/${product.id}`).catch(() => {});
    }
  } catch (e) { console.log('Cleanup failed, skipping...'); }
}

export async function createTestProduct(request, name = 'Тестовый продукт', options = {}) {
  const defaultProduct = {
    name,
    calories: 100,
    proteins: 20,
    fats: 10,
    carbohydrates: 5,
    description: 'Описание',
    category: 1,
    cookingRequirement: 0,
    flags: 0,
    photos: []
  };
  const productData = { ...defaultProduct, ...options };
  const response = await request.post(`${API_BASE}/products`, { data: productData });
  return response.json();
}

export async function createTestDish(request, name = 'Тестовое блюдо', options = {}) {
  const defaultDish = {
    name,
    portionSize: 200,
    category: 0,
    ingredients: [],
    photos: [],
    calories: undefined,
    proteins: undefined,
    fats: undefined,
    carbohydrates: undefined,
    flags: 0
  };
  const dishData = { ...defaultDish, ...options };
  const response = await request.post(`${API_BASE}/dishes`, { data: dishData });
  return response.json();
}

export { API_BASE, FRONTEND_URL };