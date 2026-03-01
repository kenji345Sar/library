using Library.Domain.Books.ValueObjects;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Books.Entities;

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

    public Hold PlaceHold(PatronId patronId)
    {
        var hold = new Hold(HoldId.NewId(), patronId, Id);
        _holds.Add(hold);
        return hold;
    }

    public Hold? NextWaitingHold()
    {
        return _holds
            .Where(h => h.Status == HoldStatus.Waiting)
            .OrderBy(h => h.PlacedAt)
            .FirstOrDefault();
    }

    public void AssignCopy(HoldId holdId, CopyId copyId)
    {
        var hold = _holds.FirstOrDefault(h => h.Id == holdId && h.Status == HoldStatus.Waiting)
            ?? throw new InvalidOperationException("該当する待機中の予約が見つかりません。");

        hold.Assign(copyId);
    }
}
