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

    public async Task<IEnumerable<SaleRecord>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await ReadAllInternalAsync();
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
