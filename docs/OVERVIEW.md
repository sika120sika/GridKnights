# GridKnights — コードベース概要

> 最終更新: 2026-04-27

---

## シーン構成

シーンは `Scenes/Main.tscn` の 1 シーンのみ。

```
Main (Node2D)
├── GameManager (Node)                    # ゲーム全体統制・状態遷移
├── GameBoard (Node2D)                    # グリッド描画ルート
│   └── [GridMap.cs]
│       ├── UnitsLayer (Node2D)           # ユニット描画レイヤー
│       └── GridHighlight (Node2D)        # 範囲ハイライト描画
├── HUD (CanvasLayer)                     # UI 上位レイヤー
│   └── [HUD.cs]
│       ├── ターンラベル
│       ├── UnitInfoPanel                 # 選択ユニット情報
│       ├── ターン終了ボタン
│       └── 操作説明ラベル
└── GameResultScreen (CanvasLayer)        # 勝利/敗北画面
```

ノード間の依存は Godot の `[Export]` で GameManager → GridMap / HUD / GridHighlight / GameResultScreen を参照。

---

## 主要クラスと責務

### ゲーム制御

| クラス | ファイル | 責務 |
|--------|---------|------|
| `GameManager` | `Scripts/GameManager.cs` | ターン管理・入力ハンドリング・勝敗判定。`PlayerInputState`（Select/Move/Attack）で状態遷移を管理。 |
| `GridMap` | `Scripts/GridMap.cs` | グリッドデータ管理（タイル・ユニット配置）・座標変換・`CellClicked` シグナル発火。 |
| `MapGenerator` | `Scripts/MapGenerator.cs` | 8×8 マップのランダム生成（障害物・ユニット配置）。最大 10 回リトライ後に強制出力。 |
| `MapValidator` | `Scripts/MapValidator.cs` | 生成マップの妥当性検証（敵数・経路保証・最小距離）。 |
| `Pathfinder` | `Scripts/Pathfinder.cs` | BFS による移動可能セル計算・経路探索・攻撃対象リスト取得。`IPassabilityMap` で地形判定を注入。 |

### ユニット

| クラス | ファイル | 責務 |
|--------|---------|------|
| `Unit` | `Scripts/Units/Unit.cs` | パラメータ管理・HP 管理・`HpChanged`/`Defeated` シグナル。`static GetStats(UnitType)` でスタティックパラメータ取得。`MoveToAsync` / `LungeForwardAsync` / `LungeReturnAsync` / `ShakeAsync` でアニメーション処理。`DrawHpBar()` でHPバー描画。 |
| `PlayerUnit` | `Scripts/Units/PlayerUnit.cs` | プレイヤーユニット。`_Draw()` で青円と種別記号・HPバーを描画。行動完了時にグレイアウト。 |
| `EnemyUnit` | `Scripts/Units/EnemyUnit.cs` | 敵ユニット。`ExecuteTurnAsync()` で `IEnemyBrain` に行動委譲（非同期）。赤四角・HPバーを描画。 |
| `RuleBasedEnemyBrain` | `Scripts/Units/RuleBasedEnemyBrain.cs` | 最近傍プレイヤーへ移動→攻撃のルールベース AI。`IEnemyBrain` 実装。`TakeTurnAsync` で非同期処理。攻撃時にランジ＋シェイクアニメーションとダメージポップアップを実行。 |

### HUD / UI

| クラス | ファイル | 責務 |
|--------|---------|------|
| `HUD` | `Scripts/HUD/HUD.cs` | UI ノード群の統括管理・ターンラベル更新・ユニット情報表示切替。 |
| `GridHighlight` | `Scripts/HUD/GridHighlight.cs` | 移動範囲（青）・攻撃範囲（赤）のセルハイライト描画。 |
| `UnitInfoPanel` | `Scripts/HUD/UnitInfoPanel.cs` | 選択ユニットの HP・攻撃力・移動・射程・行動状態を表示。 |
| `GameResultScreen` | `Scripts/HUD/GameResultScreen.cs` | 勝利/敗北テキスト表示・リスタートボタン制御。`ProcessMode = Always` で停止中も動作。 |
| `DamagePopup` | `Scripts/HUD/DamagePopup.cs` | 攻撃時のダメージ数値をユニット上に一時表示するポップアップ。`Spawn()` で生成。 |

### Enum（`Scripts/Enums/`）

| ファイル | 内容 |
|---------|------|
| `Team.cs` | `Player` / `Enemy` |
| `TileType.cs` | `Empty` / `Obstacle` |
| `TurnPhase.cs` | `PlayerTurn` / `EnemyTurn` |
| `UnitActionState.cs` | `Idle` / `Moved` / `Attacked` / `Done` |
| `UnitType.cs` | `Swordsman` / `Archer` / `Mage` / `Goblin` / `Orc` / `SkeletonArcher` / `DarkWizard` |

---

## ユニット・パラメータ一覧

`Unit.GetStats(UnitType)` で取得するスタティックパラメータ。

| ユニット | 陣営 | MaxHp | Attack | MoveRange | AttackRange |
|---------|------|------:|-------:|----------:|------------:|
| Swordsman（剣士） | Player | 120 | 40 | 3 | 1 |
| Archer（弓手） | Player | 80 | 25 | 3 | 3 |
| Mage（魔法使い） | Player | 60 | 45 | 2 | 2 |
| Goblin（ゴブリン） | Enemy | 50 | 15 | 4 | 1 |
| Orc（オーク） | Enemy | 130 | 40 | 2 | 1 |
| SkeletonArcher（骸骨弓手） | Enemy | 55 | 25 | 2 | 3 |
| DarkWizard（闇魔道士） | Enemy | 55 | 40 | 2 | 2 |

攻撃範囲はマンハッタン距離で判定。

---

## 現在の実装済み機能

### ターンシステム
- PlayerTurn → EnemyTurn の交互進行
- 全敵撃破で **勝利**、全プレイヤーユニット撃破で **敗北**
- ターン終了ボタンで手動移行可能

### プレイヤー操作
1. **選択** — ユニットをクリック → 移動範囲（青）・攻撃範囲（赤）をハイライト表示
2. **移動** — 移動範囲内のセルをクリックして移動（BFS で到達可否を判定・Tween スライドアニメーション）
3. **攻撃** — 攻撃範囲内の敵をクリックしてダメージを与える（ランジ前進 → ダメージ → 被弾シェイク → 後退のアニメーション）
- 移動後に攻撃が可能（`Moved` 状態）
- 攻撃後は行動完了（`Done` 状態）、グレイアウト表示
- アニメーション実行中（`_isAnimating` フラグ）は入力を無視

### 敵AI（RuleBasedEnemyBrain）
1. 攻撃範囲内にプレイヤーユニットがいれば即攻撃
2. いなければ最近傍プレイヤーユニットへ向かって移動
3. 移動後に再度攻撃判定を実施
- 各敵の行動は非同期（`TakeTurnAsync`）で、行動間に 0.3 秒のウェイトを挟む
- 攻撃時はプレイヤー攻撃と同様のランジ＋シェイクアニメーション＋ダメージポップアップ

### マップ生成
- グリッドサイズ: 8×8、セルサイズ: 64px
- 障害物: 約 20%
- プレイヤーユニット 3 体を左半分、敵 2〜6 体を右半分にランダム配置
- **MapValidator** による検証（敵数範囲・全経路保証・平均距離 ≥ 3）
- 検証失敗時は最大 10 回リトライ

### 移動・経路探索
- BFS で `MoveRange` マス以内の到達可能セルを計算
- 友軍ユニットのセルは通過可能だが停止不可
- 障害物・敵ユニットのセルは通過不可

### UI / HUD
- ターン表示ラベル
- 選択ユニットの HP・パラメータ情報パネル
- 移動範囲・攻撃範囲ハイライト
- 勝利/敗北結果画面 + リスタート
- 攻撃時ダメージポップアップ（`DamagePopup`）
- ユニット上部の HP バー（緑→黄→赤でグラデーション変化）

---

## 既知の課題・TODO

コード中に明示的な TODO / FIXME コメントは存在しないが、設計上の注意点・拡張余地は以下の通り。

### 設計上の制約

| 項目 | 内容 |
|-----|------|
| マップ生成の強制フォールバック | `MapGenerator` は 10 回失敗すると検証をスキップして強制生成する。稀に不正なマップが生成される可能性がある。 |
| BGM / SE 未実装 | 攻撃・移動・勝敗の効果音・BGM がない。 |

### 拡張余地

| 項目 | 備考 |
|-----|------|
| 複数 AI 戦略 | `IEnemyBrain` インターフェースにより Strategy パターンで追加可能 |
| スキル・特殊行動 | `UnitActionState.Attacked` が未使用のため、攻撃後移動など追加行動フローの拡張余地あり |
| 複数シーン・ステージ | 現状は Main.tscn 1 シーンのみ。ステージ選択・マップデータ外部化は未実装 |
| セーブ/ロード | 未実装 |
| BGM / SE | 攻撃・移動・勝敗の効果音・BGM が未実装 |
