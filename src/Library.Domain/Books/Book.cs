using Library.Domain.Copies;
using Library.Domain.Patrons;

namespace Library.Domain.Books;

public record BookId(Guid Value)
{
    public static BookId NewId() => new(Guid.NewGuid());
}

public record ISBN
{
    public string Value { get; }

    public ISBN(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ISBN は空にできません。", nameof(value));
        Value = value;
    }
}

public class Book
{
    public BookId Id { get; }
    public ISBN ISBN { get; }
    public string Title { get; }
    public decimal Price { get; }

    private readonly List<Hold> _holds = new();
    public IReadOnlyList<Hold> Holds => _holds.AsReadOnly();

    public Book(BookId id, ISBN isbn, string title, decimal price)
    {
        Id = id;
        ISBN = isbn;
        Title = !string.IsNullOrWhiteSpace(title)
            ? title
            : throw new ArgumentException("タイトルは必須です。", nameof(title));
        Price = price > 0
            ? price
            : throw new ArgumentException("価格は正の値である必要があります。", nameof(price));
    }

    /// <summary>
    /// 予約キューに追加する。
    /// この時点では Copy は未割当。
    /// </summary>
    public Hold PlaceHold(PatronId patronId)
    {
        var hold = new Hold(HoldId.NewId(), patronId, Id);
        _holds.Add(hold);
        return hold;
    }

    /// <summary>
    /// 待ちキューの先頭を返す。なければ null。
    /// </summary>
    public Hold? NextWaitingHold()
    {
        return _holds
            .Where(h => h.Status == HoldStatus.Waiting)
            .OrderBy(h => h.PlacedAt)
            .FirstOrDefault();
    }

    /// <summary>
    /// 待ちキュー内の指定 Hold に Copy を割り当てる。
    /// </summary>
    public void AssignCopy(HoldId holdId, CopyId copyId)
    {
        var hold = _holds.FirstOrDefault(h => h.Id == holdId && h.Status == HoldStatus.Waiting)
            ?? throw new InvalidOperationException("該当する待機中の予約が見つかりません。");

        hold.Assign(copyId);
    }
}
