# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: dishes.spec.js >> Блюда >> просмотр деталей блюда (ингредиенты, фото)
- Location: tests\dishes.spec.js:86:7

# Error details

```
Error: apiRequestContext.post: connect EACCES ::1:5006
Call log:
  - → POST http://localhost:5006/api/products
    - user-agent: Playwright/1.59.1 (x64; windows 10.0) node/22.14
    - accept: */*
    - accept-encoding: gzip,deflate,br
    - content-type: application/json
    - content-length: 172

```