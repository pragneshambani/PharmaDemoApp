using Microsoft.AspNetCore.Mvc;
using Moq;
using MyConsoleApp.Server.Controllers;
using MyConsoleApp.Server.Models;
using MyConsoleApp.Server.Repositories;
using Xunit;

namespace MyConsoleApp.Server.Tests.Controllers;

public class SalesControllerTests
{
    private readonly Mock<ISaleRepository> _mockRepo;
    private readonly SalesController _controller;

    public SalesControllerTests()
    {
        _mockRepo = new Mock<ISaleRepository>();
        _controller = new SalesController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResultWithSales()
    {
        // Arrange
        var sales = new List<SaleRecord>
        {
            new() { Id = Guid.NewGuid(), MedicineId = Guid.NewGuid(), MedicineName = "Paracetamol", QuantitySold = 2, UnitPrice = 5.00m, TotalAmount = 10.00m, SaleDate = DateTime.UtcNow }
        };
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync(sales);

        // Act
        var result = await _controller.GetAll(null, true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnSales = Assert.IsAssignableFrom<IEnumerable<SaleRecord>>(okResult.Value);
        Assert.Single(returnSales);
    }

    [Fact]
    public async Task CreateSale_ValidRequest_ReturnsOkWithRecord()
    {
        // Arrange
        var medId = Guid.NewGuid();
        var request = new CreateSaleRequest { MedicineId = medId, QuantitySold = 3 };
        var createdRecord = new SaleRecord { Id = Guid.NewGuid(), MedicineId = medId, MedicineName = "Amoxicillin", QuantitySold = 3, UnitPrice = 10.00m, TotalAmount = 30.00m };

        _mockRepo.Setup(r => r.AddSaleAsync(medId, 3)).ReturnsAsync(createdRecord);

        // Act
        var result = await _controller.CreateSale(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnRecord = Assert.IsType<SaleRecord>(okResult.Value);
        Assert.Equal(30.00m, returnRecord.TotalAmount);
    }

    [Fact]
    public async Task CreateSale_MedicineNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateSaleRequest { MedicineId = Guid.NewGuid(), QuantitySold = 1 };
        _mockRepo.Setup(r => r.AddSaleAsync(It.IsAny<Guid>(), It.IsAny<int>()))
                 .ThrowsAsync(new KeyNotFoundException("Medicine not found."));

        // Act
        var result = await _controller.CreateSale(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Medicine not found.", notFoundResult.Value);
    }

    [Fact]
    public async Task CreateSale_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSaleRequest { MedicineId = Guid.NewGuid(), QuantitySold = 100 };
        _mockRepo.Setup(r => r.AddSaleAsync(It.IsAny<Guid>(), It.IsAny<int>()))
                 .ThrowsAsync(new InvalidOperationException("Insufficient stock available."));

        // Act
        var result = await _controller.CreateSale(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Insufficient stock available.", badRequestResult.Value);
    }
}
