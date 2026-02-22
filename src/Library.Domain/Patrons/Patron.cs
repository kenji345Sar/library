namespace Library.Domain.Patrons;

public record PatronId(Guid Value)
{
    public static PatronId NewId() => new(Guid.NewGuid());
}

public enum PatronType
{
    Regular,
    Researcher,
}

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

    /// <summary>
    /// 予約可能かどうかを判定する。
    /// </summary>
    /// <param name="activeHoldCount">現在の有効予約数</param>
    /// <param name="overdueCount">現在の延滞数（同支店）</param>
    public bool CanPlaceHold(int activeHoldCount, int overdueCount)
    {
        // C5: 延滞 2 件超で予約拒否
        if (overdueCount > MaxOverduesBeforeBlock)
            return false;

        // C2: Regular は最大 5 件
        if (Type == PatronType.Regular && activeHoldCount >= MaxHoldsForRegular)
            return false;

        return true;
    }

    /// <summary>
    /// Restricted 本を予約できるか（C3: Researcher のみ）。
    /// </summary>
    public bool CanHoldRestricted() => IsResearcher;
}
