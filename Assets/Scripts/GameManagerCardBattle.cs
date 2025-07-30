using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class GameManagerCardBattle : MonoBehaviourPunCallbacks
{
    [SerializeField] UIManager uIManager;
    public UIManager UIManager => uIManager; // 外部アクセス用プロパティ
    // プレイヤーのターンかどうかを判定する変数
    public bool isPlayerTurn;
    
    // 現在のターン数を管理する変数
    int currentTurn;

    // カードのプレファブを入れる
    [SerializeField] CardController cardPrefab;

    // 両プレイヤーの手札のTransformを入れる
    [SerializeField] public Transform PlayerHandTransform, EnemyHandTransform;
    
    // フィールドのTransformを入れる
    [SerializeField] public Transform PlayerFieldTransform, EnemyFieldTransform;

    [SerializeField] GameObject playerLeaderObject; // PlayerLeaderのGameObject
    [SerializeField] GameObject enemyLeaderObject;  // EnemyLeaderのGameObject
    
    [SerializeField] TextMeshProUGUI playerManaPointText;
    [SerializeField] TextMeshProUGUI playerDefaultManaPointText;
    [SerializeField] TextMeshProUGUI enemyManaPointText;
    [SerializeField] TextMeshProUGUI enemyDefaultManaPointText;
    [SerializeField] Transform playerLeaderTransform;
 
    public int playerManaPoint; // 使用すると減るマナポイント
    public int playerDefaultManaPoint; // 毎ターン増えていくベースのマナポイント
    public int enemyManaPoint; // 敵の使用すると減るマナポイント
    public int enemyDefaultManaPoint; // 敵の毎ターン増えていくベースのマナポイント
    public LeaderModel playerLeader;
    public LeaderModel enemyLeader;
    
    public static GameManagerCardBattle instance;
    private bool gameEnded = false; // ゲーム終了フラグ
    
    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        // 初期化を確実に行ってからゲームを開始
        StartCoroutine(DelayedGameStart());
    }
    
    IEnumerator DelayedGameStart()
    {
        // 1フレーム待機して他のコンポーネントの初期化を待つ
        yield return null;
        
        // 必要なコンポーネントが存在するか確認
        if (uIManager == null || PlayerHandTransform == null || EnemyHandTransform == null ||
            PlayerFieldTransform == null || EnemyFieldTransform == null)
        {
            Debug.LogError("必要なコンポーネントが設定されていません。Inspectorで確認してください。");
            yield break;
        }
        
        // EffectProcessorとEffectTargetSelectorの存在確認と作成
        EnsureEffectSystemComponents();
        
        // ゲーム開始
        isPlayerTurn = true;
        currentTurn = 1;
        
        // モード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (!isMultiplayMode)
        {
            // シングルプレイの場合は通常通り開始
            StartGame();
        }
        // マルチプレイの場合は OnlineMatchingManager がマッチング完了後に StartGame を呼び出す
    }
    
    void EnsureEffectSystemComponents()
    {
        // EffectProcessorの確認
        if (EffectProcessor.Instance == null)
        {
            Debug.Log("EffectProcessorが見つからないため、新しく作成します。");
            GameObject effectProcessorObj = new GameObject("EffectProcessor");
            effectProcessorObj.AddComponent<EffectProcessor>();
        }
        else
        {
            Debug.Log("EffectProcessorが既に存在しています。");
        }
        
        // EffectTargetSelectorの確認
        if (EffectTargetSelector.instance == null)
        {
            Debug.Log("EffectTargetSelectorが見つからないため、新しく作成します。");
            GameObject effectTargetSelectorObj = new GameObject("EffectTargetSelector");
            effectTargetSelectorObj.AddComponent<EffectTargetSelector>();
        }
        else
        {
            Debug.Log("EffectTargetSelectorが既に存在しています。");
        }
        
        // GameEventManagerの確認
        if (GameEventManager.Instance == null)
        {
            Debug.Log("GameEventManagerが見つからないため、新しく作成します。");
            GameObject gameEventManagerObj = new GameObject("GameEventManager");
            gameEventManagerObj.AddComponent<GameEventManager>();
        }
        else
        {
            Debug.Log("GameEventManagerが既に存在しています。");
        }
    }

        void TurnCalc() 
    {
        StartCoroutine(TurnCalcCoroutine());
    }

    IEnumerator TurnCalcCoroutine()
    {
        // マルチプレイでEnemy Turnの場合は、パネル表示のみ（PlayerTurnの場合は呼び出し元で表示済み）
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (!isMultiplayMode || isPlayerTurn)
        {
            yield return StartCoroutine(uIManager.ShowChangeTurnPanel(isPlayerTurn));
        }
        
        // ターン進行の処理
        if (isPlayerTurn)
        {
            PlayerTurn();
        }
        else 
        {
            if (!isMultiplayMode)
            {
                StartCoroutine(EnemyTurn()); // シングルプレイ時のみNPCターン
            }
            // マルチプレイのEnemyTurn時は相手プレイヤーのターンを待つだけ
        }
    }

    void PlayerTurn() 
    {
        // プレイヤーのターン開始時の処理
        Debug.Log("Player Turn");

        if (PlayerFieldTransform != null)
        {
            CardController[] playerFieldCardList = PlayerFieldTransform.GetComponentsInChildren<CardController>();
            Debug.Log($"Player cards: {playerFieldCardList.Length}");
            foreach (CardController card in playerFieldCardList)
            {
            }
            SetAttackableFieldCard(playerFieldCardList, true);
        }

        /// マナを増やす
        playerDefaultManaPoint++;
        playerManaPoint = playerDefaultManaPoint;
        ShowManaPoint();
        SetCanUsePanelHand(); // 手札の使用可能カードを更新
        
        // プレイヤーターンではターンエンドボタンを表示
        if (uIManager != null)
        {
            uIManager.ShowTurnEndButton(true);
        }
    }


    public void ChangeTurn() 
    {
        // ゲーム終了時はターン切り替えを行わない
        if (gameEnded) return;
        
        // モード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);

        // マルチプレイの場合
        if (isMultiplayMode && photonView != null)
        {
            // 自分のターンの時のみRPCを送信（権限チェック）
            if (isPlayerTurn)
            {
                Debug.Log("マルチプレイ: ターン終了を送信");
                
                // 自分のターンを終了（Enemy Turnへ）
                isPlayerTurn = false;
                currentTurn++;
                
                // ターンエンドボタンを非表示
                if (uIManager != null)
                {
                    uIManager.ShowTurnEndButton(false);
                }
                
                // Enemy Turnパネルを表示
                StartCoroutine(uIManager.ShowChangeTurnPanel(false));
                
                // 相手にターン開始を通知（相手側でPlayerTurnになる）
                photonView.RPC("SyncTurnChange", RpcTarget.Others);
            }
            return;
        }
        
        // シングルプレイの場合は既存の処理
        // ターンの切り替え処理
        isPlayerTurn = !isPlayerTurn;
        currentTurn++; // ターン数を増加

        if (isPlayerTurn)
        {
            // プレイヤーの手札が5枚未満のときのみドロー
            if (PlayerHandTransform.childCount < 5)
            {
                int randomID = UnityEngine.Random.Range(1, 41); // 1〜40のランダムなカード
                CreateCard(PlayerHandTransform, randomID);
            }
        }
        else 
        {
            // 敵の手札が5枚未満のときのみドロー
            if (EnemyHandTransform.childCount < 5)
            {
                int randomID = UnityEngine.Random.Range(1, 41); // 1〜40のランダムなカード
                CreateCard(EnemyHandTransform, randomID);
            }
        }
        
        // 次のターンの処理を開始（TurnCalcは一度だけ呼ぶ）
        TurnCalc();
    }

    public void StartGame()
    {
        Debug.Log("StartGame開始: ゲーム初期化を開始します");
        
        // リーダーの初期化（両方ともカードID1のリーダーを使用）
        playerLeader = new LeaderModel(1, true);
        enemyLeader = new LeaderModel(1, false);
        
        // リーダーのHPを同期
        playerLeaderHP = playerLeader.currentHp;
        enemyLeaderHP = enemyLeader.currentHp;
        
        Debug.Log($"プレイヤーリーダー: {playerLeader.name}, HP: {playerLeader.currentHp}/{playerLeader.maxHp}");
        Debug.Log($"敵リーダー: {enemyLeader.name}, HP: {enemyLeader.currentHp}/{enemyLeader.maxHp}");

        /// マナの初期値設定 ///
        playerManaPoint = 1;
        playerDefaultManaPoint = 1;
        enemyManaPoint = 1;
        enemyDefaultManaPoint = 1;
        ShowManaPoint();
        ShowEnemyManaPoint();

        ShowLeaderHP();
        ShowLeaderInfo();
        SettingHand();
        SetCanUsePanelHand(); // 初期手札の使用可能カードを更新
        
        // ゲーム開始準備完了
        Debug.Log("ゲーム開始準備完了。ゲームを開始します。");
        
        // モード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (isMultiplayMode)
        {
            // マルチプレイの場合、マスタークライアントが先攻・後攻を決定
            if (PhotonNetwork.IsMasterClient)
            {
                // ランダムで先攻・後攻を決定（50%の確率）
                bool masterGoesFirst = UnityEngine.Random.Range(0, 2) == 0;
                
                Debug.Log("=== 先攻・後攻決定 ===");
                Debug.Log($"ランダム判定結果: {(masterGoesFirst ? "マスタークライアント先攻" : "非マスタークライアント先攻")}");
                Debug.Log($"マスタークライアント (自分): {(masterGoesFirst ? "先攻" : "後攻")}");
                Debug.Log($"非マスタークライアント (相手): {(!masterGoesFirst ? "先攻" : "後攻")}");
                Debug.Log("==================");
                
                // 全クライアントに先攻・後攻を通知
                photonView.RPC("InitializeTurnOrder", RpcTarget.All, masterGoesFirst);
            }
            // 非マスタークライアントは待機（RPCを受信するまで）
        }
        else
        {
            // シングルプレイの場合は従来通り（プレイヤーが先攻）
            isPlayerTurn = true;
            TurnCalc();
        }
    }


    public void CreateCard(Transform hand, int cardID) 
    {
        // 手札にカードを生成する
        CardController card = Instantiate(cardPrefab, hand, false);

        // Playerの手札に生成されたカードはPlayerのカードとする
        if (hand == PlayerHandTransform)
        {
            card.Init(cardID, true);
            // プレイヤーカード生成後にcanUse状態を更新
            StartCoroutine(UpdateCanUsePanelDelayed());
        }
        else
        {
            card.Init(cardID, false);
            // 敵カード生成後にcanUse状態を更新
            StartCoroutine(UpdateEnemyCanUsePanelDelayed());
        }
    }
    
    System.Collections.IEnumerator UpdateCanUsePanelDelayed()
    {
        yield return null; // 1フレーム待機してカードの初期化を確実に完了させる
        SetCanUsePanelHand();
    }
    
    System.Collections.IEnumerator UpdateEnemyCanUsePanelDelayed()
    {
        yield return null; // 1フレーム待機してカードの初期化を確実に完了させる
        SetCanUsePanelEnemyHand();
    }

    void SettingHand() 
    {
        // モード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (isMultiplayMode)
        {
            Debug.Log("マルチプレイモード: 初期手札配布開始");
            // マルチプレイの場合、マスタークライアントが初期手札を決定
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("マスタークライアント: 初期手札を生成中");
                // 両プレイヤー分の初期手札を決定（各3枚）
                int[] playerCards = new int[3];
                int[] enemyCards = new int[3];
                
                for (int i = 0; i < 3; i++)
                {
                    playerCards[i] = UnityEngine.Random.Range(1, 41);
                    enemyCards[i] = UnityEngine.Random.Range(1, 41);
                }
                
                Debug.Log($"初期手札生成完了: playerCards=[{string.Join(",", playerCards)}], enemyCards=[{string.Join(",", enemyCards)}]");
                
                // 全クライアントに初期手札を送信
                photonView.RPC("InitializeStartingHand", RpcTarget.All, playerCards, enemyCards);
                Debug.Log("初期手札RPC送信完了");
            }
            else
            {
                Debug.Log("非マスタークライアント: 初期手札RPCを待機中");
            }
        }
        else
        {
            // シングルプレイの場合は従来通り
            for (int i = 0; i < 3; i++)
            {
                int randomID = UnityEngine.Random.Range(1, 41); // 1〜40のランダムなカード
                CreateCard(PlayerHandTransform, randomID);  // プレイヤー用のカード
                CreateCard(EnemyHandTransform, randomID);   // 敵用のカード
            }
        }
    }

    //void EnemyTurn()
    IEnumerator EnemyTurn() // StartCoroutineで呼ばれたので、IEnumeratorに変更
    {
        // ゲーム終了時は敵のターンを実行しない
        if (gameEnded) yield break;
        
        // 敵のターン開始時の処理
        Debug.Log("敵のターンです");

        /// 敵のマナを増やす
        enemyDefaultManaPoint++;
        enemyManaPoint = enemyDefaultManaPoint;
        ShowEnemyManaPoint();
        SetCanUsePanelEnemyHand(); // 敵の手札の使用可能カードを更新

        // 敵の手札にあるカードリストを取得
        CardController[] cardList = EnemyHandTransform.GetComponentsInChildren<CardController>();

        /// 敵のフィールドのカードを攻撃可能にして、緑の枠を付ける ///
        CardController[] enemyFieldCardList = EnemyFieldTransform.GetComponentsInChildren<CardController>();

        yield return new WaitForSeconds(1f);

        SetAttackableFieldCard(enemyFieldCardList,true);

        yield return new WaitForSeconds(1f);

        if (cardList.Length > 0)
        {
            // 使用可能なカードを探す
            CardController playableCard = null;
            foreach (CardController card in cardList)
            {
                if (card.model.canUse)
                {
                    playableCard = card;
                    break;
                }
            }

            if (playableCard != null)
            {
                // 使用可能なカードをフィールドに出す（アニメーション付き）
                // アニメーション完了まで待機
                yield return MoveEnemyCardToField(playableCard);
            }
            else
            {
                Debug.Log("敵はマナが足りずカードを出せません");
            }
        }
        else
        {
            Debug.Log("敵の手札がありません");
        }

        CardController[] enemyFieldCardListSecond = EnemyFieldTransform.GetComponentsInChildren<CardController>();
 
        while (Array.Exists(enemyFieldCardListSecond, card => card.model.canAttack))
        {
            // ゲーム終了時は攻撃ループを停止
            if (gameEnded) yield break;
            
            // 攻撃可能カードを取得
            CardController[] enemyCanAttackCardList = Array.FindAll(enemyFieldCardListSecond, card => card.model.canAttack);
            CardController[] playerFieldCardList = PlayerFieldTransform.GetComponentsInChildren<CardController>();
            CardController attackCard = enemyCanAttackCardList[0];
 
           // AttackToLeader(attackCard, false); 
            if (playerFieldCardList.Length > 0) // プレイヤーの場にカードがある場合
            {
                
                int defenceCardNumber = UnityEngine.Random.Range(0, playerFieldCardList.Length);
                CardController defenceCard = playerFieldCardList[defenceCardNumber];
                // カードが破棄されていないことを確認
                if (attackCard != null && attackCard.movement != null && defenceCard != null)
                {
                    yield return StartCoroutine (attackCard.movement.AttackMotion(defenceCard.transform));
                    // AttackMotion後も再度確認
                    if (attackCard != null && defenceCard != null)
                    {
                        CardBattle(attackCard, defenceCard);
                        // 攻撃時効果パネル表示完了を待機
                        yield return new WaitForSeconds(2f);
                    }
                }
            }
            else // プレイヤーの場にカードがない場合
            {
                // カードが破棄されていないことを確認
                if (attackCard != null && attackCard.movement != null && playerLeaderTransform != null)
                {
                    yield return StartCoroutine(attackCard.movement.AttackMotion(playerLeaderTransform));
                    // AttackMotion後も再度確認
                    if (attackCard != null)
                    {
                        AttackToLeader(attackCard, false);
                        // 攻撃時効果パネル表示完了を待機
                        yield return new WaitForSeconds(2f);
                    }
                }
            }

            yield return new WaitForSeconds(1f);
 
            enemyFieldCardListSecond = EnemyFieldTransform.GetComponentsInChildren<CardController>();
        }


        // 効果発動の完了を待ってからターンを終了する
        yield return new WaitForSeconds(0.5f);
        
        // ターンを終了する
        ChangeTurn();
    }

    // 敵のカードをフィールドに移動するアニメーション処理
    private IEnumerator MoveEnemyCardToField(CardController card)
    {
        // アニメーション付きでカードを移動
        yield return card.movement.MoveToFieldWithAnimation(EnemyFieldTransform);
        
        // マナコストを引く
        card.DropField();
        Debug.Log("敵がカードをフィールドに出しました");
        
        // 登場時効果発動後に2秒待機（効果パネル表示完了を確保）
        yield return new WaitForSeconds(2f);
    }

        public void CardBattle(CardController attackCard, CardController defenceCard)
    {
        Debug.Log($"Battle: {attackCard.model.name} → {defenceCard.model.name}");
        
        // ゲーム終了時はカードバトルを行わない
        if (gameEnded) return;
        
        // 攻撃カードがアタック可能でなければ攻撃しないで処理終了する
        if (attackCard.model.canAttack == false) return;
        
        // 味方同士の場合はバトルしない（canAttackは変更しない）
        if (attackCard.model.isPlayerCard == defenceCard.model.isPlayerCard) return;
        
        // 手札にあるカードへの攻撃を防ぐ（canAttackは変更しない）
        if (defenceCard.model.summonedTurn == -1) return;
        
        // マルチプレイの場合は戦闘結果を同期
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (isMultiplayMode)
        {
            // マルチプレイではローカルで実行し、結果のみを相手に送信
            ExecuteCardBattle(attackCard, defenceCard);
            
            // 戦闘結果情報を取得
            string attackCardName = attackCard.model.name;
            string defenceCardName = defenceCard.model.name;
            int attackCardHp = attackCard.model.hp;
            int defenceCardHp = defenceCard.model.hp;
            bool attackCardDestroyed = (attackCard.model.hp <= 0);
            bool defenceCardDestroyed = (defenceCard.model.hp <= 0);
            
            // 両方のカードの戦闘結果を送信（攻撃＋反撃）
            photonView.RPC("SyncBattleResult", RpcTarget.Others, 
                          attackCardName, defenceCardName, 
                          attackCardHp, defenceCardHp, 
                          attackCardDestroyed, defenceCardDestroyed);
        }
        else
        {
            // シングルプレイの場合は従来通りの処理
            ExecuteCardBattle(attackCard, defenceCard);
        }
    }
    
    void ExecuteCardBattle(CardController attackCard, CardController defenceCard)
    {
        // 有効な攻撃なので、攻撃後はcanAttackをfalseにし、緑の縁を消す
        attackCard.model.canAttack = false;
        attackCard.view.SetCanAttackPanel(false);
        
        // 攻撃時イベントを0.5秒遅らせて発火（攻撃モーションとずらすため）
        StartCoroutine(TriggerAttackEventDelayed(attackCard, defenceCard, 0.5f));
        
        // お互いのカードにダメージを与える
        Debug.Log($"Battle: {attackCard.model.name} vs {defenceCard.model.name}");
        
        // 防御側のカードのHPを攻撃側の攻撃力分減らす
        defenceCard.model.hp -= attackCard.model.at;
        Debug.Log($"{defenceCard.model.name} HP: {defenceCard.model.hp}");
        
        // 攻撃側のカードのHPを防御側の攻撃力分減らす
        attackCard.model.hp -= defenceCard.model.at;
        Debug.Log($"{attackCard.model.name} HP: {attackCard.model.hp}");
        
        // UIを更新
        defenceCard.view.Show(defenceCard.model);
        attackCard.view.Show(attackCard.model);
        
        // HPが0以下になったカードを破壊
        if (defenceCard.model.hp <= 0 && defenceCard != null)
        {
            Debug.Log($"{defenceCard.model.name} destroyed");
            defenceCard.DestroyCard(defenceCard);
        }
        
        if (attackCard.model.hp <= 0 && attackCard != null)
        {
            Debug.Log($"{attackCard.model.name} destroyed");
            attackCard.DestroyCard(attackCard);
        }       
    }
    void SetAttackableFieldCard(CardController[] cardList, bool canAttack)
    {
        foreach (CardController card in cardList)
        {
            if (card != null && card.model != null)
            {
                        
                // フィールドにあり、出したターンではない場合のみ攻撃可能にする
                if (card.model.summonedTurn >= 0 && card.model.summonedTurn < currentTurn)
                {
                    card.model.canAttack = canAttack;
                    card.view.SetCanAttackPanel(canAttack);
                    Debug.Log($"{card.model.name}を攻撃可能に設定（パネル: {canAttack}）");
                }
                else
                {
                    card.model.canAttack = false;
                    card.view.SetCanAttackPanel(false);
                    Debug.Log($"{card.model.name}を攻撃不可に設定（パネル: false）");
                }
            }
        }
    }

    // 攻撃時イベントを遅延して発火するコルーチン
    private IEnumerator TriggerAttackEventDelayed(CardController attackCard, CardController defenceCard, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // カードが破壊されていないか確認してからイベント発火
        if (attackCard != null && attackCard.gameObject != null)
        {
            Debug.Log($"攻撃イベント発火: {attackCard.model.name}");
            GameEventManager.TriggerAttackEvent(attackCard, defenceCard);
        }
        else
        {
            Debug.Log("攻撃イベント発火キャンセル: 攻撃カードが既に破壊されています");
        }
    }

    public int playerLeaderHP;
    public int enemyLeaderHP;
 
    public void AttackToLeader(CardController attackCard, bool isPlayerCard)
    {
        Debug.Log($"Leader Attack: {attackCard.model.name} → {(attackCard.model.isPlayerCard ? "EnemyLeader" : "PlayerLeader")}");
        
        // ゲーム終了時は攻撃処理を行わない
        if (gameEnded) return;
        
        if (attackCard.model.canAttack == false) return;
        
        // マルチプレイの場合は同期処理を追加
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (isMultiplayMode)
        {
            // マルチプレイでは攻撃をローカルで実行し、結果を相手に送信
            ExecuteLeaderAttack(attackCard);
            
            // 攻撃結果を相手に同期（ダメージのみ送信）
            string attackCardName = attackCard.model.name;
            int damage = attackCard.model.at;
            bool isAttackPlayerCard = attackCard.model.isPlayerCard;
            
            photonView.RPC("SyncLeaderAttack", RpcTarget.Others, 
                          attackCardName, damage, isAttackPlayerCard);
        }
        else
        {
            // シングルプレイの場合は従来通り
            ExecuteLeaderAttack(attackCard);
        }
    }
    
    void ExecuteLeaderAttack(CardController attackCard)
    {
        if (attackCard.model.isPlayerCard == true) // attackCardがプレイヤーのカードなら
        {
            enemyLeaderHP -= attackCard.model.at; // 敵のリーダーのHPを減らす
        }
        else // attackCardが敵のカードなら
        {
            playerLeaderHP -= attackCard.model.at; // プレイヤーのリーダーのHPを減らす
        }

        attackCard.model.canAttack = false;
        attackCard.view.SetCanAttackPanel(false);
        
        // 攻撃時イベントを0.5秒遅らせて発火（攻撃モーションとずらすため）
        StartCoroutine(TriggerAttackEventDelayed(attackCard, null, 0.5f));
        
        Debug.Log($"Leader HP - Player:{playerLeaderHP}, Enemy:{enemyLeaderHP}");
        ShowLeaderHP();
    }

    public void ShowLeaderHP()
    {
        if (playerLeaderHP <= 0)
        {
            playerLeaderHP = 0;
        }
        if (enemyLeaderHP <= 0)
        {
            enemyLeaderHP = 0;
        }
 
        // 既存のPlayerLeader/EnemyLeaderオブジェクトの子要素からHP Textを取得
        if (playerLeaderObject != null)
        {
            TextMeshProUGUI hpText = playerLeaderObject.transform.Find("HP")?.GetComponent<TextMeshProUGUI>();
            if (hpText != null)
                hpText.text = playerLeaderHP.ToString();
        }
        
        if (enemyLeaderObject != null)
        {
            TextMeshProUGUI hpText = enemyLeaderObject.transform.Find("HP")?.GetComponent<TextMeshProUGUI>();
            if (hpText != null)
                hpText.text = enemyLeaderHP.ToString();
        }
        
        // 勝敗判定
        CheckGameEnd();
    }
    
    void CheckGameEnd()
    {
        // ゲームが既に終了している場合は何もしない
        if (gameEnded) return;
        
        // モード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (playerLeaderHP <= 0)
        {
            // プレイヤーの敗北
            gameEnded = true;
            
            if (isMultiplayMode)
            {
                // マルチプレイでは相手に勝利を通知
                photonView.RPC("SyncGameEnd", RpcTarget.Others, true); // 相手に勝利を通知
                StartCoroutine(EndGame(false)); // 自分は敗北
            }
            else
            {
                StartCoroutine(EndGame(false));
            }
        }
        else if (enemyLeaderHP <= 0)
        {
            // プレイヤーの勝利
            gameEnded = true;
            
            if (isMultiplayMode)
            {
                // マルチプレイでは相手に敗北を通知
                photonView.RPC("SyncGameEnd", RpcTarget.Others, false); // 相手に敗北を通知
                StartCoroutine(EndGame(true)); // 自分は勝利
            }
            else
            {
                StartCoroutine(EndGame(true));
            }
        }
    }
    
    IEnumerator EndGame(bool playerWon)
    {
        Debug.Log($"ゲーム終了: プレイヤー勝利 = {playerWon}");
        
        // UIManagerのゲーム終了パネルを表示
        if (uIManager != null)
        {
            yield return StartCoroutine(uIManager.ShowGameEndPanel(playerWon));
        }
        else
        {
            Debug.LogError("UIManager が設定されていません");
        }
    }
    
    void ShowLeaderInfo()
    {
        // プレイヤーリーダーの情報を表示
        if (playerLeaderObject != null && playerLeader != null)
        {
            // 名前を表示
            TextMeshProUGUI nameText = playerLeaderObject.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = playerLeader.name;
                
            // アイコンを表示
            Image iconImage = playerLeaderObject.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && playerLeader.icon != null)
                iconImage.sprite = playerLeader.icon;
        }
        
        // 敵リーダーの情報を表示
        if (enemyLeaderObject != null && enemyLeader != null)
        {
            // 名前を表示
            TextMeshProUGUI nameText = enemyLeaderObject.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = enemyLeader.name;
                
            // アイコンを表示
            Image iconImage = enemyLeaderObject.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && enemyLeader.icon != null)
                iconImage.sprite = enemyLeader.icon;
        }
    }

    void ShowManaPoint() // マナポイントを表示するメソッド
    {
        playerManaPointText.text = playerManaPoint.ToString();
        playerDefaultManaPointText.text = playerDefaultManaPoint.ToString();
    }

    void ShowEnemyManaPoint() // 敵のマナポイントを表示するメソッド
    {
        enemyManaPointText.text = enemyManaPoint.ToString();
        enemyDefaultManaPointText.text = enemyDefaultManaPoint.ToString();
    }

    public void ReduceManaPoint(int cost) // コストの分、マナポイントを減らす
    {
        playerManaPoint -= cost;
        ShowManaPoint();
        SetCanUsePanelHand();
    }

    public void ReduceEnemyManaPoint(int cost) // 敵のコストの分、マナポイントを減らす
    {
        enemyManaPoint -= cost;
        ShowEnemyManaPoint();
        SetCanUsePanelEnemyHand();
    }
    public void SetCanUsePanelHand() // 手札のカードを取得して、使用可能なカードにCanUseパネルを付ける
    {        
        CardController[] playerHandCardList = PlayerHandTransform.GetComponentsInChildren<CardController>();
        foreach (CardController card in playerHandCardList)
        {
            if (card.model.cost <= playerManaPoint)
            {
                card.model.canUse = true;
                card.view.SetCanUsePanel(card.model.canUse);
            }
            else
            {
                card.model.canUse = false;
                card.view.SetCanUsePanel(card.model.canUse);
            }
        }
    }

    public void SetCanUsePanelEnemyHand() // 敵の手札のカードを取得して、使用可能なカードにCanUseパネルを付ける
    {        
        CardController[] enemyHandCardList = EnemyHandTransform.GetComponentsInChildren<CardController>();
        foreach (CardController card in enemyHandCardList)
        {
            if (card.model.cost <= enemyManaPoint)
            {
                card.model.canUse = true;
                card.view.SetCanUsePanel(card.model.canUse);
            }
            else
            {
                card.model.canUse = false;
                card.view.SetCanUsePanel(card.model.canUse);
            }
        }
    }
    
    public int GetCurrentTurn()
    {
        return currentTurn;
    }
    
    [PunRPC]
    void SyncDrawCard(int cardID, bool isPlayerCard)
    {
        Debug.Log($"ドロー同期受信: カードID {cardID}, プレイヤーカード: {isPlayerCard}");
        
        // マルチプレイでは視点が逆転するため、配置を逆転させる
        // 送信側: プレイヤーカード(true) → 自分の手札に追加
        // 受信側: プレイヤーカード(true) → 敵の手札に追加（相手のカードのため）
        Transform targetHand = isPlayerCard ? EnemyHandTransform : PlayerHandTransform;
        CreateCard(targetHand, cardID);
        
        Debug.Log($"ドローカード配置: カードID {cardID} → {targetHand.name}");
    }
    
    [PunRPC]
    void InitializeStartingHand(int[] playerCards, int[] enemyCards)
    {
        Debug.Log($"=== 初期手札同期受信 ===");
        Debug.Log($"プレイヤーカード {playerCards.Length}枚: [{string.Join(",", playerCards)}]");
        Debug.Log($"敵カード {enemyCards.Length}枚: [{string.Join(",", enemyCards)}]");
        Debug.Log($"自分はマスタークライアント: {PhotonNetwork.IsMasterClient}");
        
        // 配布前の手札数確認
        Debug.Log($"配布前 - PlayerHand: {PlayerHandTransform.childCount}枚, EnemyHand: {EnemyHandTransform.childCount}枚");
        
        // マルチプレイでは各プレイヤーが自分の視点で手札を配置
        // マスタークライアント: playerCards=自分, enemyCards=相手
        // 非マスタークライアント: playerCards=相手, enemyCards=自分
        
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("マスタークライアント視点で手札配布");
            // マスタークライアント視点: playerCards=自分の手札, enemyCards=相手の手札
            for (int i = 0; i < playerCards.Length; i++)
            {
                CreateCard(PlayerHandTransform, playerCards[i]);
                Debug.Log($"マスター自分手札: カードID {playerCards[i]} → PlayerHand");
            }
            
            for (int i = 0; i < enemyCards.Length; i++)
            {
                CreateCard(EnemyHandTransform, enemyCards[i]);
                Debug.Log($"マスター敵手札: カードID {enemyCards[i]} → EnemyHand");
            }
        }
        else
        {
            Debug.Log("非マスタークライアント視点で手札配布");
            // 非マスタークライアント視点: playerCards=相手の手札, enemyCards=自分の手札
            for (int i = 0; i < playerCards.Length; i++)
            {
                CreateCard(EnemyHandTransform, playerCards[i]);
                Debug.Log($"非マスター敵手札: カードID {playerCards[i]} → EnemyHand");
            }
            
            for (int i = 0; i < enemyCards.Length; i++)
            {
                CreateCard(PlayerHandTransform, enemyCards[i]);
                Debug.Log($"非マスター自分手札: カードID {enemyCards[i]} → PlayerHand");
            }
        }
        
        // 配布後の手札数確認
        Debug.Log($"配布後 - PlayerHand: {PlayerHandTransform.childCount}枚, EnemyHand: {EnemyHandTransform.childCount}枚");
        Debug.Log("=== 初期手札同期完了 ===");
    }
    
    [PunRPC]
    void SyncBattleResult(string attackCardName, string defenceCardName, int attackCardHp, int defenceCardHp, bool attackCardDestroyed, bool defenceCardDestroyed)
    {
        Debug.Log($"戦闘結果同期受信: 攻撃 {attackCardName} vs 防御 {defenceCardName}");
        
        // 攻撃カード（相手のカード）をEnemyFieldで検索
        CardController attackCard = FindCardInField(attackCardName, EnemyFieldTransform);
        
        // 防御カード（自分のカード）をPlayerFieldで検索  
        CardController defenceCard = FindCardInField(defenceCardName, PlayerFieldTransform);
        
        if (attackCard != null)
        {
            // 攻撃カードのHP更新（反撃ダメージ）
            attackCard.model.hp = attackCardHp;
            attackCard.view.Show(attackCard.model);
            Debug.Log($"攻撃カード更新: {attackCardName} HP:{attackCardHp}");
            
            // 攻撃カード破壊処理
            if (attackCardDestroyed)
            {
                Debug.Log($"攻撃カード破壊: {attackCardName}");
                attackCard.DestroyCard(attackCard);
            }
        }
        else
        {
            Debug.LogWarning($"攻撃カードが見つかりません: {attackCardName}");
        }
        
        if (defenceCard != null)
        {
            // 防御カードのHP更新
            defenceCard.model.hp = defenceCardHp;
            defenceCard.view.Show(defenceCard.model);
            Debug.Log($"防御カード更新: {defenceCardName} HP:{defenceCardHp}");
            
            // 防御カード破壊処理
            if (defenceCardDestroyed)
            {
                Debug.Log($"防御カード破壊: {defenceCardName}");
                defenceCard.DestroyCard(defenceCard);
            }
        }
        else
        {
            Debug.LogWarning($"防御カードが見つかりません: {defenceCardName}");
        }
    }
    
    [PunRPC]
    void SyncLeaderAttack(string attackCardName, int damage, bool isAttackPlayerCard)
    {
        Debug.Log($"リーダー攻撃同期受信: {attackCardName} → {(isAttackPlayerCard ? "EnemyLeader" : "PlayerLeader")}, ダメージ {damage}");
        
        // マルチプレイでは視点が逆転するため、攻撃元の判定を逆転させる
        // 送信側: プレイヤーカード(true) → 敵リーダー攻撃
        // 受信側: 敵カード(true) → プレイヤーリーダー攻撃
        if (isAttackPlayerCard)
        {
            // 送信側でプレイヤーカードが攻撃 → 受信側では敵カードがプレイヤーリーダーを攻撃
            playerLeaderHP -= damage;
            Debug.Log($"受信側: 敵カード({attackCardName})がプレイヤーリーダーを攻撃, ダメージ {damage}");
        }
        else
        {
            // 送信側で敵カードが攻撃 → 受信側ではプレイヤーカードが敵リーダーを攻撃  
            enemyLeaderHP -= damage;
            Debug.Log($"受信側: プレイヤーカード({attackCardName})が敵リーダーを攻撃, ダメージ {damage}");
        }
        
        // 攻撃カードを検索してcanAttackをfalseに設定（視点逆転で検索）
        CardController attackCard = FindCardByName(attackCardName);
        if (attackCard != null)
        {
            attackCard.model.canAttack = false;
            attackCard.view.SetCanAttackPanel(false);
            Debug.Log($"攻撃カード状態更新: {attackCardName} canAttack=false");
        }
        else
        {
            Debug.LogWarning($"攻撃カードが見つかりません: {attackCardName}");
        }
        
        // UI更新
        ShowLeaderHP();
        
        Debug.Log($"リーダーHP更新完了 - Player:{playerLeaderHP}, Enemy:{enemyLeaderHP}");
    }
    
    // 古いSyncCardBattleは使用しない（互換性のためコメントアウト）
    /*
    [PunRPC]
    void SyncCardBattle(int attackCardID, int defenceCardID, bool attackIsPlayerCard, bool defenceIsPlayerCard, int attackDamage, int defenceDamage)
    {
        Debug.Log($"カードバトル同期受信: 攻撃カード {attackCardID} vs 防御カード {defenceCardID}");
        
        // カードを検索
        CardController attackCard = FindCardByID(attackCardID, attackIsPlayerCard);
        CardController defenceCard = FindCardByID(defenceCardID, defenceIsPlayerCard);
        
        if (attackCard != null && defenceCard != null)
        {
            ExecuteCardBattle(attackCard, defenceCard);
        }
        else
        {
            Debug.LogWarning($"カードバトル同期エラー: カードが見つかりません。攻撃カード={attackCard}, 防御カード={defenceCard}");
        }
    }
    */

    [PunRPC]
    void InitializeTurnOrder(bool masterGoesFirst)
    {
        Debug.Log("=== ターン順序確定 ===");
        
        // 各プレイヤーの視点で先攻・後攻を設定
        if (PhotonNetwork.IsMasterClient)
        {
            // マスタークライアント視点
            isPlayerTurn = masterGoesFirst;
            string turnOrder = masterGoesFirst ? "先攻" : "後攻";
            Debug.Log($"【マスタークライアント】あなたは{turnOrder}です");
            Debug.Log($"最初のターン: {(isPlayerTurn ? "あなたのターン" : "相手のターン")}");
        }
        else
        {
            // 非マスタークライアント視点（逆になる）
            isPlayerTurn = !masterGoesFirst;
            string turnOrder = !masterGoesFirst ? "先攻" : "後攻";
            Debug.Log($"【非マスタークライアント】あなたは{turnOrder}です");
            Debug.Log($"最初のターン: {(isPlayerTurn ? "あなたのターン" : "相手のターン")}");
        }
        
        Debug.Log("=================");
        
        // ゲーム開始
        TurnCalc();
    }
    
    [PunRPC]
    void SyncTurnChange()
    {
        Debug.Log("ターン開始通知受信");
        
        // ゲーム終了時はターン切り替えを行わない
        if (gameEnded) return;
        
        // 自分のターンを開始（Player Turnへ）
        isPlayerTurn = true;
        currentTurn++;
        
        // 自分のターンになったらドロー
        if (PlayerHandTransform.childCount < 5)
        {
            int randomID = UnityEngine.Random.Range(1, 41);
            CreateCard(PlayerHandTransform, randomID);
            
            // ドロー情報を相手に同期
            if (photonView != null)
            {
                photonView.RPC("SyncDrawCard", RpcTarget.Others, randomID, true);
            }
        }
        
        // プレイヤーターン処理を開始
        PlayerTurn();
        
        // Your Turnパネルを表示
        StartCoroutine(uIManager.ShowChangeTurnPanel(true));
    }
    
    CardController FindCardInField(string cardName, Transform searchField)
    {
        // 指定されたフィールドでカード名により検索
        CardController[] fieldCards = searchField.GetComponentsInChildren<CardController>();
        
        foreach (CardController card in fieldCards)
        {
            if (card.model.name == cardName)
            {
                return card;
            }
        }
        
        return null;
    }
    
    CardController FindCardByName(string cardName)
    {
        // 両方のフィールドでカード名により検索
        Transform[] searchFields = { PlayerFieldTransform, EnemyFieldTransform };
        
        Debug.Log($"FindCardByName: カード名 '{cardName}'");
        
        foreach (Transform searchField in searchFields)
        {
            CardController[] fieldCards = searchField.GetComponentsInChildren<CardController>();
            
            foreach (CardController card in fieldCards)
            {
                if (card.model.name == cardName)
                {
                    Debug.Log($"カード発見: {card.model.name} (フィールド: {searchField.name})");
                    return card;
                }
            }
        }
        
        Debug.LogWarning($"カードが見つかりません: 名前 '{cardName}'");
        return null;
    }
    
    CardController FindCardByID(int cardID, bool isPlayerCard)
    {
        // 両方のフィールドでカードを検索（マルチプレイ対応）
        Transform[] searchFields = { PlayerFieldTransform, EnemyFieldTransform };
        
        Debug.Log($"FindCardByID: カードID {cardID}, isPlayerCard {isPlayerCard}");
        
        foreach (Transform searchField in searchFields)
        {
            CardController[] fieldCards = searchField.GetComponentsInChildren<CardController>();
            Debug.Log($"検索フィールド: {searchField.name}, カード数: {fieldCards.Length}");
            
            foreach (CardController card in fieldCards)
            {
                Debug.Log($"フィールドカード: {card.model.name}, ID {card.model.id}, isPlayerCard {card.model.isPlayerCard}");
                if (card.model.id == cardID && card.model.isPlayerCard == isPlayerCard)
                {
                    Debug.Log($"カード発見: {card.model.name} (フィールド: {searchField.name})");
                    return card;
                }
            }
        }
        
        Debug.LogWarning($"カードが見つかりません: ID {cardID}, isPlayerCard {isPlayerCard}");
        return null;
    }
    
    [PunRPC]
    void SyncGameEnd(bool playerWon)
    {
        Debug.Log($"ゲーム終了同期受信: プレイヤー勝利 = {playerWon}");
        
        // ゲームが既に終了している場合は重複処理を避ける
        if (gameEnded) return;
        
        gameEnded = true;
        StartCoroutine(EndGame(playerWon));
    }
    
    [PunRPC]
    void SyncEffectTrigger(string cardName, int eventType, bool isPlayerCard)
    {
        Debug.Log($"特殊効果発動同期受信: カード={cardName}, イベント={eventType}, プレイヤーカード={isPlayerCard}");
        
        // カードを検索
        CardController sourceCard = FindCardByName(cardName);
        if (sourceCard == null)
        {
            Debug.LogWarning($"効果発動カードが見つかりません: {cardName}");
            return;
        }
        
        // イベントデータを作成（マルチプレイでは送信側と受信側でisPlayerEvent が逆転）
        var eventData = new GameEventData((GameEventType)eventType, sourceCard, !isPlayerCard);
        
        // EffectProcessorに処理を依頼（該当するトリガーの全ての効果を処理）
        if (EffectProcessor.Instance != null && sourceCard.model.cardEffects != null)
        {
            var effects = sourceCard.model.cardEffects.GetEffectsForTrigger((GameEventType)eventType);
            foreach (var effect in effects)
            {
                // 同期処理を回避するため、直接効果を処理（ProcessEffect内の同期処理をスキップ）
                ProcessEffectDirectly(effect, eventData);
            }
        }
    }
    
    // 同期処理をスキップして効果を直接処理
    void ProcessEffectDirectly(CardEffectData effect, GameEventData eventData)
    {
        if (EffectProcessor.Instance == null) return;
        
        // 対象選択が必要な場合
        if (effect.target == EffectTarget.SelfUnit || effect.target == EffectTarget.EnemyUnit)
        {
            // マルチプレイではランダム選択にフォールバック
            var targets = GetEffectTargets(effect.target, eventData);
            ExecuteEffectForSync(effect, targets, eventData);
        }
        else
        {
            // 自動選択の場合
            var targets = GetEffectTargets(effect.target, eventData);
            ExecuteEffectForSync(effect, targets, eventData);
        }
    }
    
    // 同期用の効果実行（UIパネル表示とRPC送信をスキップ）
    void ExecuteEffectForSync(CardEffectData effect, List<object> targets, GameEventData eventData)
    {
        // UIManagerがある場合は効果パネルを表示
        if (uIManager != null)
        {
            StartCoroutine(uIManager.ShowEffectPanel(effect.description));
        }
        
        // 効果を実際に適用（同期はしない）
        foreach (var target in targets)
        {
            ApplyEffectDirect(GetEffectTypeFromCardEffect(effect), target, effect.value);
        }
    }
    
    // 効果対象を取得
    List<object> GetEffectTargets(EffectTarget targetType, GameEventData eventData)
    {
        var targets = new List<object>();
        
        switch (targetType)
        {
            case EffectTarget.SelfLeader:
                if (eventData.isPlayerEvent)
                    targets.Add(playerLeader);
                else
                    targets.Add(enemyLeader);
                break;
                
            case EffectTarget.EnemyLeader:
                if (eventData.isPlayerEvent)
                    targets.Add(enemyLeader);
                else
                    targets.Add(playerLeader);
                break;
                
            case EffectTarget.SelfUnit:
                var selfUnits = GetUnitsOnField(eventData.isPlayerEvent);
                if (selfUnits.Count > 0)
                {
                    var randomIndex = UnityEngine.Random.Range(0, selfUnits.Count);
                    targets.Add(selfUnits[randomIndex]);
                }
                break;
                
            case EffectTarget.EnemyUnit:
                var enemyUnits = GetUnitsOnField(!eventData.isPlayerEvent);
                if (enemyUnits.Count > 0)
                {
                    var randomIndex = UnityEngine.Random.Range(0, enemyUnits.Count);
                    targets.Add(enemyUnits[randomIndex]);
                }
                break;
                
            case EffectTarget.AllSelfUnits:
                targets.AddRange(GetUnitsOnField(eventData.isPlayerEvent).Cast<object>());
                break;
                
            case EffectTarget.AllEnemyUnits:
                targets.AddRange(GetUnitsOnField(!eventData.isPlayerEvent).Cast<object>());
                break;
                
            case EffectTarget.AllEnemies:
                if (eventData.isPlayerEvent)
                    targets.Add(enemyLeader);
                else
                    targets.Add(playerLeader);
                targets.AddRange(GetUnitsOnField(!eventData.isPlayerEvent).Cast<object>());
                break;
        }
        
        return targets;
    }
    
    // フィールドのユニットを取得
    List<CardController> GetUnitsOnField(bool isPlayer)
    {
        Transform fieldTransform = isPlayer ? PlayerFieldTransform : EnemyFieldTransform;
        var units = new List<CardController>();
        
        foreach (Transform child in fieldTransform)
        {
            var card = child.GetComponent<CardController>();
            if (card != null)
            {
                units.Add(card);
            }
        }
        return units;
    }
    
    // CardEffectDataからEffectTypeを取得
    EffectType GetEffectTypeFromCardEffect(CardEffectData effect)
    {
        return effect.effectType;
    }
    
    [PunRPC]
    void SyncEffectResult(string description, string targetName, int effectType, int value, bool targetIsPlayerCard)
    {
        Debug.Log($"特殊効果結果同期受信: {description} → {targetName}, タイプ={effectType}, 値={value}");
        
        // UIパネルを表示
        if (uIManager != null)
        {
            StartCoroutine(uIManager.ShowEffectPanel(description));
        }
        
        // 効果を適用
        ApplySyncedEffect(targetName, (EffectType)effectType, value, targetIsPlayerCard);
    }
    
    void ApplySyncedEffect(string targetName, EffectType effectType, int value, bool targetIsPlayerCard)
    {
        object target = null;
        
        // リーダーかカードかを判定して対象を取得
        if (targetName == "PlayerLeader")
        {
            target = playerLeader;
        }
        else if (targetName == "EnemyLeader")
        {
            target = enemyLeader;
        }
        else
        {
            // カード名で検索
            CardController card = FindCardByName(targetName);
            if (card != null)
            {
                target = card;
            }
        }
        
        if (target == null)
        {
            Debug.LogWarning($"効果対象が見つかりません: {targetName}");
            return;
        }
        
        // 効果を適用
        ApplyEffectDirect(effectType, target, value);
    }
    
    void ApplyEffectDirect(EffectType effectType, object target, int value)
    {
        switch (effectType)
        {
            case EffectType.HealHP:
                if (target is LeaderModel leader)
                {
                    leader.currentHp = Mathf.Min(leader.currentHp + value, leader.maxHp);
                    ShowLeaderHP();
                }
                else if (target is CardController card)
                {
                    card.model.hp = Mathf.Min(card.model.hp + value, card.model.hp + value);
                    card.view.Show(card.model);
                }
                break;
                
            case EffectType.Damage:
                if (target is LeaderModel leader2)
                {
                    leader2.currentHp = Mathf.Max(leader2.currentHp - value, 0);
                    ShowLeaderHP();
                }
                else if (target is CardController card2)
                {
                    card2.model.hp = Mathf.Max(card2.model.hp - value, 0);
                    card2.view.Show(card2.model);
                    
                    if (card2.model.hp <= 0)
                    {
                        card2.DestroyCard(card2);
                    }
                }
                break;
                
            case EffectType.IncreaseHP:
                if (target is CardController card3)
                {
                    card3.model.hp += value;
                    card3.view.Show(card3.model);
                }
                break;
                
            case EffectType.IncreaseAttack:
                if (target is CardController card4)
                {
                    card4.model.at += value;
                    card4.view.Show(card4.model);
                }
                break;
                
            case EffectType.DecreaseAttack:
                if (target is CardController card5)
                {
                    card5.model.at = Mathf.Max(card5.model.at - value, 0);
                    card5.view.Show(card5.model);
                }
                break;
        }
    }
}