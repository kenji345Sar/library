using Library.Domain.Copies;
using Library.Domain.Patrons;

namespace Library.Domain.Checkouts;

public record CheckoutId(Guid Value)
{
    public static CheckoutId NewId() => new(Guid.NewGuid());
}

public class Checkout
{
    public const int MaxLoanDays = 60;

    public CheckoutId Id { get; }
    public PatronId PatronId { get; }
    public CopyId CopyId { get; }
    public DateTime CheckedOutAt { get; }
    public DateTime DueDate { get; }

    public Checkout(CheckoutId id, PatronId patronId, CopyId copyId, DateTime checkedOutAt)
    {
        Id = id;
        PatronId = patronId;
        CopyId = copyId;
        CheckedOutAt = checkedOutAt;
        DueDate = checkedOutAt.AddDays(MaxLoanDays);
    }

    /// <summary>
    /// 延滞しているかどうか。
    /// </summary>
    public bool IsOverdue(DateTime now) => now > DueDate;
}
