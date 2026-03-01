using Library.Domain.Books;
using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Checkouts;
using Library.Domain.Checkouts.Entities;
using Library.Domain.Checkouts.ValueObjects;
using Library.Domain.Copies;
using Library.Domain.Copies.Entities;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons.ValueObjects;

namespace Library.Application;

/// <summary>
/// ⑥ 貸出（Checkout）
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

    public async Task<(CheckoutId CheckoutId, DateTime DueDate)> Execute(CopyId copyId, PatronId patronId)
    {
        // 1. Copy を取得し、OnHold かつ本人か確認
        BookCopy copy = await _bookCopyRepository.FindById(copyId)
            ?? throw new InvalidOperationException("蔵書が見つかりません。");

        if (copy.Status != CopyStatus.OnHold)
            throw new InvalidOperationException("取り置き中の蔵書ではありません。");

        if (copy.HeldBy != patronId)
            throw new InvalidOperationException("この蔵書は別の利用者に取り置きされています。");

        // 2. Book から該当 Hold を見つけて Fulfilled にする
        Book book = await _bookRepository.FindById(copy.BookId)
            ?? throw new InvalidOperationException("書籍が見つかりません。");

        Hold hold = book.Holds.FirstOrDefault(
            h => h.AssignedCopyId == copy.Id && h.Status == HoldStatus.Assigned)
            ?? throw new InvalidOperationException("該当する予約が見つかりません。");

        hold.Fulfill();

        // 3. Copy を Loaned にする
        copy.Checkout();

        // 4. Checkout エンティティ作成（C6: 最大 60 日）
        Checkout checkout = new Checkout(
            CheckoutId.NewId(),
            patronId,
            copy.Id,
            DateTime.UtcNow);

        // 5. 保存
        await _bookRepository.Save(book);
        await _bookCopyRepository.Save(copy);
        await _checkoutRepository.Save(checkout);

        return (checkout.Id, checkout.DueDate);
    }
}
