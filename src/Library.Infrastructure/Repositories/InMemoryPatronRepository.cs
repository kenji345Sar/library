using Library.Domain.Patrons;
using Library.Domain.Patrons.Entities;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Infrastructure.Repositories;

public class InMemoryPatronRepository : IPatronRepository
{
    private readonly Dictionary<PatronId, Patron> _patrons = new();

    public void Add(Patron patron) => _patrons[patron.Id] = patron;

    public Task<Patron?> FindById(PatronId id)
        => Task.FromResult(_patrons.GetValueOrDefault(id));
}
