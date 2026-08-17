using System.Text.Json;
using MyConsoleApp.Server.Models;

namespace MyConsoleApp.Server.Repositories;

public class JsonMedicineRepository : IMedicineRepository
{
    private readonly string _filePath;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonMedicineRepository(IWebHostEnvironment env)
    {
        var dataFolder = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataFolder);
        _filePath = Path.Combine(dataFolder, "medicines.json");

        if (!File.Exists(_filePath))
        {
            SeedInitialData();
        }
    }

    private void SeedInitialData()
    {
        var initial = new List<Medicine>
        {
            new() { Id = Guid.NewGuid(), FullName = "Amoxicillin 500mg", Brand = "Pfizer", Quantity = 50, Price = 12.50m, ExpiryDate = DateTime.Today.AddDays(90), Notes = "Take with food" },
            new() { Id = Guid.NewGuid(), FullName = "Paracetamol 500mg", Brand = "GSK", Quantity = 5, Price = 4.99m, ExpiryDate = DateTime.Today.AddDays(200), Notes = "Pain reliever" },
            new() { Id = Guid.NewGuid(), FullName = "Ibuprofen 400mg", Brand = "Bayer", Quantity = 25, Price = 8.75m, ExpiryDate = DateTime.Today.AddDays(15), Notes = "Anti-inflammatory" },
            new() { Id = Guid.NewGuid(), FullName = "Cough Syrup 100ml", Brand = "Novartis", Quantity = 4, Price = 15.00m, ExpiryDate = DateTime.Today.AddDays(10), Notes = "For dry cough" }
        };

        File.WriteAllText(_filePath, JsonSerializer.Serialize(initial, _jsonOptions));
    }

    private async Task<List<Medicine>> ReadAllInternalAsync()
    {
        if (!File.Exists(_filePath)) return new List<Medicine>();
        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<Medicine>>(json, _jsonOptions) ?? new List<Medicine>();
    }

    private async Task SaveInternalAsync(List<Medicine> items)
    {
        var tempFile = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(tempFile, json);
        File.Move(tempFile, _filePath, overwrite: true);
    }

    public async Task<IEnumerable<Medicine>> GetAllAsync(string? searchTerm = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                items = items.Where(m =>
                    m.FullName.ToLowerInvariant().Contains(term) ||
                    m.Brand.ToLowerInvariant().Contains(term) ||
                    m.Notes.ToLowerInvariant().Contains(term)
                ).ToList();
            }
            return items;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Medicine?> GetByIdAsync(Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            return items.FirstOrDefault(m => m.Id == id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Medicine> AddAsync(Medicine medicine)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            medicine.Id = Guid.NewGuid();
            items.Add(medicine);
            await SaveInternalAsync(items);
            return medicine;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> UpdateAsync(Medicine medicine)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            var index = items.FindIndex(m => m.Id == medicine.Id);
            if (index == -1) return false;

            items[index] = medicine;
            await SaveInternalAsync(items);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var items = await ReadAllInternalAsync();
            var count = items.RemoveAll(m => m.Id == id);
            if (count > 0)
            {
                await SaveInternalAsync(items);
                return true;
            }
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
