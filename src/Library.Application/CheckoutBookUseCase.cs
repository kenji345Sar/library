using Library.Domain.Books;
using Library.Domain.Checkouts;
using Library.Domain.Copies;
using Library.Domain.Patrons;

namespace Library.Application;

/// <summary>
/// ⑥ 貸出（Checkout）
///
/// 入力: CopyId, PatronId（来館した利用者）
/// 処理:
///   1. Copy が OnHold かつ本人のものか確認
///   2. Hold を Fulfilled にする
///   3. Copy を Loaned にする
///   4. Checkout エンティティを作成する
/// </summary>
public class CheckoutBookUseCase
{
    private readonly IBookCopyRepository _bookCopyRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ICheckoutRepository _checkoutRepository;

    public CheckoutBookUseCase(
        IBookCopyRepository bookCopyRepository,
        IBookRepository bookRepository,
        ICheckoutRepository checkoutRepository)
    {
        _bookCopyRepository = bookCopyRepository;
        _bookRepository = bookRepository;
        _checkoutRepository = checkoutRepository;
    }

    public record Command(CopyId CopyId, PatronId PatronId);
    public record Result(CheckoutId CheckoutId, DateTime DueDate);

    public async Task<Result> Execute(Command command)
    {
        // 1. Copy を取得し、OnHold かつ本人か確認
        var copy = await _bookCopyRepository.FindById(command.CopyId)
            ?? throw new InvalidOperationException("蔵書が見つかりません。");

        if (copy.Status != CopyStatus.OnHold)
            throw new InvalidOperationException("取り置き中の蔵書ではありません。");

        if (copy.HeldBy != command.PatronId)
            throw new InvalidOperationException("この蔵書は別の利用者に取り置きされています。");

        // 2. Book から該当 Hold を見つけて Fulfilled にする
        var book = await _bookRepository.FindById(copy.BookId)
            ?? throw new InvalidOperationException("書籍が見つかりません。");

        var hold = book.Holds.FirstOrDefault(
            h => h.AssignedCopyId == copy.Id && h.Status == HoldStatus.Assigned)
            ?? throw new InvalidOperationException("該当する予約が見つかりません。");

        hold.Fulfill();

        // 3. Copy を Loaned にする
        copy.Checkout();

        // 4. Checkout エンティティ作成（C6: 最大 60 日）
        var checkout = new Checkout(
            CheckoutId.NewId(),
            command.PatronId,
            copy.Id,
            DateTime.UtcNow);

        // 5. 保存
        await _bookRepository.Save(book);
        await _bookCopyRepository.Save(copy);
        await _checkoutRepository.Save(checkout);

        return new Result(checkout.Id, checkout.DueDate);
    }
}
