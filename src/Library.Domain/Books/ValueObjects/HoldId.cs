namespace Library.Domain.Books.ValueObjects;

public record HoldId(Guid Value)
{
    public static HoldId NewId() => new(Guid.NewGuid());
}

public enum HoldStatus
{
    Waiting,
    Assigned,
    Fulfilled,
    Cancelled,
}
