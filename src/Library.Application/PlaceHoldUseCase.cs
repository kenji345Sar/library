using Library.Domain.Books;
using Library.Domain.Checkouts;
using Library.Domain.Patrons;

namespace Library.Application;

/// <summary>
/// ① 予約する（Hold 作成）
///
/// 入力: PatronId, BookId
/// 処理:
///   1. 利用者は存在するか
///   2. Regular なら現在予約数 &lt; 5 か
///   3. 延滞制限に引っかかっていないか
///   4. Book に Hold を追加（キューの最後）
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

    public record Command(PatronId PatronId, BookId BookId);
    public record Result(HoldId HoldId);

    public async Task<Result> Execute(Command command)
    {
        // 1. 利用者を取得
        var patron = await _patronRepository.FindById(command.PatronId)
            ?? throw new InvalidOperationException("利用者が見つかりません。");

        // 2. 有効予約数を取得
        var activeHoldCount = await _bookRepository.CountActiveHoldsByPatron(command.PatronId);

        // 3. 延滞数を取得
        var overdueCount = await _checkoutRepository.CountOverduesByPatron(
            command.PatronId, DateTime.UtcNow);

        // 4. 予約可能か判定（C2: 予約上限, C5: 延滞制限）
        if (!patron.CanPlaceHold(activeHoldCount, overdueCount))
            throw new InvalidOperationException("予約制限により予約できません。");

        // 5. Book を取得
        var book = await _bookRepository.FindById(command.BookId)
            ?? throw new InvalidOperationException("書籍が見つかりません。");

        // 6. 予約キューに追加（この時点では Copy 未割当）
        var hold = book.PlaceHold(command.PatronId);

        // 7. 保存
        await _bookRepository.Save(book);

        return new Result(hold.Id);
    }
}
