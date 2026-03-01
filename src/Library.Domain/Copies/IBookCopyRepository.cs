using Library.Domain.Copies.Entities;
using Library.Domain.Copies.ValueObjects;

namespace Library.Domain.Copies;

public interface IBookCopyRepository
{
    Task<BookCopy?> FindById(CopyId id);
    Task Save(BookCopy copy);
}
