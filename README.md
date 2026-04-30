# QuizPlatform API 🎯

Це RESTful API для платформи онлайн-вікторин, розроблений на базі ASP.NET Core (.NET 10) та Entity Framework Core (PostgreSQL). 

Проєкт дозволяє створювати вікторини, додавати питання різних типів (Single Choice, Multiple Choice, True/False), проходити тестування, автоматично підраховувати бали та формувати таблицю лідерів. Особливу увагу в проєкті приділено архітектурі тестування: реалізовано модульні (Unit), інтеграційні (Integration), тести бази даних (Database з Testcontainers) та тести продуктивності (k6).

## 🛠 Технології
* **Фреймворк:** ASP.NET Core Web API (.NET 10)
* **База даних:** PostgreSQL + Entity Framework Core
* **Тестування:** xUnit, NSubstitute, Shouldly, AutoFixture
* **БД для тестів:** Testcontainers (PostgreSQL), In-Memory Database
* **Покриття коду:** Coverlet
* **Тестування продуктивності:** k6

---

## Як зібрати та запустити

### Передумови:
1. Встановлений [.NET 10 SDK](https://dotnet.microsoft.com/).
2. Запущений **Docker** (обов'язково для роботи Testcontainers під час тестів БД).
3. Встановлений та запущений PostgreSQL (для локального запуску самого API, рядок підключення знаходиться в `appsettings.json`).

### Кроки запуску:
1. Відкрийте термінал у кореневій папці проєкту.
2. Перейдіть до папки з API:
   cd QuizPlatform.Api
3. Зберіть проєкт:
   dotnet build
4. Запустіть застосунок:
   dotnet run
Після запуску API буде доступне за адресою, вказаною в терміналі (зазвичай https://localhost:5001 або http://localhost:5000). Swagger UI для тестування ендпоінтів доступний за шляхом /swagger.

### Як запустити тести
Проєкт містить понад 30 тестів, які перевіряють бізнес-логіку, інтеграцію компонентів та роботу з реальною базою даних через Testcontainers.

Щоб запустити всі тести, виконайте в кореневій папці проєкту команду:
   dotnet test

### Як згенерувати звіт покриття коду
Проєкт налаштований на вимірювання покриття коду за допомогою Coverlet.

Для генерації звіту виконайте команду:
   dotnet test --collect:"XPlat Code Coverage"
Після виконання команди в папці QuizPlatform.Tests/TestResults/{guid}/ з'явиться файл coverage.cobertura.xml.

(Опціонально) Щоб згенерувати зручний HTML-звіт, можна використати інструмент ReportGenerator:
   dotnet tool install -g dotnet-reportgenerator-globaltool
   reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
Після цього відкрийте файл coveragereport/index.html у браузері.

### Як запустити тести продуктивності (k6)
Для перевірки стійкості API під навантаженням написані скрипти k6.

Передумови:
1. Переконайтеся, що API запущено (dotnet run).

2. Встановіть k6.

Запуск сценаріїв:
Відкрийте новий термінал у кореневій папці проєкту (не зупиняючи API) та виконайте потрібний сценарій:

1. Smoke-тест (перевірка базової працездатності, 1-2 користувачі):
   k6 run k6/smoke-test.js
2. Навантажувальний тест (Load Test) (імітація нормального навантаження, 10-50 користувачів):
   k6 run k6/load-test.js
3. Стрес-тест (Stress Test) (поступове збільшення навантаження до відмови):
   k6 run k6/stress-test.js