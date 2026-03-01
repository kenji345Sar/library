using Library.Domain.Checkouts;
using Library.Domain.Checkouts.Entities;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Infrastructure.Repositories;

public class InMemoryCheckoutRepository : ICheckoutRepository
{
    private readonly List<Checkout> _checkouts = new();

    public IReadOnlyList<Checkout> All => _checkouts.AsReadOnly();

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
