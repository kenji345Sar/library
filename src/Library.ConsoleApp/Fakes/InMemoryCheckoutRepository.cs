using Library.Domain.Checkouts;
using Library.Domain.Patrons;

namespace Library.ConsoleApp.Fakes;

public class InMemoryCheckoutRepository : ICheckoutRepository
{
    private readonly List<Checkout> _checkouts = new();

    public Task Save(Checkout checkout)
    {
        _checkouts.Add(checkout);
        return Task.CompletedTask;
    }

    public Task<int> CountOverduesByPatron(PatronId patronId, DateTime asOf)
    {
        var count = _checkouts.Count(c => c.PatronId == patronId && c.IsOverdue(asOf));
        return Task.FromResult(count);
    }
}
