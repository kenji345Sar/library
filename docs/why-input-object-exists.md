# なぜユースケースの引数をオブジェクトで包むパターンがあるのか

## 今のコード（直接引数）

```csharp
// シンプル。ただの引数。
var holdId = await placeHold.Execute(patron.Id, book.Id);
```

```csharp
public async Task<HoldId> Execute(PatronId patronId, BookId bookId)
{
    // patronId, bookId をそのまま使う
}
```

これで十分。今はこの形にしている。

---

## 包むパターン（以前のコード）

```csharp
// わざわざオブジェクトを作って渡す
var holdId = await placeHold.Execute(new PlaceHoldInput(patron.Id, book.Id));
```

```csharp
public record PlaceHoldInput(PatronId PatronId, BookId BookId);

public async Task<HoldId> Execute(PlaceHoldInput input)
{
    // input.PatronId, input.BookId で取り出す
}
```

やっていることは同じ。袋に入れて渡して、中で取り出しているだけ。

---

## なぜ包むパターンが存在するのか

直接引数で困る場面が出てきたときに必要になる。

### 1. 引数が多すぎる

```csharp
// 引数が 6 個。どれが何かわからない。順番を間違えるリスクもある。
await useCase.Execute(patronId, bookId, copyId, branchId, startDate, endDate);

// オブジェクトにまとめると名前がつく
await useCase.Execute(new TransferInput(
    PatronId: patronId,
    BookId: bookId,
    CopyId: copyId,
    BranchId: branchId,
    StartDate: startDate,
    EndDate: endDate));
```

### 2. コマンドをキューに入れて後で実行したい

```csharp
// オブジェクトなら保存できる
var input = new PlaceHoldInput(patronId, bookId);
queue.Enqueue(input);  // キューに入れて後で実行

// 直接引数だとキューに入れられない
```

### 3. ログや監査に記録したい

```csharp
// オブジェクトならそのまま記録できる
logger.Log($"実行: {input}");

// 直接引数だと1個ずつ書く必要がある
logger.Log($"実行: patronId={patronId}, bookId={bookId}");
```

### 4. バリデーションを入力側に持たせたい

```csharp
public record PlaceHoldInput(PatronId PatronId, BookId BookId)
{
    // 作成時にバリデーション
    public PlaceHoldInput
    {
        if (PatronId is null) throw new ArgumentNullException(nameof(PatronId));
        if (BookId is null) throw new ArgumentNullException(nameof(BookId));
    }
}
```

---

## 判断基準

| 状況 | 選択 |
|------|------|
| 引数が 1〜3 個 | 直接引数でいい |
| 引数が 4 個以上 | オブジェクトにまとめた方が読みやすい |
| キューやログに使いたい | オブジェクトが必要 |
| 上記のどれも当てはまらない | 直接引数でいい |

---

## 今のプロジェクトの方針

- 引数は少ない（最大 2 個）
- キューもログも使っていない
- なので**直接引数**にしている
- 上記の必要が出てきたら、そのときにオブジェクトに変える
