# Budget Calculator — тестирование REST API (xUnit)

Учебный проект: покрываем тестами сервис «Калькулятор бюджета» (ASP.NET Core) и ищем в нём ошибки.

Эндпоинт: `POST /api/budget/calculate`

```json
{ "dailyCost": 100, "days": 5, "includeInsurance": true }
```

## Структура

| Папка | Что внутри |
|---|---|
| `BudgetCalculator/` | API: `BudgetController`, модель `BudgetRequest` |
| `BudgetCalculator.Tests/` | xUnit-тесты контроллера |

## Запуск тестов

```bash
dotnet test
```

## Найденный баг

Страховка должна стоить 5% от суммы, а код умножал сумму на 5 (500%):

```diff
-    total *= 5;
+    total *= 1.05m;
```

Тест `Calculate_WithInsurance_AddsFivePercent` до исправления падал: ожидалось `525`, получено `2500`. После исправления проходит.

## Что проверяют тесты

| Тест | Входные данные | Ожидаемый результат |
|---|---|---|
| `Calculate_WithoutInsurance_ReturnsOkAndTotal` | 100 × 5 дней, без страховки | `200 OK`, `500` |
| `Calculate_WithInsurance_AddsFivePercent` | 100 × 5 дней, со страховкой | `200 OK`, `525` |
| `Calculate_DaysNotPositive_ReturnsBadRequest` | `Days = -1` и `Days = 0` | `400 Bad Request` + текст ошибки |
| `Calculate_ZeroDailyCost_ReturnsZero` | `DailyCost = 0` (без страховки и со страховкой) | `200 OK`, `0` |
| `Calculate_FractionalDailyCost_IsExactToKopecks` | 100.50 × 3 дня | `200 OK`, `301.50` |

У контроллера нет внешних зависимостей (база данных, сервисы), поэтому моки не нужны:
тесты создают `BudgetController` напрямую и вызывают `Calculate` как обычный метод.

## Покрытие кода

```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:Html
```

Откройте `coveragereport/index.html`. Для `BudgetController` покрыты обе ветки каждого `if`
(`Days <= 0` — да/нет, `IncludeInsurance` — да/нет). Класс `Program` в отчёте может показывать 0% —
он только запускает хост и в unit-тестах контроллера не участвует.
