using Library.Domain.Copies;
using Library.Domain.Patrons;

namespace Library.Domain.Books;

public record HoldId(Guid Value)
{
    public static HoldId NewId() => new(Guid.NewGuid());
}

public enum HoldStatus
{
    /// <summary>Copy 割当待ち</summary>
    Waiting,

    /// <summary>Copy 割当済み（取り置き中）</summary>
    Assigned,

    /// <summary>貸出完了</summary>
    Fulfilled,

    /// <summary>キャンセル済み</summary>
    Cancelled,
}

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

    /// <summary>
    /// Copy を割り当てる（OnHold 状態へ）。
    /// </summary>
    internal void Assign(CopyId copyId)
    {
        if (Status != HoldStatus.Waiting)
            throw new InvalidOperationException("待機中の予約にのみ Copy を割り当てできます。");

        AssignedCopyId = copyId;
        Status = HoldStatus.Assigned;
    }

    /// <summary>
    /// 貸出完了にする。
    /// </summary>
    public void Fulfill()
    {
        if (Status != HoldStatus.Assigned)
            throw new InvalidOperationException("割当済みの予約のみ完了にできます。");

        Status = HoldStatus.Fulfilled;
    }

    /// <summary>
    /// 予約をキャンセルする。
    /// </summary>
    public void Cancel()
    {
        if (Status is HoldStatus.Fulfilled or HoldStatus.Cancelled)
            throw new InvalidOperationException("完了済みまたはキャンセル済みの予約はキャンセルできません。");

        Status = HoldStatus.Cancelled;
    }
}
