using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Books;

public interface IBookRepository
{
    Task<Book?> FindById(BookId id);
    Task Save(Book book);
    Task<int> CountActiveHoldsByPatron(PatronId patronId);
}
