using Library.Domain.Patrons;

namespace Library.Domain.Checkouts;

public interface ICheckoutRepository
{
    Task Save(Checkout checkout);

    /// <summary>
    /// 指定 Patron の延滞数を返す。
    /// </summary>
    Task<int> CountOverduesByPatron(PatronId patronId, DateTime asOf);
}
