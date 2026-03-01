using Library.Domain.Books;
using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Copies;
using Library.Domain.Copies.Entities;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons;
using Library.Domain.Patrons.Entities;
using Library.Domain.Patrons.ValueObjects;
using Library.Domain.Services;

namespace Library.Application;

/// <summary>
/// ④ Copy を割り当てる
/// </summary>
public class AssignCopyUseCase
{
    private readonly IBookRepository _bookRepository;
    private readonly IBookCopyRepository _bookCopyRepository;
    private readonly IPatronRepository _patronRepository;

    public AssignCopyUseCase(
        IBookRepository bookRepository,
        IBookCopyRepository bookCopyRepository,
        IPatronRepository patronRepository)
    {
        _bookRepository = bookRepository;
        _bookCopyRepository = bookCopyRepository;
        _patronRepository = patronRepository;
    }

    public async Task<(HoldId HoldId, PatronId PatronId)?> Execute(CopyId copyId)
    {
        // 1. Copy を取得
        BookCopy copy = await _bookCopyRepository.FindById(copyId)
            ?? throw new InvalidOperationException("蔵書が見つかりません。");

        if (copy.Status != CopyStatus.Available)
            throw new InvalidOperationException("利用可能な蔵書ではありません。");

        // 2. Book を取得し、待ちキューの先頭を確認
        Book book = await _bookRepository.FindById(copy.BookId)
            ?? throw new InvalidOperationException("書籍が見つかりません。");

        Hold? hold = book.NextWaitingHold();
        if (hold is null)
            return null;

        // 3. Restricted チェック（C3: Researcher のみ）
        //    Copy と Patron の2つの集約にまたがるルールなので Domain Service に聞く
        if (copy.Type == CopyType.Restricted)
        {
            Patron patron = await _patronRepository.FindById(hold.PatronId)
                ?? throw new InvalidOperationException("利用者が見つかりません。");

            RestrictedBookPolicy.EnsureCanAssign(copy, patron);
        }

        // 4. 割当
        book.AssignCopy(hold.Id, copy.Id);
        copy.PlaceOnHold(hold.PatronId);

        // 5. 保存
        await _bookRepository.Save(book);
        await _bookCopyRepository.Save(copy);

        return (hold.Id, hold.PatronId);
    }
}
