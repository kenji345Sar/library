# 集約・境界・Value Object

## 1. 集約の構成

本プロジェクトは 4 つの集約に分かれている。

```
Book 集約           Patron 集約        BookCopy 集約     Checkout 集約
┌──────────┐       ┌──────────┐       ┌──────────┐     ┌──────────┐
│ Book     │       │ Patron   │       │ BookCopy │     │ Checkout │
│  └ Hold  │       │          │       │          │     │          │
│  └ Hold  │       └──────────┘       └──────────┘     └──────────┘
│  └ Hold  │
└──────────┘
```

| 集約 | ルート | 内包するエンティティ | 理由 |
|------|--------|----------------------|------|
| Book | Book | Hold（複数） | 予約キューの順番を Book が保証する |
| Patron | Patron | なし | 予約上限チェックは Patron 自身の責任 |
| BookCopy | BookCopy | なし | 状態遷移（Available → OnHold → Loaned）は Copy 自身の責任 |
| Checkout | Checkout | なし | 貸出記録は他の集約と別ライフサイクル |

### なぜ Hold は Book の中か

予約キュー（誰が何番目か）は Book の責任である。Hold を別の集約にすると、キューの順番を 1 つのトランザクションで保証できなくなる。

```csharp
// Book がキューを管理する
book.PlaceHold(patronId);          // キューに追加
book.NextWaitingHold();            // 先頭を取得
book.AssignCopy(holdId, copyId);   // 割当
```

---

## 2. 境界（Aggregate Boundary）

集約の境界 = **内側は整合性を保証する、外側とは ID で参照する**。

### 集約の内側（直接参照）

```csharp
// Book は Hold を直接持ち、状態を操作できる
book.Holds[0].Fulfill();
```

### 集約の外側（ID で参照）

```csharp
// Hold は Patron を ID でしか知らない
public PatronId PatronId { get; }

// BookCopy は Book を ID でしか知らない
public BookId BookId { get; }
```

### 境界をまたぐ場面（ユースケースが調整する）

AssignCopyUseCase が典型例:

```csharp
var copy   = await _bookCopyRepository.FindById(copyId);      // BookCopy 集約
var book   = await _bookRepository.FindById(copy.BookId);      // Book 集約
var patron = await _patronRepository.FindById(hold.PatronId);  // Patron 集約

book.AssignCopy(hold.Id, copy.Id);   // Book 集約内で Hold の状態を変える
copy.PlaceOnHold(hold.PatronId);     // BookCopy 集約の状態を変える
```

**集約の中のことは集約に任せる。集約をまたぐ調整はユースケースがやる。**

---

## 3. Value Object

### ID 系（record で定義）

| Value Object | 定義場所 | 内容 |
|-------------|----------|------|
| BookId | Books/Book.cs | `record BookId(Guid Value)` |
| ISBN | Books/Book.cs | `record ISBN` + 空文字バリデーション |
| HoldId | Books/Hold.cs | `record HoldId(Guid Value)` |
| CopyId | Copies/BookCopy.cs | `record CopyId(Guid Value)` |
| PatronId | Patrons/Patron.cs | `record PatronId(Guid Value)` |
| CheckoutId | Checkouts/Checkout.cs | `record CheckoutId(Guid Value)` |

### record とは

C# の `record` は、**データを入れるための型**を1行で定義できる仕組み。

```csharp
// record で書く（1行）
public record BookId(Guid Value);

// class で同じことを書くと長い
public class BookId
{
    public Guid Value { get; }
    public BookId(Guid value) { Value = value; }
    // Equals, GetHashCode, ToString も自分で書く必要がある
}
```

record が自動で提供するもの:

| 機能 | 説明 |
|------|------|
| コンストラクタ + プロパティ | `(Guid Value)` だけで両方作られる |
| イミュータブル | 作成後に値を変更できない |
| 構造的等価性 | 中身が同じなら `==` で `true` になる |

```csharp
var a = new BookId(Guid.Parse("..."));
var b = new BookId(Guid.Parse("..."));

// class → a == b は false（別のオブジェクトだから）
// record → a == b は true（中身が同じだから）
```

Value Object は「値が同じなら同じ」なので、record が適している。

### enum 系

| enum | 定義場所 | 値 |
|------|----------|-----|
| HoldStatus | Books/Hold.cs | Waiting, Assigned, Fulfilled, Cancelled |
| CopyType | Copies/BookCopy.cs | Circulating, Restricted |
| CopyStatus | Copies/BookCopy.cs | Available, OnHold, Loaned |
| PatronType | Patrons/Patron.cs | Regular, Researcher |

### Entity との違い

| | Value Object | Entity |
|---|---|---|
| 同一性 | **値**が同じなら同じ | **ID** で識別する |
| 可変性 | 不変（immutable） | 状態が変わる |
| 例 | BookId, ISBN, PatronId | Book, Hold, BookCopy |

### ファイル配置について

現時点では Value Object はエンティティと同じファイルに同居している。Value Object にロジックが増えた場合はファイルを分離する。

---

## 4. 集約設計の判断基準：整合性が必要かどうか

### OOP の自然な発想と DDD の違い

OOP を素直に考えると、こうしたくなる:

- 利用者は予約を持っているはず → `Patron.Holds`
- 本はコピーを持っているはず → `Book.Copies`
- 双方向に参照した方が自然

つまり **現実世界の関係をそのままオブジェクトで再現したくなる**。

しかし DDD が最優先するのは **整合性が壊れないこと** であり、オブジェクトの美しさではない。

### DDD の問い

集約を決めるとき、DDD はこう問う:

1. **何を一緒に保存しないと壊れるか？**
2. **どこまでが 1 トランザクションか？**
3. **どこでロックするか？**

「関連があるから持たせる」ではなく「一緒に保存しないと整合性が壊れるから持たせる」。

### 本プロジェクトでの判断

| 関係 | OOP の発想 | DDD の判断 | 理由 |
|------|-----------|-----------|------|
| Book → Hold | Book が Hold を持つ | **持たせる** | 予約キューの順番は 1 トランザクションで保証が必要 |
| Book → BookCopy | Book が Copy を持つ | **持たせない** | Copy の状態変更に Book の整合性は不要。ライフサイクルが違う |
| Patron → Hold | Patron が Hold を持つ | **持たせない** | Hold が PatronId を持っていれば関係は十分 |

### なぜ Book は BookCopy を持たないか

```
Book に List<BookCopy> を持たせた場合:
  Copy の状態を OnHold → Loaned にするだけなのに
  Book 全体 + 全 Hold + 全 Copy をロード・保存する必要がある

今の設計:
  Copy だけロード・保存すればよい
```

Copy が欲しいときは、リポジトリで取る:

```csharp
Task<List<BookCopy>> FindByBookId(BookId bookId);
```

**集約の中に持つ** のではなく、**必要なときにクエリで取る**。

### 原則

- 関連があるからといって集約に入れない
- **整合性の保証が必要なものだけ**を同じ集約にする
- 関連は ID 参照で十分な場合が多い
- 双方向ナビゲーション（`Book.Copies` と `Copy.BookId` の両方）は避ける
