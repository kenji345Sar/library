# 図書館システム 残作業一覧

## 現状（仮設定でやっていること）

| 項目 | 今の状態 | 本来の姿 |
|------|---------|---------|
| データ保存 | InMemory（メモリ上のリスト、終了したら消える） | DB に永続化 |
| Book / Copy / Patron | Program.cs で手動で new して作っている | 登録機能で作成・DB に保存 |
| 延滞数 | Checkout データがないので常に 0 | 過去の貸出記録から実際に数える |
| 返却 | 返却の概念がない（借りたら借りっぱなし） | 返却処理で Checkout を完了にする |

---

## 未実装の業務ルール

| # | ルール | 何が足りないか |
|---|--------|--------------|
| C1 | 1 Book Instance に同時予約は 1 人まで | Book の予約キューで同一人物の重複チェック |
| C4 | 無期限予約（Open-ended Hold）は Researcher のみ | Hold に期限の概念がない |

---

## 残作業一覧

| # | 作業 | 状態 | 内容 |
|---|------|------|------|
| 1 | 返却機能 | 未着手 | Checkout に返却の概念を追加。これがないと延滞数が正しく計算できない |
| 2 | 業務ルール C1 | 未着手 | 1 つの Book に同じ人が重複して予約できないようにする |
| 3 | 業務ルール C4 | 未着手 | Hold に期限の概念を入れる（Open-ended / Closed-ended） |
| 4 | 延滞の日次チェック | 未着手 | 毎日延滞を確認し、制限をかける処理 |
| 5 | データの登録機能 | 未着手 | Book / Copy / Patron を登録する UseCase |
| 6 | DB への永続化 | 未着手 | InMemory を実際の DB に差し替える |

---

### 1. 返却機能

今の Checkout は「貸出した」記録だけで「返した」がない。

- Checkout に `ReturnedAt`（返却日時）を追加
- `IsOverdue` を「未返却かつ期限超過」に変更
- 返却の UseCase を作成
- 返却したら Copy の状態を `Loaned` → `Available` に戻す

これがないと延滞数が正しく計算できない。

### 2. 業務ルール C1 の実装

1 つの Book に同じ人が重複して予約できないようにする。

- Book の予約キューに同一 PatronId が既にいないか確認

### 3. 業務ルール C4 の実装

Hold に期限の概念を入れる。

- Closed-ended Hold: 期限付き（Regular / Researcher）
- Open-ended Hold: 無期限（Researcher のみ）
- 期限切れ Hold の失効処理（日次バッチ）

### 4. 延滞の日次チェック

- 毎日延滞を確認する処理
- domain-rules.md に「日次で延滞チェックを実施する」と記載済み

### 5. データの登録機能

Book、Copy、Patron を登録する機能。

- 今は Program.cs で直接 new しているが、本来は登録 UseCase が必要
- Book の登録（ISBN、タイトル、価格）
- Copy の登録（どの Book に紐づけるか、Circulating / Restricted）
- Patron の登録（Regular / Researcher）

### 6. DB への永続化

InMemory の Repository を実際の DB に差し替える。

- Repository Interface はそのまま使える（Domain 層にある）
- Infrastructure 層に DB 用の Repository 実装を追加
- InMemory 版はテスト用として残す
