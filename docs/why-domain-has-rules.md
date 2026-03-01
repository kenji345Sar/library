# なぜビジネスルールはドメインエンティティに書くのか

## よくある間違い: ユースケースにルールを書く

```csharp
// ユースケースに直接ルールを書いてしまう例（NG）
public async Task<PlaceHoldResult> Execute(PlaceHoldCommand command)
{
    var patron = await _patronRepository.FindById(command.PatronId);

    // ユースケースが「5件まで」を知っている
    if (patron.Type == PatronType.Regular && activeHoldCount >= 5)
        throw new InvalidOperationException("予約制限");

    // ユースケースが「延滞2件」を知っている
    if (overdueCount > 2)
        throw new InvalidOperationException("延滞制限");
}
```

この書き方だと、別のユースケースでも同じ判定が必要になったとき、コピペが発生する。

---

## DDD の書き方: エンティティに聞く

```csharp
// ユースケースは「聞くだけ」
public async Task<PlaceHoldResult> Execute(PlaceHoldCommand command)
{
    var patron = await _patronRepository.FindById(command.PatronId);

    if (!patron.CanPlaceHold(activeHoldCount, overdueCount))  // Patron に判断を任せる
        throw new InvalidOperationException("予約制限");

    var book = await _bookRepository.FindById(command.BookId);
    var hold = book.PlaceHold(command.PatronId);               // Book に状態変更を任せる
}
```

```csharp
// ビジネスルールは Patron の中にある
public class Patron
{
    public bool CanPlaceHold(int activeHoldCount, int overdueCount)
    {
        if (overdueCount > 2) return false;
        if (Type == PatronType.Regular && activeHoldCount >= 5) return false;
        return true;
    }
}
```

---

## 役割の違い

| 層 | 役割 | やること |
|---|---|---|
| ユースケース | 調整役 | Repository からエンティティを取得し、エンティティに頼み、保存する |
| ドメインエンティティ | ビジネスルールの持ち主 | 判断する、状態を変える |
| Repository | 永続化 | エンティティの保存・取得 |

---

## ユースケースの流れ（本プロジェクトの例）

```
PlaceHoldUseCase.Execute()
    │
    ├── 1. Repository からエンティティを取得
    │     patron = _patronRepository.FindById(...)
    │     book   = _bookRepository.FindById(...)
    │
    ├── 2. エンティティにビジネスルールを聞く
    │     patron.CanPlaceHold(...)     ← Patron が判断する
    │
    ├── 3. エンティティに状態変更を頼む
    │     book.PlaceHold(patronId)     ← Book が Hold を追加する
    │
    └── 4. Repository で保存
          _bookRepository.Save(book)
```

ユースケースは **いつ・誰に頼むか** を決めるだけ。**どう判断するか** はエンティティが知っている。

---

## Policy や Domain Service にまとめない理由

ユースケースが長いと感じて、判定ロジックを Policy にまとめたくなることがある。

```csharp
// Policy にまとめた場合
await _holdPolicy.EnsureCanPlaceHold(patronId);
```

一見スッキリするが、中で何をしているかわからない。Patron に聞いているのか、別のルールがあるのか、読んだだけでは判断できない。

```csharp
// エンティティに直接聞く場合
if (!patron.CanPlaceHold(activeHoldCount, overdueCount))
    throw new InvalidOperationException("予約制限により予約できません。");
```

こちらは「Patron にルールを聞いている」「何を渡しているか」が読めばわかる。

### まとめるべきタイミング

- 同じ判定を複数のユースケースで使い回すようになったとき
- 判定のステップが 5 個以上に増えたとき

今のように判定が 1 箇所だけなら、エンティティに直接聞く方がわかりやすい。

---

## なぜこうするのか

1. **ルールの重複を防ぐ** — 予約上限の判定が1箇所にまとまる
2. **ルール変更が楽** — 「5件→10件」に変えるとき Patron だけ直せばいい
3. **テストしやすい** — `patron.CanPlaceHold(5, 0)` だけでルールをテストできる
4. **読みやすい** — ユースケースを読めば「何をしているか」、エンティティを読めば「どういうルールか」がわかる
