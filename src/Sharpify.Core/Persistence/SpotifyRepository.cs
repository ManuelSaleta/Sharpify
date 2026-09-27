using System;
using System.IO.IsolatedStorage;

namespace Sharpify.Core.Persistence;

public interface IRepository<T>
{
    public Task<bool> ContainsAsync(string id, CancellationToken t = default);
    public Task<T> GetByIdAsync(string id, CancellationToken t = default);
    public Task<IEnumerable<T>> ListAsync(CancellationToken t = default);
    public Task<T> AddAsync(T value, CancellationToken t = default);
    public Task<T> RemoveAsync(string id, CancellationToken t = default);
    public Task<T> UpdateAsync(string id, CancellationToken t = default);
}

public class SpotifyRepository<T>(IStorageService storage) : IRepository<T>
{
    private readonly IStorageService _storage = storage
        ?? throw new ArgumentNullException(nameof(storage));
    public Task<T> AddAsync(T value, CancellationToken t = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ContainsAsync(string id, CancellationToken t = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> GetByIdAsync(string id, CancellationToken t = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<T>> ListAsync(CancellationToken t = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> RemoveAsync(string id, CancellationToken t = default)
    {
        throw new NotImplementedException();
    }

    public Task<T> UpdateAsync(string id, CancellationToken t = default)
    {
        throw new NotImplementedException();
    }
}
