using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;

namespace WeddingManager.Application.Services;

public class WeddingService : IWeddingService
{
    private readonly IWeddingRepository _repository;

    public WeddingService(IWeddingRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<IEnumerable<Wedding>> GetAllAsync() => await _repository.GetAllAsync();
    public async Task<Wedding?> GetByIdAsync(Guid id) => await _repository.GetByIdAsync(id);
    public async Task AddAsync(Wedding wedding) => await _repository.AddAsync(wedding);
    public async Task UpdateAsync(Wedding wedding) => await _repository.UpdateAsync(wedding);
    public async Task DeleteAsync(Guid id) => await _repository.DeleteAsync(id);
}