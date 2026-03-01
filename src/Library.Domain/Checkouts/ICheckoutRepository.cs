using Library.Domain.Checkouts.Entities;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Domain.Checkouts;

public interface ICheckoutRepository
{
    Task Save(Checkout checkout);
    Task<int> CountOverduesByPatron(PatronId patronId, DateTime asOf);
}
