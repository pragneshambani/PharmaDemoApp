using Microsoft.AspNetCore.Mvc;
using MyConsoleApp.Server.Models;
using MyConsoleApp.Server.Repositories;

namespace MyConsoleApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly IMedicineRepository _medicineRepository;

    public MedicinesController(IMedicineRepository medicineRepository)
    {
        _medicineRepository = medicineRepository;
    }

    /// <summary>
    /// Gets available medicines with optional search term.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Medicine>>> GetAll([FromQuery] string? search)
    {
        var medicines = await _medicineRepository.GetAllAsync(search);
        return Ok(medicines);
    }

    /// <summary>
    /// Gets a single medicine by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Medicine>> GetById(Guid id)
    {
        var medicine = await _medicineRepository.GetByIdAsync(id);
        if (medicine == null) return NotFound();
        return Ok(medicine);
    }

    /// <summary>
    /// Adds a new medicine detail to system inventory.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Medicine>> Create([FromBody] Medicine medicine)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _medicineRepository.AddAsync(medicine);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates existing medicine.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Medicine medicine)
    {
        if (id != medicine.Id) return BadRequest("ID mismatch.");
        var updated = await _medicineRepository.UpdateAsync(medicine);
        if (!updated) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Deletes medicine by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _medicineRepository.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
