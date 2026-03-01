using Library.Application;
using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Checkouts.Entities;
using Library.Domain.Checkouts.ValueObjects;
using Library.Domain.Copies.Entities;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons.Entities;
using Library.Domain.Patrons.ValueObjects;
using Library.Infrastructure.Repositories;

namespace Library.Tests;

public class HoldToCheckoutFlowTests
{
    private readonly InMemoryBookRepository _bookRepo = new();
    private readonly InMemoryBookCopyRepository _copyRepo = new();
    private readonly InMemoryPatronRepository _patronRepo = new();
    private readonly InMemoryCheckoutRepository _checkoutRepo = new();

    private readonly PlaceHoldUseCase _placeHold;
    private readonly AssignCopyUseCase _assignCopy;
    private readonly CheckoutBookUseCase _checkoutBook;

    public HoldToCheckoutFlowTests()
    {
        _placeHold = new PlaceHoldUseCase(_patronRepo, _bookRepo, _checkoutRepo);
        _assignCopy = new AssignCopyUseCase(_bookRepo, _copyRepo, _patronRepo);
        _checkoutBook = new CheckoutBookUseCase(_copyRepo, _bookRepo, _checkoutRepo);
    }

    private (Book book, BookCopy copy, Patron patron) SetupStandard()
    {
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000001-0"), "ドメイン駆動設計入門", 3000m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Circulating);
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);

        _bookRepo.Add(book);
        _copyRepo.Add(copy);
        _patronRepo.Add(patron);

        return (book, copy, patron);
    }

    [Fact]
    public async Task 予約して割当されて貸出できる()
    {
        var (book, copy, patron) = SetupStandard();

        var holdId = await _placeHold.Execute(patron.Id, book.Id);
        Assert.NotNull(holdId);
        Assert.Single(book.Holds);
        Assert.Equal(HoldStatus.Waiting, book.Holds[0].Status);

        var assignResult = await _assignCopy.Execute(copy.Id);
        Assert.NotNull(assignResult);
        Assert.Equal(patron.Id, assignResult.Value.PatronId);
        Assert.Equal(HoldStatus.Assigned, book.Holds[0].Status);
        Assert.Equal(CopyStatus.OnHold, copy.Status);

        var checkoutResult = await _checkoutBook.Execute(copy.Id, patron.Id);
        Assert.Equal(HoldStatus.Fulfilled, book.Holds[0].Status);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
        Assert.Single(_checkoutRepo.All);
    }

    [Fact]
    public async Task 予約がなければ割当されない()
    {
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000002-0"), "クリーンアーキテクチャ", 3500m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Circulating);
        _bookRepo.Add(book);
        _copyRepo.Add(copy);

        var result = await _assignCopy.Execute(copy.Id);
        Assert.Null(result);
        Assert.Equal(CopyStatus.Available, copy.Status);
    }

    [Fact]
    public async Task Regular利用者は5件を超えて予約できない()
    {
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);
        _patronRepo.Add(patron);

        for (var i = 0; i < 5; i++)
        {
            var b = new Book(BookId.NewId(), new ISBN($"978-4-00-00000{i}-0"), $"本{i}", 1000m);
            _bookRepo.Add(b);
            await _placeHold.Execute(patron.Id, b.Id);
        }

        var sixthBook = new Book(BookId.NewId(), new ISBN("978-4-00-000099-0"), "6冊目", 1000m);
        _bookRepo.Add(sixthBook);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _placeHold.Execute(patron.Id, sixthBook.Id));
        Assert.Contains("予約制限", ex.Message);
    }

    [Fact]
    public async Task 延滞が2件を超えると予約できない()
    {
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);
        _patronRepo.Add(patron);

        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000010-0"), "テスト本", 2000m);
        _bookRepo.Add(book);

        for (var i = 0; i < 3; i++)
        {
            var checkout = new Checkout(
                CheckoutId.NewId(), patron.Id, CopyId.NewId(),
                DateTime.UtcNow.AddDays(-61));
            await _checkoutRepo.Save(checkout);
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _placeHold.Execute(patron.Id, book.Id));
        Assert.Contains("予約制限", ex.Message);
    }

    [Fact]
    public async Task Restricted本はRegular利用者に割当できない()
    {
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000020-0"), "希少本", 50000m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Restricted);
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);

        _bookRepo.Add(book);
        _copyRepo.Add(copy);
        _patronRepo.Add(patron);

        await _placeHold.Execute(patron.Id, book.Id);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _assignCopy.Execute(copy.Id));
        Assert.Contains("Researcher", ex.Message);
    }

    [Fact]
    public async Task ResearcherはRestricted本を割当して貸出できる()
    {
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000030-0"), "研究資料", 80000m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Restricted);
        var patron = new Patron(PatronId.NewId(), PatronType.Researcher);

        _bookRepo.Add(book);
        _copyRepo.Add(copy);
        _patronRepo.Add(patron);

        await _placeHold.Execute(patron.Id, book.Id);
        await _assignCopy.Execute(copy.Id);
        var result = await _checkoutBook.Execute(copy.Id, patron.Id);

        Assert.Equal(CopyStatus.Loaned, copy.Status);
    }
}
