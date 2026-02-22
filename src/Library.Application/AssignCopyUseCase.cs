using Library.Domain.Books;
using Library.Domain.Copies;
using Library.Domain.Patrons;

namespace Library.Application;

/// <summary>
/// ④ Copy を割り当てる
///
/// トリガー: Copy が Available になった（返却 or 新規登録）
/// 処理:
///   1. その Copy の Book を確認
///   2. 予約キューの先頭を取得
///   3. Restricted なら Researcher か確認
///   4. Hold に Copy を紐づけ、Copy を OnHold へ
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

    public record Command(CopyId CopyId);
    public record Result(HoldId HoldId, PatronId PatronId);

    /// <summary>
    /// 割当を試みる。待ちキューが空なら null を返す。
    /// </summary>
    public async Task<Result?> Execute(Command command)
    {
        // 1. Copy を取得
        var copy = await _bookCopyRepository.FindById(command.CopyId)
            ?? throw new InvalidOperationException("蔵書が見つかりません。");

        if (copy.Status != CopyStatus.Available)
            throw new InvalidOperationException("利用可能な蔵書ではありません。");

        // 2. Book を取得し、待ちキューの先頭を確認
        var book = await _bookRepository.FindById(copy.BookId)
            ?? throw new InvalidOperationException("書籍が見つかりません。");

        var hold = book.NextWaitingHold();
        if (hold is null)
            return null; // 待ちなし

        // 3. Restricted チェック（C3: Researcher のみ）
        if (copy.Type == CopyType.Restricted)
        {
            var patron = await _patronRepository.FindById(hold.PatronId)
                ?? throw new InvalidOperationException("利用者が見つかりません。");

            if (!patron.CanHoldRestricted())
                throw new InvalidOperationException("Restricted 本は Researcher のみ予約できます。");
        }

        // 4. 割当: Hold に Copy を紐づけ、Copy を OnHold へ
        book.AssignCopy(hold.Id, copy.Id);
        copy.PlaceOnHold(hold.PatronId);

        // 5. 保存
        await _bookRepository.Save(book);
        await _bookCopyRepository.Save(copy);

        return new Result(hold.Id, hold.PatronId);
    }
}
