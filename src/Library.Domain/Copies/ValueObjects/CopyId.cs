namespace Library.Domain.Copies.ValueObjects;

public record CopyId(Guid Value)
{
    public static CopyId NewId() => new(Guid.NewGuid());
}

public enum CopyType
{
    Circulating,
    Restricted,
}

public enum CopyStatus
{
    Available,
    OnHold,
    Loaned,
}
