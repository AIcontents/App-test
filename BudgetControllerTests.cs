using BudgetCalculator.Controllers; // поправьте namespace'ы под свой проект,
using BudgetCalculator.Models;      // если у вас они называются иначе (или удалите, если их нет)
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BudgetCalculator.Tests;

public class BudgetControllerTests
{
    // У контроллера нет зависимостей (БД, сервисов), поэтому моки не нужны:
    // создаём его напрямую и вызываем метод как обычный C#-метод.
    private readonly BudgetController _controller = new();

    // Контроллер возвращает анонимный тип new { TotalAmount = ... }.
    // Анонимные типы internal в другой сборке, поэтому dynamic из тестов не сработает —
    // читаем свойство TotalAmount через рефлексию.
    private static decimal GetTotalAmount(OkObjectResult ok)
    {
        var property = ok.Value!.GetType().GetProperty("TotalAmount");
        Assert.NotNull(property);
        return (decimal)property!.GetValue(ok.Value)!;
    }

    // ---------- Часть 1: Happy Path ----------

    [Fact]
    public void Calculate_WithoutInsurance_ReturnsOkAndTotal()
    {
        // Arrange
        var request = new BudgetRequest { DailyCost = 100m, Days = 5, IncludeInsurance = false };

        // Act
        var result = _controller.Calculate(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(500m, GetTotalAmount(ok));
    }

    // ---------- Часть 2: поиск бага ----------

    [Fact]
    public void Calculate_WithInsurance_AddsFivePercent()
    {
        var request = new BudgetRequest { DailyCost = 100m, Days = 5, IncludeInsurance = true };

        var result = _controller.Calculate(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        // 100 * 5 = 500; страховка 5% -> 525.
        // С багом (total *= 5) вернётся 2500, и тест упадёт.
        Assert.Equal(525m, GetTotalAmount(ok));
    }

    // ---------- Негативные и граничные сценарии ----------

    [Theory]
    [InlineData(-1)] // случай из задания
    [InlineData(0)]  // граница условия Days <= 0
    public void Calculate_DaysNotPositive_ReturnsBadRequest(int days)
    {
        var request = new BudgetRequest { DailyCost = 100m, Days = days, IncludeInsurance = false };

        var result = _controller.Calculate(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("Количество дней должно быть больше нуля.", badRequest.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Calculate_ZeroDailyCost_ReturnsZero(bool includeInsurance)
    {
        var request = new BudgetRequest { DailyCost = 0m, Days = 3, IncludeInsurance = includeInsurance };

        var result = _controller.Calculate(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(0m, GetTotalAmount(ok));
    }

    [Fact]
    public void Calculate_FractionalDailyCost_IsExactToKopecks()
    {
        // 100.50 * 3 = 301.50. Тип decimal считает точно, без ошибок double вида 0.30000000000000004.
        var request = new BudgetRequest { DailyCost = 100.50m, Days = 3, IncludeInsurance = false };

        var result = _controller.Calculate(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(301.50m, GetTotalAmount(ok));
    }
}
