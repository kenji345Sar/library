using Library.Application;
using Library.Domain.Books.Entities;
using Library.Domain.Books.ValueObjects;
using Library.Domain.Copies.Entities;
using Library.Domain.Copies.ValueObjects;
using Library.Domain.Patrons.Entities;
using Library.Domain.Patrons.ValueObjects;
using Library.Infrastructure.Repositories;

// ── 準備 ──

var bookRepo = new InMemoryBookRepository();
var copyRepo = new InMemoryBookCopyRepository();
var patronRepo = new InMemoryPatronRepository();
var checkoutRepo = new InMemoryCheckoutRepository();

var placeHold = new PlaceHoldUseCase(patronRepo, bookRepo, checkoutRepo);
var assignCopy = new AssignCopyUseCase(bookRepo, copyRepo, patronRepo);
var checkoutBook = new CheckoutBookUseCase(copyRepo, bookRepo, checkoutRepo);

// ── データ作成（③ Copy が空いている状態） ──

var book = new Book(BookId.NewId(), new ISBN("978-4-00-000001-0"), "ドメイン駆動設計入門", 3000m);
var copy = new BookCopy(CopyId.NewId(), book.Id, CopyType.Circulating);
var patron = new Patron(PatronId.NewId(), PatronType.Regular);

bookRepo.Add(book);
copyRepo.Add(copy);
patronRepo.Add(patron);

Console.WriteLine("=== 公共図書館システム: 予約 → 割当 → 貸出 ===");
Console.WriteLine();
Console.WriteLine($"書籍:   {book.Title} ({book.ISBN.Value})");
Console.WriteLine($"蔵書:   {copy.Id.Value} ({copy.Type})");
Console.WriteLine($"利用者: {patron.Id.Value} ({patron.Type})");

// ── ① 予約 ──

Console.WriteLine();
Console.WriteLine("--- ① 予約する ---");

var holdId = await placeHold.Execute(patron.Id, book.Id);

Console.WriteLine($"予約作成: HoldId={holdId.Value}");
Console.WriteLine($"  Hold状態: {book.Holds[0].Status}");
Console.WriteLine($"  Copy状態: {copy.Status}");

// ── ④ Copy 割当 ──

Console.WriteLine();
Console.WriteLine("--- ④ Copy を割り当てる ---");

var assignResult = await assignCopy.Execute(copy.Id);

Console.WriteLine($"割当完了: HoldId={assignResult!.Value.HoldId.Value}");
Console.WriteLine($"  Hold状態: {book.Holds[0].Status}");
Console.WriteLine($"  Copy状態: {copy.Status}");
Console.WriteLine($"  取り置き: {copy.HeldBy?.Value}");

// ── ⑥ 貸出 ──

Console.WriteLine();
Console.WriteLine("--- ⑥ 貸出する ---");

var checkoutResult = await checkoutBook.Execute(copy.Id, patron.Id);

Console.WriteLine($"貸出完了: CheckoutId={checkoutResult.CheckoutId.Value}");
Console.WriteLine($"  返却期限: {checkoutResult.DueDate:yyyy-MM-dd}");
Console.WriteLine($"  Hold状態: {book.Holds[0].Status}");
Console.WriteLine($"  Copy状態: {copy.Status}");

Console.WriteLine();
Console.WriteLine("=== 完了 ===");
