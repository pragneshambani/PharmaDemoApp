using MyConsoleApp.Server.Models;

namespace MyConsoleApp.Server.Repositories;

public interface IMedicineRepository
{
    Task<IEnumerable<Medicine>> GetAllAsync(string? searchTerm = null);
    Task<Medicine?> GetByIdAsync(Guid id);
    Task<Medicine> AddAsync(Medicine medicine);
    Task<bool> UpdateAsync(Medicine medicine);
    Task<bool> DeleteAsync(Guid id);
}
