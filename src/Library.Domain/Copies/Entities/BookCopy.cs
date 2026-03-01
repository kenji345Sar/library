using Library.Domain.Books.ValueObjects;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Copies.Entities;

public class BookCopy
{
    public CopyId Id { get; }
    public BookId BookId { get; }
    public CopyType Type { get; }
    public CopyStatus Status { get; private set; }
    public PatronId? HeldBy { get; private set; }

    public BookCopy(CopyId id, BookId bookId, CopyType type)
    {
        Id = id;
        BookId = bookId;
        Type = type;
        Status = CopyStatus.Available;
    }

    public void PlaceOnHold(PatronId patronId)
    {
        if (Status != CopyStatus.Available)
            throw new InvalidOperationException("利用可能な BookCopy のみ取り置きできます。");

        Status = CopyStatus.OnHold;
        HeldBy = patronId;
    }

    public void Checkout()
    {
        if (Status != CopyStatus.OnHold)
            throw new InvalidOperationException("取り置き中の BookCopy のみ貸出できます。");

        Status = CopyStatus.Loaned;
        HeldBy = null;
    }
}
