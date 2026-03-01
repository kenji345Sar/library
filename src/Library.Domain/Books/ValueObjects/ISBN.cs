namespace Library.Domain.Books.ValueObjects;

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
