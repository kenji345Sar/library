using Library.Domain.Books;
using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Checkouts;
using Library.Domain.Patrons;
using Library.Domain.Patrons.Entities;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Application;

/// <summary>
/// ① 予約する（Hold 作成）
/// </summary>
public class PlaceHoldUseCase
{
    private readonly IPatronRepository _patronRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ICheckoutRepository _checkoutRepository;

    public PlaceHoldUseCase(
        IPatronRepository patronRepository,
        IBookRepository bookRepository,
        ICheckoutRepository checkoutRepository)
    {
        _patronRepository = patronRepository;
        _bookRepository = bookRepository;
        _checkoutRepository = checkoutRepository;
    }

    public async Task<HoldId> Execute(PatronId patronId, BookId bookId)
    {
        // 1. 利用者を取得
        Patron patron = await _patronRepository.FindById(patronId)
            ?? throw new InvalidOperationException("利用者が見つかりません。");

        // 2. 有効予約数を取得
        int activeHoldCount = await _bookRepository.CountActiveHoldsByPatron(patronId);

        // 3. 延滞数を取得
        int overdueCount = await _checkoutRepository.CountOverduesByPatron(
            patronId, DateTime.UtcNow);

        // 4. 予約可能か判定（C2: 予約上限, C5: 延滞制限）
        if (!patron.CanPlaceHold(activeHoldCount, overdueCount))
            throw new InvalidOperationException("予約制限により予約できません。");

        // 5. Book を取得
        Book book = await _bookRepository.FindById(bookId)
            ?? throw new InvalidOperationException("書籍が見つかりません。");

        // 6. 予約キューに追加（この時点では Copy 未割当）
        Hold hold = book.PlaceHold(patronId);

        // 7. 保存
        await _bookRepository.Save(book);

        return hold.Id;
    }
}
