namespace Library.Domain.Patrons;

public interface IPatronRepository
{
    Task<Patron?> FindById(PatronId id);
}
