using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Patrons.Entities;

public class Patron
{
    private const int MaxHoldsForRegular = 5;
    private const int MaxOverduesBeforeBlock = 2;

    public PatronId Id { get; }
    public PatronType Type { get; }

    public Patron(PatronId id, PatronType type)
    {
        Id = id;
        Type = type;
    }

    public bool IsResearcher => Type == PatronType.Researcher;

    public bool CanPlaceHold(int activeHoldCount, int overdueCount)
    {
        if (overdueCount > MaxOverduesBeforeBlock)
            return false;

        if (Type == PatronType.Regular && activeHoldCount >= MaxHoldsForRegular)
            return false;

        return true;
    }

    public bool CanHoldRestricted() => IsResearcher;
}
