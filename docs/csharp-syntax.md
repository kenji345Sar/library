# C# の記法メモ

## `=>` の省略記法（式本体メンバー）

中身が 1 行だけのメソッドやプロパティで `{ return ... }` を省略できる書き方。

```csharp
// 省略記法
public Task<BookCopy?> FindById(CopyId id)
    => Task.FromResult(_copies.GetValueOrDefault(id));

// 普通に書くと同じ意味
public Task<BookCopy?> FindById(CopyId id)
{
    return Task.FromResult(_copies.GetValueOrDefault(id));
}
```

見た目が関数型に似ているが、ただの省略記法であり関数型ではない。

---

## 関数型の書き方（ラムダ式 + LINQ）

「条件や処理をメソッドに渡す」のが関数型の特徴。

このプロジェクトでの実例（`InMemoryCheckoutRepository.cs`）:

```csharp
// 関数型の書き方（LINQ）
var count = _checkouts.Count(c => c.PatronId == patronId && c.IsOverdue(asOf));
```

`c => ...` の部分がラムダ式。「この条件に合うものを数えて」と Count メソッドに渡している。

普通に書くとこうなる:

```csharp
// 普通の書き方（for ループ）
var count = 0;
foreach (var c in _checkouts)
{
    if (c.PatronId == patronId && c.IsOverdue(asOf))
    {
        count++;
    }
}
```

結果は同じだが:

- **関数型**: 「何を数えるか」だけを書く
- **for ループ**: 「どうやってループして数えるか」も書く

---

## 違いのまとめ

| 書き方 | 例 | 何か |
|--------|---|------|
| `=>` 省略記法 | `public int Foo() => 42;` | メソッドの `{ return ... }` を省略しただけ |
| `=>` ラムダ式 | `c => c.IsOverdue(asOf)` | 条件や処理をメソッドに渡す関数型の書き方 |

見た目は同じ `=>` だが意味が違う。

---

## `var` と明示的な型

C# では `var` で書くのが一般的。右辺を見れば型がわかるので、左辺に重複して書く必要がない。

```csharp
// var（一般的な書き方）
var copy = await _bookCopyRepository.FindById(copyId);
var book = new Book(BookId.NewId(), new ISBN("978-4-00-000001-0"), "ドメイン駆動設計入門", 3000m);

// 明示的な型（同じ情報を二度書いている）
BookCopy copy = await _bookCopyRepository.FindById(copyId);
Book book = new Book(BookId.NewId(), new ISBN("978-4-00-000001-0"), "ドメイン駆動設計入門", 3000m);
```

このプロジェクトでは学習のために明示的な型で書いている。慣れたら `var` に戻して問題ない。
