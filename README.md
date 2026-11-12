# WearStore

Модуль импорта товаров магазина одежды из CSV-файла (ASP.NET Core MVC, C#, .NET 8).
Индивидуальное задание производственной практики ПП.02.01, вариант 3 «Одежда».

## Возможности

- импорт товаров из CSV: по пути к файлу или через загрузку файла в браузере
- валидация данных, список ошибок с номерами строк
- страница списка товаров с поиском по названию, фильтром по категории и сортировкой по цене и остатку
- очистка списка товаров
- данные хранятся только в памяти приложения (после перезапуска очищаются)

## Формат CSV

Заголовок и поля (кодировка UTF-8):

sku,name,description,price,currency,category_name,stock,image_url,is_active,size,color,material

Поля варианта «Одежда»: size (XS, S, M, L, XL, XXL или число от 36 до 60), color (обязательно), material (может быть пустым).

## Где лежат файлы с данными

- WearStore/wwwroot/csv/sample_products.csv — основной пример, 11 корректных товаров. На странице «Импорт» указать путь /csv/sample_products.csv или загрузить файл кнопкой «Выберите файл»
- WearStore.Tests/testdata/ — тестовые файлы с ошибками: invalid_price.csv, duplicate_sku.csv, negative_stock.csv, bad_size.csv, missing_color.csv, bad_active.csv, empty_file.csv, valid_clothing.csv

## Структура проекта

- Controllers/ProductsController.cs — контроллер
- Models/ — Product, ImportResult, ImportError, view-модели, константы
- Services/ — репозиторий в памяти, парсер CSV, сервис импорта
- Views/Products/ — страницы «Товары» и «Импорт»
- WearStore.Tests/ — unit-тесты (xUnit)

## Запуск

Открыть решение в Visual Studio и нажать F5. Откроется сайт со списком товаров, импорт доступен в меню «Импорт».

## Тесты

Visual Studio: Тест → Запустить все тесты (Ctrl+R, A).