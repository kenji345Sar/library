namespace Library.Domain.Books.ValueObjects;

public record BookId(Guid Value)
{
    public static BookId NewId() => new(Guid.NewGuid());
}
