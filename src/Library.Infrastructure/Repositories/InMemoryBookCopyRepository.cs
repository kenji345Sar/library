using Library.Domain.Copies;
using Library.Domain.Copies.Entities;
using Library.Domain.Copies.ValueObjects;

namespace Library.Infrastructure.Repositories;

public class InMemoryBookCopyRepository : IBookCopyRepository
{
    private readonly Dictionary<CopyId, BookCopy> _copies = new();

    public void Add(BookCopy copy) => _copies[copy.Id] = copy;

    public Task<BookCopy?> FindById(CopyId id)
        => Task.FromResult(_copies.GetValueOrDefault(id));

    public Task Save(BookCopy copy)
    {
        _copies[copy.Id] = copy;
        return Task.CompletedTask;
    }
}
