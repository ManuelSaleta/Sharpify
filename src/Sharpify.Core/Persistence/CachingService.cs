namespace Sharpify.Core.Persistence;

public interface ICachingService<T>
{
    public Task<T> GetAsync(string id, CancellationToken t = default);
}

public class CachingService<T>(IRepository<T> repository, Dictionary<string, T> cache) : ICachingService<T>
{
    private readonly IRepository<T> _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly Dictionary<string, T> _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<T> GetAsync(string id, CancellationToken t = default)
    {
        if (_cache.TryGetValue(id, out var hit))
        {
            return await Task.FromResult<T>(hit);
        }

        var item = await _repository.GetByIdAsync(id, t);

        if (item is not null)
        {
            var itemCachedSuccessfully = _cache.TryAdd(id, item);
            if (itemCachedSuccessfully is not true)
                Console.WriteLine($"Failed to add to cache for key: {id}, type of T {typeof(T)}");

            return item;
        }
        return item;
    }
}
