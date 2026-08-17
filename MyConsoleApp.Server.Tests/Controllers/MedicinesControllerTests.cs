using Microsoft.AspNetCore.Mvc;
using Moq;
using MyConsoleApp.Server.Controllers;
using MyConsoleApp.Server.Models;
using MyConsoleApp.Server.Repositories;
using Xunit;

namespace MyConsoleApp.Server.Tests.Controllers;

public class MedicinesControllerTests
{
    private readonly Mock<IMedicineRepository> _mockRepo;
    private readonly MedicinesController _controller;

    public MedicinesControllerTests()
    {
        _mockRepo = new Mock<IMedicineRepository>();
        _controller = new MedicinesController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResultWithListOfMedicines()
    {
        // Arrange
        var testMedicines = new List<Medicine>
        {
            new() { Id = Guid.NewGuid(), FullName = "Medicine A", Brand = "Brand A", Quantity = 20, Price = 10.00m, ExpiryDate = DateTime.Today.AddDays(100) },
            new() { Id = Guid.NewGuid(), FullName = "Medicine B", Brand = "Brand B", Quantity = 5, Price = 15.50m, ExpiryDate = DateTime.Today.AddDays(10) }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()))
                 .ReturnsAsync(testMedicines);

        // Act
        var result = await _controller.GetAll(null, null, true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnMedicines = Assert.IsAssignableFrom<IEnumerable<Medicine>>(okResult.Value);
        Assert.Equal(2, returnMedicines.Count());
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithMedicine()
    {
        // Arrange
        var id = Guid.NewGuid();
        var medicine = new Medicine { Id = id, FullName = "Amoxicillin", Brand = "Pfizer", Quantity = 10, Price = 5.00m, ExpiryDate = DateTime.Today.AddDays(60) };
        _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(medicine);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnMedicine = Assert.IsType<Medicine>(okResult.Value);
        Assert.Equal("Amoxicillin", returnMedicine.FullName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Medicine?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidMedicine_ReturnsCreatedAtAction()
    {
        // Arrange
        var medicine = new Medicine { FullName = "New Med", Brand = "Brand X", Quantity = 50, Price = 12.00m, ExpiryDate = DateTime.Today.AddDays(90) };
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Medicine>())).ReturnsAsync((Medicine m) => m);

        // Act
        var result = await _controller.Create(medicine);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdMed = Assert.IsType<Medicine>(createdResult.Value);
        Assert.Equal("New Med", createdMed.FullName);
    }

    [Fact]
    public async Task Update_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var medicine = new Medicine { Id = id, FullName = "Updated Med", Brand = "Brand Y", Quantity = 30, Price = 8.00m, ExpiryDate = DateTime.Today.AddDays(40) };
        _mockRepo.Setup(r => r.UpdateAsync(medicine)).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(id, medicine);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_IdMismatch_ReturnsBadRequest()
    {
        // Arrange
        var id = Guid.NewGuid();
        var medicine = new Medicine { Id = Guid.NewGuid(), FullName = "Med", Brand = "Brand", Quantity = 10, Price = 5.00m, ExpiryDate = DateTime.Today };

        // Act
        var result = await _controller.Update(id, medicine);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
