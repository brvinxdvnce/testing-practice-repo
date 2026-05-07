# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: dishes.spec.js >> Блюда >> редактирование блюда (добавление ингредиента)
- Location: tests\dishes.spec.js:53:7

# Error details

```
Test timeout of 10000ms exceeded.
```

# Page snapshot

```yaml
- generic [active] [ref=e1]:
  - heading "Книга рецептов" [level=1] [ref=e2]
  - generic [ref=e3]:
    - button "Продукты" [ref=e4]
    - button "Блюда" [ref=e5]
  - separator [ref=e6]
  - generic [ref=e7]:
    - heading "Продукты" [level=2] [ref=e8]
    - generic [ref=e9]:
      - textbox "Поиск по названию" [ref=e10]
      - combobox [ref=e11]:
        - option "Все категории" [selected]
        - option "Замороженный"
        - option "Мясной"
        - option "Овощи"
        - option "Зелень"
        - option "Специи"
        - option "Крупы"
        - option "Консервы"
        - option "Жидкость"
        - option "Сладости"
      - combobox [ref=e12]:
        - option "Любая готовка" [selected]
        - option "Готовый к употреблению"
        - option "Полуфабрикат"
        - option "Требует приготовления"
      - combobox [ref=e13]:
        - option "Все флаги" [selected]
        - option "Веган"
        - option "Без глютена"
        - option "Без сахара"
      - combobox [ref=e14]:
        - option "Название" [selected]
        - option "Калории"
        - option "Белки"
        - option "Жиры"
        - option "Углеводы"
      - button "Применить фильтры" [ref=e15]
      - button "Сбросить" [ref=e16]
      - button "Новый продукт" [ref=e17]
    - generic [ref=e18]:
      - heading "Список продуктов" [level=3] [ref=e19]
      - table [ref=e20]:
        - rowgroup [ref=e21]:
          - row "Название Калории Белки Жиры Углеводы Категория Флаги Действия" [ref=e22]:
            - columnheader "Название" [ref=e23]
            - columnheader "Калории" [ref=e24]
            - columnheader "Белки" [ref=e25]
            - columnheader "Жиры" [ref=e26]
            - columnheader "Углеводы" [ref=e27]
            - columnheader "Категория" [ref=e28]
            - columnheader "Флаги" [ref=e29]
            - columnheader "Действия" [ref=e30]
        - rowgroup [ref=e31]:
          - row "Курица 165 31 3.6 0 Мясной Нет 📷 Просмотр Редактировать Удалить" [ref=e32]:
            - cell "Курица" [ref=e33]
            - cell "165" [ref=e34]
            - cell "31" [ref=e35]
            - cell "3.6" [ref=e36]
            - cell "0" [ref=e37]
            - cell "Мясной" [ref=e38]
            - cell "Нет" [ref=e39]
            - cell "📷 Просмотр Редактировать Удалить" [ref=e40]:
              - button "📷" [ref=e41]
              - button "Просмотр" [ref=e42]
              - button "Редактировать" [ref=e43]
              - button "Удалить" [ref=e44]
          - row "Рис 130 2.7 0.3 28 Крупы Нет 📷 Просмотр Редактировать Удалить" [ref=e45]:
            - cell "Рис" [ref=e46]
            - cell "130" [ref=e47]
            - cell "2.7" [ref=e48]
            - cell "0.3" [ref=e49]
            - cell "28" [ref=e50]
            - cell "Крупы" [ref=e51]
            - cell "Нет" [ref=e52]
            - cell "📷 Просмотр Редактировать Удалить" [ref=e53]:
              - button "📷" [ref=e54]
              - button "Просмотр" [ref=e55]
              - button "Редактировать" [ref=e56]
              - button "Удалить" [ref=e57]
```