using Library.Domain.Books;
using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Infrastructure.Repositories;

public class InMemoryBookRepository : IBookRepository
{
    private readonly Dictionary<BookId, Book> _books = new();

    public void Add(Book book) => _books[book.Id] = book;

    public Task<Book?> FindById(BookId id)
        => Task.FromResult(_books.GetValueOrDefault(id));

    public Task Save(Book book)
    {
        _books[book.Id] = book;
        return Task.CompletedTask;
    }

    public Task<int> CountActiveHoldsByPatron(PatronId patronId)
    {
        var count = _books.Values
            .SelectMany(b => b.Holds)
            .Count(h => h.PatronId == patronId
                && h.Status is HoldStatus.Waiting or HoldStatus.Assigned);
        return Task.FromResult(count);
    }
}
