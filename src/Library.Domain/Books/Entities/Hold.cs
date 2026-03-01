using Library.Domain.Books.ValueObjects;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Books.Entities;

public class Hold
{
    public HoldId Id { get; }
    public PatronId PatronId { get; }
    public BookId BookId { get; }
    public CopyId? AssignedCopyId { get; private set; }
    public HoldStatus Status { get; private set; }
    public DateTime PlacedAt { get; }

    internal Hold(HoldId id, PatronId patronId, BookId bookId)
    {
        Id = id;
        PatronId = patronId;
        BookId = bookId;
        Status = HoldStatus.Waiting;
        PlacedAt = DateTime.UtcNow;
    }

    internal void Assign(CopyId copyId)
    {
        if (Status != HoldStatus.Waiting)
            throw new InvalidOperationException("待機中の予約にのみ Copy を割り当てできます。");

        AssignedCopyId = copyId;
        Status = HoldStatus.Assigned;
    }

    public void Fulfill()
    {
        if (Status != HoldStatus.Assigned)
            throw new InvalidOperationException("割当済みの予約のみ完了にできます。");

        Status = HoldStatus.Fulfilled;
    }

    public void Cancel()
    {
        if (Status is HoldStatus.Fulfilled or HoldStatus.Cancelled)
            throw new InvalidOperationException("完了済みまたはキャンセル済みの予約はキャンセルできません。");

        Status = HoldStatus.Cancelled;
    }
}
