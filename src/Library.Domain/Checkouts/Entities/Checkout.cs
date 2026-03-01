using Library.Domain.Copies.ValueObjects;
using Library.Domain.Checkouts.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Checkouts.Entities;

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

    public bool IsOverdue(DateTime now) => now > DueDate;
}
