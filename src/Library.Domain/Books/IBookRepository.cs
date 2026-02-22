using Library.Domain.Patrons;

namespace Library.Domain.Books;

public interface IBookRepository
{
    Task<Book?> FindById(BookId id);
    Task Save(Book book);

    /// <summary>
    /// 指定 Patron の有効予約数（Waiting + Assigned）を返す。
    /// </summary>
    Task<int> CountActiveHoldsByPatron(PatronId patronId);
}
