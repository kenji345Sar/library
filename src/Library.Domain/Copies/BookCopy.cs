using Library.Domain.Books;
using Library.Domain.Patrons;

namespace Library.Domain.Copies;

public record CopyId(Guid Value)
{
    public static CopyId NewId() => new(Guid.NewGuid());
}

public enum CopyType
{
    /// <summary>通常貸出用</summary>
    Circulating,

    /// <summary>研究専用（Researcher のみ）</summary>
    Restricted,
}

public enum CopyStatus
{
    Available,
    OnHold,
    Loaned,
}

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

    /// <summary>
    /// 取り置き状態にする（Hold 割当時）。
    /// </summary>
    public void PlaceOnHold(PatronId patronId)
    {
        if (Status != CopyStatus.Available)
            throw new InvalidOperationException("利用可能な BookCopy のみ取り置きできます。");

        Status = CopyStatus.OnHold;
        HeldBy = patronId;
    }

    /// <summary>
    /// 貸出状態にする（Checkout 時）。
    /// </summary>
    public void Checkout()
    {
        if (Status != CopyStatus.OnHold)
            throw new InvalidOperationException("取り置き中の BookCopy のみ貸出できます。");

        Status = CopyStatus.Loaned;
        HeldBy = null;
    }
}
