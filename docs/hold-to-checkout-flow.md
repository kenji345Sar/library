# 予約〜貸出フロー（Hold → Checkout）

## スコープ

予約して、実際に借りられるところまで。返却は含まない。

### 前提

- 予約（Hold）は **Book に付ける**（Copy ではない）
- Copy は後で割り当てる

---

## 全体フロー

```
① 予約する
② 予約キューに入る
③ Copy が空く
④ Copy を予約者に割り当てる
⑤ 利用者が来館
⑥ 貸出（Checkout）
```

---

## ① 予約する（Hold 作成）

### 入力

- PatronId
- BookId

### 業務チェック

1. 利用者は存在するか
2. Regular なら現在予約数 < 5 か
3. 延滞制限に引っかかっていないか

OK なら:

- Book に Hold を追加（予約キューの最後に入れる）

この時点では **CopyId はまだない**。

---

## ② 予約キューの状態

Book はこういう状態を持つ:

```
Book
 └─ Holds（待ち行列）
     ├─ Hold1（A さん）
     ├─ Hold2（B さん）
     └─ Hold3（C さん）
```

---

## ③ Copy が空く（返却 or 新規登録）

Copy が Available になった瞬間:

- その Copy の Book を確認
- 予約キューがあるか確認

---

## ④ Copy を割り当てる

予約キューの先頭を見る:

1. Patron が制限違反していないか
2. Restricted の場合は Researcher か

OK なら:

- Hold に CopyId を紐づける
- Copy の状態を **OnHold（取り置き中）** に変更

```
Copy
  Status = OnHold
  AssignedTo = PatronId
```

---

## ⑤ 利用者が来館

利用者がカウンターに来る。

職員が以下を確認:

- Copy が OnHold であること
- その利用者のものであること

---

## ⑥ 貸出（Checkout）

ここで初めて Checkout が作られる。

### 処理

1. Hold を完了（Fulfilled）にする
2. Copy を **Loaned** 状態へ変更する
3. Checkout エンティティを作成する
   - PatronId
   - CopyId
   - DueDate（最大 60 日）

```
Copy.Status = Loaned
Checkout.Create(PatronId, CopyId, DueDate)
```

ここで「借りられた」状態になる。

---

## 責任分担

| エンティティ | 責任 |
|------------|------|
| Patron | 予約上限チェック |
| Book | 予約キュー管理 |
| Copy | 状態管理（Available → OnHold → Loaned） |
| Checkout | 貸出記録 |

---

## Copy の状態遷移

```
Available ──→ OnHold ──→ Loaned
   ↑                       │
   └───────────────────────┘
          （返却：今回のスコープ外）
```

---

## 関連するドメイン制約

| # | 制約 | チェックタイミング |
|---|------|-------------------|
| C1 | 1 Book Instance に同時予約は 1 人まで | ④ Copy 割当時 |
| C2 | Regular の同時予約は最大 5 件 | ① Hold 作成時 |
| C3 | Restricted 本は Researcher のみ | ④ Copy 割当時 |
| C5 | 延滞 2 件超で予約拒否 | ① Hold 作成時 |
| C6 | 貸出は最大 60 日間 | ⑥ Checkout 作成時 |

---

## テスト一覧

### 正常系

| テスト | フロー | 検証内容 |
|--------|--------|----------|
| 予約して割当されて貸出できる | ①→④→⑥ | 全フローを通しで実行。各ステップで Hold と Copy の状態遷移が正しいことを確認 |
| Researcher は Restricted 本を割当して貸出できる | ①→④→⑥ | Researcher が Restricted 本で全フローを完走できる |

### 異常系（割当できないケース）

| テスト | フロー | 検証内容 | 対応制約 |
|--------|--------|----------|----------|
| 予約がなければ割当されない | ④ | 待ちキューが空の Copy に対して割当を試みると null が返る | - |
| Regular 利用者は 5 件を超えて予約できない | ① | 6 件目の予約で例外が発生する | C2 |
| 延滞が 2 件を超えると予約できない | ① | 延滞 3 件の状態で予約すると例外が発生する | C5 |
| Restricted 本は Regular 利用者に割当できない | ①→④ | 予約キューには入るが、割当時に例外が発生する | C3 |

### テストの実行場所

- **ユニットテスト**: `tests/Library.Tests/HoldToCheckoutFlowTests.cs`
- **動作確認（正常系のみ）**: `src/Library.ConsoleApp/Program.cs`
