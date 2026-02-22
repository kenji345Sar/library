using Library.Application;
using Library.Domain.Books;
using Library.Domain.Checkouts;
using Library.Domain.Copies;
using Library.Domain.Patrons;
using Library.Tests.Fakes;

namespace Library.Tests;

/// <summary>
/// 予約 → 割当 → 貸出 の一連フローをテストする。
/// </summary>
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

    // ─── ヘルパー ───

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

    // ─── 正常系: 予約 → 割当 → 貸出 ───

    [Fact]
    public async Task 予約して割当されて貸出できる()
    {
        // Arrange
        var (book, copy, patron) = SetupStandard();

        // Act ① 予約
        var holdResult = await _placeHold.Execute(
            new PlaceHoldUseCase.Command(patron.Id, book.Id));

        Assert.NotNull(holdResult);
        Assert.Single(book.Holds);
        Assert.Equal(HoldStatus.Waiting, book.Holds[0].Status);

        // Act ④ Copy 割当
        var assignResult = await _assignCopy.Execute(
            new AssignCopyUseCase.Command(copy.Id));

        Assert.NotNull(assignResult);
        Assert.Equal(patron.Id, assignResult.PatronId);
        Assert.Equal(HoldStatus.Assigned, book.Holds[0].Status);
        Assert.Equal(CopyStatus.OnHold, copy.Status);

        // Act ⑥ 貸出
        var checkoutResult = await _checkoutBook.Execute(
            new CheckoutBookUseCase.Command(copy.Id, patron.Id));

        Assert.NotNull(checkoutResult);
        Assert.Equal(HoldStatus.Fulfilled, book.Holds[0].Status);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
        Assert.Single(_checkoutRepo.All);
    }

    // ─── 割当: 待ちキューが空なら割当されない ───

    [Fact]
    public async Task 予約がなければ割当されない()
    {
        // Arrange: 予約なしの Book + Copy
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000002-0"), "クリーンアーキテクチャ", 3500m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Circulating);
        _bookRepo.Add(book);
        _copyRepo.Add(copy);

        // Act
        var result = await _assignCopy.Execute(
            new AssignCopyUseCase.Command(copy.Id));

        // Assert: null（待ちなし）
        Assert.Null(result);
        Assert.Equal(CopyStatus.Available, copy.Status);
    }

    // ─── 予約制限: Regular は 5 件超えたら予約できない（C2） ───

    [Fact]
    public async Task Regular利用者は5件を超えて予約できない()
    {
        // Arrange
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);
        _patronRepo.Add(patron);

        // 5 冊分の Book を作り、それぞれ予約する
        for (var i = 0; i < 5; i++)
        {
            var b = new Book(BookId.NewId(), new ISBN($"978-4-00-00000{i}-0"), $"本{i}", 1000m);
            _bookRepo.Add(b);
            await _placeHold.Execute(new PlaceHoldUseCase.Command(patron.Id, b.Id));
        }

        // 6 冊目
        var sixthBook = new Book(BookId.NewId(), new ISBN("978-4-00-000099-0"), "6冊目", 1000m);
        _bookRepo.Add(sixthBook);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _placeHold.Execute(new PlaceHoldUseCase.Command(patron.Id, sixthBook.Id)));

        Assert.Contains("予約制限", ex.Message);
    }

    // ─── 延滞制限: 延滞 2 件超で予約拒否（C5） ───

    [Fact]
    public async Task 延滞が2件を超えると予約できない()
    {
        // Arrange
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);
        _patronRepo.Add(patron);

        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000010-0"), "テスト本", 2000m);
        _bookRepo.Add(book);

        // 延滞中の Checkout を 3 件作る（61 日前に借りた → 延滞）
        for (var i = 0; i < 3; i++)
        {
            var checkout = new Checkout(
                CheckoutId.NewId(), patron.Id, CopyId.NewId(),
                DateTime.UtcNow.AddDays(-61));
            await _checkoutRepo.Save(checkout);
        }

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _placeHold.Execute(new PlaceHoldUseCase.Command(patron.Id, book.Id)));

        Assert.Contains("予約制限", ex.Message);
    }

    // ─── Restricted 本は Researcher のみ割当可能（C3） ───

    [Fact]
    public async Task Restricted本はRegular利用者に割当できない()
    {
        // Arrange
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000020-0"), "希少本", 50000m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Restricted);
        var patron = new Patron(PatronId.NewId(), PatronType.Regular);

        _bookRepo.Add(book);
        _copyRepo.Add(copy);
        _patronRepo.Add(patron);

        // 予約はキューに入る（この時点では Copy 種別は関係ない）
        await _placeHold.Execute(new PlaceHoldUseCase.Command(patron.Id, book.Id));

        // Act & Assert: 割当時に Restricted チェックが効く
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _assignCopy.Execute(new AssignCopyUseCase.Command(copy.Id)));

        Assert.Contains("Researcher", ex.Message);
    }

    // ─── Researcher は Restricted 本も割当可能 ───

    [Fact]
    public async Task ResearcherはRestricted本を割当して貸出できる()
    {
        // Arrange
        var book = new Book(BookId.NewId(), new ISBN("978-4-00-000030-0"), "研究資料", 80000m);
        var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Restricted);
        var patron = new Patron(PatronId.NewId(), PatronType.Researcher);

        _bookRepo.Add(book);
        _copyRepo.Add(copy);
        _patronRepo.Add(patron);

        // Act: 予約 → 割当 → 貸出
        await _placeHold.Execute(new PlaceHoldUseCase.Command(patron.Id, book.Id));
        await _assignCopy.Execute(new AssignCopyUseCase.Command(copy.Id));
        var result = await _checkoutBook.Execute(
            new CheckoutBookUseCase.Command(copy.Id, patron.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
    }
}
