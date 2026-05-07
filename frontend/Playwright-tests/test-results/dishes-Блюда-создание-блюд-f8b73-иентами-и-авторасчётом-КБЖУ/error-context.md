# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: dishes.spec.js >> Блюда >> создание блюда с ингредиентами и авторасчётом КБЖУ
- Location: tests\dishes.spec.js:20:7

# Error details

```
Test timeout of 10000ms exceeded.
```

# Page snapshot

```yaml
- generic [ref=e1]:
  - heading "Книга рецептов" [level=1] [ref=e2]
  - generic [ref=e3]:
    - button "Продукты" [ref=e4]
    - button "Блюда" [ref=e5]
  - separator [ref=e6]
  - generic [ref=e7]:
    - heading "Блюда" [level=2] [ref=e8]
    - generic [ref=e9]:
      - textbox "Поиск по названию" [ref=e10]
      - combobox [ref=e11]:
        - option "Все категории" [selected]
        - option "Десерт"
        - option "Первое"
        - option "Второе"
        - option "Напиток"
        - option "Салат"
        - option "Суп"
        - option "Перекус"
      - combobox [ref=e12]:
        - option "Все флаги" [selected]
        - option "Веган"
        - option "Без глютена"
        - option "Без сахара"
      - combobox [ref=e13]:
        - option "Название" [selected]
        - option "Калории"
        - option "Белки"
        - option "Жиры"
        - option "Углеводы"
      - generic [ref=e14]:
        - checkbox "По убыванию" [ref=e15]
        - text: По убыванию
      - button "Применить фильтры" [ref=e16]
      - button "Сбросить" [ref=e17]
      - button "Новое блюдо" [ref=e18]
    - generic [ref=e19]:
      - heading "Список блюд" [level=3] [ref=e20]
      - table [ref=e21]:
        - rowgroup [ref=e22]:
          - row "Название Калории Белки Жиры Углеводы Порция (г) Категория Флаги Действия" [ref=e23]:
            - columnheader "Название" [ref=e24]
            - columnheader "Калории" [ref=e25]
            - columnheader "Белки" [ref=e26]
            - columnheader "Жиры" [ref=e27]
            - columnheader "Углеводы" [ref=e28]
            - columnheader "Порция (г)" [ref=e29]
            - columnheader "Категория" [ref=e30]
            - columnheader "Флаги" [ref=e31]
            - columnheader "Действия" [ref=e32]
        - rowgroup
    - generic [ref=e33]:
      - heading "Создать блюдо" [level=3] [ref=e34]
      - generic [ref=e35]:
        - generic [ref=e37]:
          - text: "Название:"
          - textbox "Название:" [ref=e38]: Курица с рисом
        - generic [ref=e40]:
          - text: "Размер порции (г):"
          - spinbutton "Размер порции (г):" [active] [ref=e41]: "300"
        - generic [ref=e43]:
          - text: "Категория:"
          - combobox "Категория:" [ref=e44]:
            - option "-- Выберите категорию --" [disabled]
            - option "Десерт"
            - option "Первое"
            - option "Второе" [selected]
            - option "Напиток"
            - option "Салат"
            - option "Суп"
            - option "Перекус"
        - generic [ref=e46]:
          - text: "Фотографии (URL):"
          - 'textbox "Фотографии (URL): Добавить" [ref=e47]':
            - /placeholder: Вставьте URL картинки
          - button "Добавить" [ref=e48]
        - generic [ref=e50]:
          - text: "Ингредиенты (продукт и количество в граммах):"
          - 'combobox "Ингредиенты (продукт и количество в граммах): Добавить ингредиент" [ref=e51]'
          - spinbutton [ref=e52]
          - button "Добавить ингредиент" [ref=e53]
          - list
        - generic [ref=e55]:
          - text: "КБЖУ (опционально, переопределяет расчёт):"
          - spinbutton "КБЖУ (опционально, переопределяет расчёт):" [ref=e56]
          - spinbutton [ref=e57]
          - spinbutton [ref=e58]
          - spinbutton [ref=e59]
        - generic [ref=e61]:
          - text: "Флаги (можно выбрать несколько):"
          - 'checkbox "Флаги (можно выбрать несколько): Веган Без глютена Без сахара" [ref=e62]'
          - text: Веган
          - checkbox [ref=e63]
          - text: Без глютена
          - checkbox [ref=e64]
          - text: Без сахара
        - generic [ref=e65]:
          - button "Сохранить" [ref=e66]
          - button "Отмена" [ref=e67]
```