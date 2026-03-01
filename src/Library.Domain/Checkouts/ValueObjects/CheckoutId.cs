namespace Library.Domain.Checkouts.ValueObjects;

public record CheckoutId(Guid Value)
{
    public static CheckoutId NewId() => new(Guid.NewGuid());
}
