using Library.Domain.Copies.Entities;
using Library.Domain.Patrons.Entities;

namespace Library.Domain.Services;

/// <summary>
/// Restricted 本の割当ルール。
///
/// Copy と Patron の2つの集約にまたがる判定なので、
/// どちらのエンティティにも置けない → Domain Service にする。
///
/// - copy.Type が Restricted かどうかは Copy が知っている
/// - patron が Researcher かどうかは Patron が知っている
/// - 「Restricted なら Researcher のみ」という組み合わせルールはどちらにも属さない
/// </summary>
public static class RestrictedBookPolicy
{
    public static void EnsureCanAssign(BookCopy copy, Patron patron)
    {
        if (copy.Type == Copies.ValueObjects.CopyType.Restricted && !patron.CanHoldRestricted())
            throw new InvalidOperationException("Restricted 本は Researcher のみ予約できます。");
    }
}
