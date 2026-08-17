using System.Text.Json;
using MyConsoleApp.Server.Models;

namespace MyConsoleApp.Server.Repositories;

public class JsonSaleRepository : ISaleRepository
{
    private readonly string _salesFilePath;
    private readonly IMedicineRepository _medicineRepository;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonSaleRepository(IWebHostEnvironment env, IMedicineRepository medicineRepository)
    {
        _medicineRepository = medicineRepository;
        var dataFolder = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataFolder);
        _salesFilePath = Path.Combine(dataFolder, "sales.json");

        if (!File.Exists(_salesFilePath))
        {
            SeedInitialData();
        }
    }

    private void SeedInitialData()
    {
        var initial = new List<SaleRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MedicineId = Guid.NewGuid(),
                MedicineName = "Amoxicillin 500mg",
                QuantitySold = 10,
                UnitPrice = 12.50m,
                TotalAmount = 125.00m,
                SaleDate = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MedicineId = Guid.NewGuid(),
                MedicineName = "Paracetamol 500mg",
                QuantitySold = 15,
                UnitPrice = 4.99m,
                TotalAmount = 74.85m,
                SaleDate = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                MedicineId = Guid.NewGuid(),
                MedicineName = "Atorvastatin 20mg",
                QuantitySold = 5,
                UnitPrice = 35.99m,
                TotalAmount = 179.95m,
                SaleDate = DateTime.UtcNow.AddHours(-3)
            }
        };

        File.WriteAllText(_salesFilePath, JsonSerializer.Serialize(initial, _jsonOptions));
    }

    private async Task<List<SaleRecord>> ReadAllInternalAsync()
    {
        if (!File.Exists(_salesFilePath)) return new List<SaleRecord>();
        var json = await File.ReadAllTextAsync(_salesFilePath);
        return JsonSerializer.Deserialize<List<SaleRecord>>(json, _jsonOptions) ?? new List<SaleRecord>();
    }

    private async Task SaveInternalAsync(List<SaleRecord> items)
    {
        var tempFile = _salesFilePath + ".tmp";
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(tempFile, json);
        File.Move(tempFile, _salesFilePath, overwrite: true);
    }

    public async Task<IEnumerable<SaleRecord>> GetAllAsync(string? sortBy = null, bool isAscending = true)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                items = sortBy.ToLowerInvariant() switch
                {
                    "saledate" => isAscending ? items.OrderBy(s => s.SaleDate).ToList() : items.OrderByDescending(s => s.SaleDate).ToList(),
                    "medicinename" => isAscending ? items.OrderBy(s => s.MedicineName).ToList() : items.OrderByDescending(s => s.MedicineName).ToList(),
                    "quantitysold" => isAscending ? items.OrderBy(s => s.QuantitySold).ToList() : items.OrderByDescending(s => s.QuantitySold).ToList(),
                    "unitprice" => isAscending ? items.OrderBy(s => s.UnitPrice).ToList() : items.OrderByDescending(s => s.UnitPrice).ToList(),
                    "totalamount" => isAscending ? items.OrderBy(s => s.TotalAmount).ToList() : items.OrderByDescending(s => s.TotalAmount).ToList(),
                    _ => items
                };
            }
            return items;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SaleRecord?> GetByIdAsync(Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            return items.FirstOrDefault(s => s.Id == id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SaleRecord> AddSaleAsync(Guid medicineId, int quantitySold)
    {
        var medicine = await _medicineRepository.GetByIdAsync(medicineId)
            ?? throw new KeyNotFoundException("Medicine not found.");

        if (quantitySold <= 0)
        {
            throw new InvalidOperationException("Quantity sold must be greater than zero.");
        }

        if (medicine.Quantity < quantitySold)
        {
            throw new InvalidOperationException($"Insufficient stock available. Current stock: {medicine.Quantity}");
        }

        // Decrement stock via medicine repository
        medicine.Quantity -= quantitySold;
        await _medicineRepository.UpdateAsync(medicine);

        await _semaphore.WaitAsync();
        try
        {
            var sales = await ReadAllInternalAsync();
            var record = new SaleRecord
            {
                Id = Guid.NewGuid(),
                MedicineId = medicine.Id,
                MedicineName = medicine.FullName,
                QuantitySold = quantitySold,
                UnitPrice = medicine.Price,
                TotalAmount = medicine.Price * quantitySold,
                SaleDate = DateTime.UtcNow
            };
            sales.Add(record);
            await SaveInternalAsync(sales);
            return record;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
