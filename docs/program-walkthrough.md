# Program.cs ウォークスルー（現在の実装状態）

## 概要

`src/Library.ConsoleApp/Program.cs` は、予約〜貸出フローの正常系を一通り動かすコンソールアプリ。

**DB は使っていない。** Program.cs の先頭でテストデータ（Book、Copy、Patron）を自分で作り、
それを使って業務フローを流している。実行するたびに空の状態から始まり、終了したらデータは消える。
外部のデータは一切使わない。必要なデータはすべて Program.cs の中で作っている。

---

## フロー全体（①〜⑥）

```
① 予約する              ← 実装済み（PlaceHoldUseCase）
② 予約キューに入る       ← ① の内部で自動的に起こる
③ Copy が空く           ← データ作成時に Available な Copy を用意して再現
④ Copy を予約者に割り当てる ← 実装済み（AssignCopyUseCase）
⑤ 利用者が来館           ← 物理的イベント（コード不要）
⑥ 貸出（Checkout）       ← 実装済み（CheckoutBookUseCase）
```

Program.cs で UseCase として実行しているのは **①④⑥** の 3 ステップ。
②③⑤ はコードとして明示的に書く必要がないステップ。

---

## 未実装の業務ルール

| # | ルール | 状態 | 備考 |
|---|--------|------|------|
| C1 | 1 Book Instance に同時予約は 1 人まで | **未実装** | Book の予約キューで重複チェックが必要 |
| C4 | 無期限予約（Open-ended Hold）は Researcher のみ | **未実装** | Hold に期限の概念がまだない |

### 実装済みの業務ルール

| # | ルール | どこでチェックしているか |
|---|--------|------------------------|
| C2 | Regular の同時予約は最大 5 件 | `Patron.CanPlaceHold()` |
| C3 | Restricted 本は Researcher のみ | `RestrictedBookPolicy.EnsureCanAssign()`（Domain Service） |
| C5 | 延滞 2 件超で予約拒否 | `Patron.CanPlaceHold()` |
| C6 | 貸出は最大 60 日間 | `Checkout` コンストラクタ |

※ Program.cs は正常系 1 件だけを流すので、C2/C3/C5 は発動しない。C6 は DueDate に反映される。

---

## 準備フェーズ

Repository（インメモリ実装）と UseCase を組み立てる。

```
InMemoryBookRepository
InMemoryBookCopyRepository
InMemoryPatronRepository
InMemoryCheckoutRepository
```

UseCase は 3 つ:

| UseCase | 役割 |
|---------|------|
| PlaceHoldUseCase | ① 予約する |
| AssignCopyUseCase | ④ Copy を割り当てる |
| CheckoutBookUseCase | ⑥ 貸出する |

---

## テストデータ作成（③ Copy が空いている状態）

**ここが業務フローを流す前の「データの準備」。**
本来は DB に入っているデータだが、今は DB がないので自分で作っている。
ドメインのエンティティを直接 `new` して Repository に追加する。

```csharp
var book   = new Book(BookId.NewId(), new ISBN("978-4-00-000001-0"), "ドメイン駆動設計入門", 3000m);
var copy   = new BookCopy(CopyId.NewId(), book.Id, CopyType.Circulating);
var patron = new Patron(PatronId.NewId(), PatronType.Regular);

bookRepo.Add(book);
copyRepo.Add(copy);
patronRepo.Add(patron);
```

- `Book`、`BookCopy`、`Patron` はそれぞれ別の集約のエンティティ
- `BookId.NewId()` などの Value Object を通して ID を生成
- Copy は `CopyType.Circulating`（通常貸出用）で作成
- この時点で Copy の状態は `Available` → フロー③に相当

---

## ① 予約する

```csharp
var holdId = await placeHold.Execute(patron.Id, book.Id);
```

- UseCase に `PatronId` と `BookId` を渡すだけ
- 内部で以下を行う:
  1. Patron を取得し、予約可能か確認（C2: 予約上限 5 件、C5: 延滞 2 件超で拒否）
  2. Book に Hold を追加（予約キューの末尾に入る）
  3. Book を保存
- 結果: `HoldId` が返る
- この時点の状態:
  - `Hold.Status = Waiting`
  - `Copy.Status = Available`（まだ割当されていない）

---

## ④ Copy を割り当てる

```csharp
var assignResult = await assignCopy.Execute(copy.Id);
```

- UseCase に `CopyId` を渡す
- 内部で以下を行う:
  1. Copy を取得し、`Available` であることを確認
  2. Book を取得し、待ちキューの先頭（`Waiting` の Hold）を見る
  3. Copy が `Restricted` なら、Patron が `Researcher` か確認（C3: Domain Service `RestrictedBookPolicy` を使用）
  4. Hold に CopyId を紐づけ、Copy を取り置き状態にする
  5. Book と Copy を保存
- 結果: `(HoldId, PatronId)` のタプルが返る
- この時点の状態:
  - `Hold.Status = Assigned`
  - `Copy.Status = OnHold`
  - `Copy.HeldBy = PatronId`（誰のために取り置いているか）

---

## ⑥ 貸出する

```csharp
var checkoutResult = await checkoutBook.Execute(copy.Id, patron.Id);
```

- UseCase に `CopyId` と `PatronId` を渡す
- 内部で以下を行う:
  1. Copy を取得し、`OnHold` かつ渡された Patron のものか確認
  2. Book を取得し、Hold を `Fulfilled` にする
  3. Copy を `Loaned` 状態にする
  4. Checkout エンティティを作成（返却期限 = 今日 + 60 日、C6）
  5. Copy と Checkout を保存
- 結果: `(CheckoutId, DueDate)` のタプルが返る
- この時点の状態:
  - `Hold.Status = Fulfilled`
  - `Copy.Status = Loaned`

---

## 異常系の確認

Program.cs は正常系のみ。異常系は `tests/Library.Tests/HoldToCheckoutFlowTests.cs` でテストしている。

参照: [テスト一覧](hold-to-checkout-flow.md#テスト一覧)
