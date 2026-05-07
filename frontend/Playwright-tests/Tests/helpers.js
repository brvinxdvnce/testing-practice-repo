// helpers.js
export async function waitForApiResponse(page, urlSubstring, options = {}) {
  await page.waitForResponse(
    resp => resp.url().includes(urlSubstring) && resp.status() === 200,
    options
  );
}

export async function clearDatabase(page) {
  const products = await page.request.get('http://localhost:5006/api/products');
  const dishes = await page.request.get('http://localhost:5006/api/dishes');
  for (const p of await products.json()) {
    await page.request.delete(`http://localhost:5006/api/products/${p.id}`);
  }
  for (const d of await dishes.json()) {
    await page.request.delete(`http://localhost:5006/api/dishes/${d.id}`);
  }
}