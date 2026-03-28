using System.Collections.Concurrent;

namespace FrameworksPrac2.Services;

public sealed class InMemoryStorage : IStorage
{
    private readonly ConcurrentQueue<string> _values = new();

    public void Add(string value) => _values.Enqueue(value);

    public IReadOnlyList<string> GetAll() => _values.ToArray();
}
