# CardBattleScene タスクリスト（完全版）

## UI/基本表示
- [x] Canvasの作成・設定
  - Render Mode: Screen Space - Overlay
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1920x1080
  - Match: 0.5
- [x] プレイヤー手札（PlayerHand）・敵手札（EnemyHand）・フィールド（PlayerField/EnemyField）パネルの作成
  - RectTransformのサイズ・位置調整
  - Image/Spriteの設定
- [x] カードPrefabの作成
  - CardController, CardMovement スクリプトのアタッチ
  - カード画像・テキスト（名前/攻撃/HP/コスト等）の配置

## 手札生成＆ドラッグ操作
- [x] ゲーム開始時に両プレイヤーへ3枚ずつカードを配る
  - CreateCard() 関数の実装
  - PlayerHand/EnemyHand へのカード生成
- [x] カードのドラッグ＆ドロップでフィールドに移動できる機能
  - CardMovement スクリプトで SetCardTransform() 実装
  - ドラッグ時の親Transform切り替え
- [x] カードPrefabにCardMovement, CardControllerの連携
  - Awake()でmovement取得

## ターン制の実装
- [x] GameManagerCardBattleスクリプトの作成・アタッチ
  - isPlayerTurn 変数でターン管理
  - StartGame(), TurnCalc(), PlayerTurn(), EnemyTurn(), ChangeTurn() の実装
- [x] ターンエンドボタンの設置
  - Canvas内にButton作成（TurnEndButton）
  - OnClick()でGameManagerCardBattle.ChangeTurn()を呼ぶ
  - ボタンテキスト「ターンエンド」に変更
- [x] ターン切り替え時の挙動確認
  - プレイヤー/敵のターンでログ出力

## ドロー機能
- [x] ターン開始時にカードを1枚ドロー
  - ChangeTurn()でCreateCard()を呼ぶ
  - プレイヤー/敵の手札に追加
- [x] ドローするカードをランダム化
  - CreateCard()でランダムなカードIDを設定
- [x] 手札の上限設定（例：5枚）
  - hand.childCount < 5 の判定追加
  - 上限時はログ出力

## 敵AI（カードを出す）
- [x] 敵ターン時に手札からカードをフィールドに出す
  - EnemyHandTransform.GetComponentsInChildren<CardController>()で手札取得
  - 1枚目のカードをEnemyFieldTransformへ移動
  - CardController.movement.SetCardTransform()を呼ぶ
- [ ] 敵AIの強化（任意）
  - ランダムなカードを出す
  - 条件付きでカードを出す（コスト/状況判定）
  - 1ターンに複数枚出す

## スクリプト/オブジェクト設定
- [x] GameManagerCardBattleのInspectorで各Transform（PlayerHand, EnemyHand, PlayerField, EnemyField）を設定
- [x] CardControllerにCardMovementが正しくアタッチされていることを確認
- [x] CardMovementのdefaultParentが正しく更新されることを確認

## テスト・デバッグ
- [x] ゲーム開始時の挙動確認（手札配布/ターン表示）
- [x] ターンエンドボタンでターンが切り替わるか確認
- [x] ドロー・手札上限・敵AIの動作確認
- [x] ログ出力で各処理の流れを確認

## バトル処理・攻撃・HP・破壊・勝敗判定（tutorial2以降）

- [ ] カード同士のバトル処理の実装 🟡
  - フィールド上のカードを選択し、攻撃対象を指定できるようにする
  - CardControllerにAttack(CardController target)メソッドを追加
  - 攻撃時、攻撃力分だけ相手カードのHPを減算
  - 依存: カードのHP/攻撃力のUI表示
  - 見積: 2h

- [ ] カードのHP減少・破壊処理の実装 🟡
  - HPが0以下になったカードを破壊（GameObjectのDestroyまたは墓地エリアへ移動）
  - CardControllerにDestroyCard()メソッドを追加
  - 破壊時にエフェクトやアニメーションを再生（任意）
  - 依存: バトル処理
  - 見積: 1.5h

- [ ] カードのHP/攻撃力/コストのUI更新 🟢
  - CardViewにHPバーやテキストの更新処理を追加
  - HP減少時にUIが即時反映されるようにする
  - 依存: バトル処理
  - 見積: 1h

- [ ] プレイヤー/敵リーダーHP管理の実装 🟡
  - GameManagerCardBattleにplayerHP, enemyHP変数を追加
  - カードがリーダーを攻撃した場合、リーダーHPを減算
  - リーダーHPのUI表示（TextまたはSlider）
  - 依存: バトル処理
  - 見積: 1.5h

- [ ] 勝敗判定・ゲーム終了処理の実装 🔴
  - リーダーHPが0以下になったら勝敗を判定
  - GameManagerCardBattleにCheckGameEnd()メソッドを追加
  - 勝敗時にUIで結果を表示（Win/Lose画面）
  - 依存: リーダーHP管理
  - 見積: 1h

- [ ] バトル演出・エフェクトの追加 ⚪
  - 攻撃時や破壊時にアニメーションやエフェクトを再生
  - サウンドの追加（任意）
  - 依存: バトル処理・破壊処理
  - 見積: 2h

- [ ] テスト・デバッグ（バトル処理） 🟢
  - 攻撃・HP減少・破壊・勝敗の一連の流れをテスト
  - ログ出力やUIで動作確認
  - 依存: 上記全タスク
  - 見積: 1h 