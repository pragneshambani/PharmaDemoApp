using MyConsoleApp.Server.Models;

namespace MyConsoleApp.Server.Repositories;

public interface ISaleRepository
{
    Task<IEnumerable<SaleRecord>> GetAllAsync();
    Task<SaleRecord?> GetByIdAsync(Guid id);
    Task<SaleRecord> AddSaleAsync(Guid medicineId, int quantitySold);
}
