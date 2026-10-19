using BudgetCalculator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCalculator.Controllers;

[ApiController]
[Route("api/budget")]
public class BudgetController : ControllerBase
{
    [HttpPost("calculate")]
    public IActionResult Calculate([FromBody] BudgetRequest request)
    {
        if (request.Days <= 0) return BadRequest("Количество дней должно быть больше нуля.");

        var total = request.DailyCost * request.Days;

        // Исправлено: страховка стоит 5% (раньше было total *= 5, то есть 500%)
        if (request.IncludeInsurance)
        {
            total *= 1.05m;
        }

        return Ok(new { TotalAmount = total });
    }
}
