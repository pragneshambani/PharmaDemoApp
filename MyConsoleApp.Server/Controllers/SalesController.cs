using Microsoft.AspNetCore.Mvc;
using MyConsoleApp.Server.Models;
using MyConsoleApp.Server.Repositories;

namespace MyConsoleApp.Server.Controllers;

public class CreateSaleRequest
{
    public Guid MedicineId { get; set; }
    public int QuantitySold { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleRepository _saleRepository;

    public SalesController(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    /// <summary>
    /// Gets all recorded sales.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleRecord>>> GetAll()
    {
        var sales = await _saleRepository.GetAllAsync();
        return Ok(sales);
    }

    /// <summary>
    /// Creates a new sale record and decrements medicine stock.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SaleRecord>> CreateSale([FromBody] CreateSaleRequest request)
    {
        try
        {
            var record = await _saleRepository.AddSaleAsync(request.MedicineId, request.QuantitySold);
            return Ok(record);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
