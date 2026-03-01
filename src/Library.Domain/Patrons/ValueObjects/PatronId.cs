namespace Library.Domain.Patrons.ValueObjects;

public record PatronId(Guid Value)
{
    public static PatronId NewId() => new(Guid.NewGuid());
}

public enum PatronType
{
    Regular,
    Researcher,
}
