using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

// 効果処理システム
public class EffectProcessor : MonoBehaviour
{
    public static EffectProcessor Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // イベントシステムに登録
            GameEventManager.OnGameEvent += HandleGameEvent;
            Debug.Log("EffectProcessor: インスタンス初期化完了。GameEventManagerのイベントリスナーを登録しました。");
        }
        else
        {
            Debug.LogWarning("EffectProcessor: 既にインスタンスが存在するため、このオブジェクトを破棄します。");
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // イベントシステムから解除
        GameEventManager.OnGameEvent -= HandleGameEvent;
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // ゲームイベントを処理
    void HandleGameEvent(GameEventData eventData)
    {
        Debug.Log($"EffectProcessor.HandleGameEvent: イベント受信 - Type: {eventData.eventType}, Card: {eventData.sourceCard?.model?.name}");
        
        if (eventData.sourceCard == null)
        {
            Debug.LogWarning("HandleGameEvent: sourceCardがnullです");
            return;
        }
        
        if (eventData.sourceCard.model.cardEffects == null)
        {
            Debug.LogWarning($"HandleGameEvent: カード {eventData.sourceCard.model.name} のcardEffectsがnullです");
            return;
        }
        
        // 該当するトリガーの効果を取得
        var effects = eventData.sourceCard.model.cardEffects.GetEffectsForTrigger(eventData.eventType);
        Debug.Log($"HandleGameEvent: トリガー {eventData.eventType} に対応する効果数: {effects.Count}");
        
        foreach (var effect in effects)
        {
            Debug.Log($"HandleGameEvent: 効果を処理します - {effect.description}");
            ProcessEffect(effect, eventData);
        }
    }
    
    // 効果を処理
    public void ProcessEffect(CardEffectData effect, GameEventData eventData)
    {
        Debug.Log($"Processing effect: {effect.description}");
        
        // マルチプレイモード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        // マルチプレイで自分のカードの効果の場合、相手に同期
        // （攻撃時効果も含め、自分がコントロールしているカードの効果のみ）
        bool shouldSync = isMultiplayMode && 
                         eventData.sourceCard != null && 
                         eventData.sourceCard.model.isPlayerCard && 
                         eventData.isPlayerEvent;
        
        if (shouldSync)
        {
            var gameManager = GameManagerCardBattle.instance;
            if (gameManager != null && gameManager.photonView != null)
            {
                gameManager.photonView.RPC("SyncEffectTrigger", RpcTarget.Others, 
                                         eventData.sourceCard.model.name, 
                                         (int)eventData.eventType, 
                                         eventData.sourceCard.model.isPlayerCard);
            }
        }
        
        // 対象選択が必要かチェック
        if (NeedsTargetSelection(effect.target))
        {
            // EffectTargetSelectorが存在するかチェック
            if (EffectTargetSelector.instance != null)
            {
                // 対象選択システムを使用
                EffectTargetSelector.instance.StartTargetSelection(effect.target, effect, eventData, (selectedCard) =>
                {
                    Debug.Log($"EffectProcessor: コールバック実行開始 - 選択されたカード: {selectedCard?.model?.name}");
                    // 選択されたカードで効果を実行（パネル表示なし版）
                    var targets = new List<object> { selectedCard };
                    ExecuteEffectDirect(effect, targets, eventData);
                    Debug.Log($"EffectProcessor: コールバック実行完了");
                });
            }
            else
            {
                Debug.LogWarning("EffectTargetSelector.instanceがnullです。ランダム選択にフォールバックします。");
                // フォールバック：ランダム選択
                var targets = GetTargets(effect.target, eventData);
                ExecuteEffect(effect, targets, eventData);
            }
        }
        else
        {
            // 従来通りの処理（自動選択）
            var targets = GetTargets(effect.target, eventData);
            ExecuteEffect(effect, targets, eventData);
        }
    }
    
    // 対象選択が必要かどうかを判定
    bool NeedsTargetSelection(EffectTarget targetType)
    {
        return targetType == EffectTarget.SelfUnit || targetType == EffectTarget.EnemyUnit;
    }
    
    // ターゲットを取得
    List<object> GetTargets(EffectTarget targetType, GameEventData eventData)
    {
        var targets = new List<object>();
        var gameManager = GameManagerCardBattle.instance;
        
        switch (targetType)
        {
            case EffectTarget.SelfLeader:
                if (eventData.isPlayerEvent)
                    targets.Add(gameManager.playerLeader);
                else
                    targets.Add(gameManager.enemyLeader);
                break;
                
            case EffectTarget.EnemyLeader:
                if (eventData.isPlayerEvent)
                    targets.Add(gameManager.enemyLeader);
                else
                    targets.Add(gameManager.playerLeader);
                break;
                
            case EffectTarget.SelfUnit:
                // 自ユニット1体（ランダム選択）
                var selfUnits = GetUnitsOnField(eventData.isPlayerEvent);
                if (selfUnits.Count > 0)
                {
                    var randomIndex = Random.Range(0, selfUnits.Count);
                    targets.Add(selfUnits[randomIndex]);
                }
                break;
                
            case EffectTarget.EnemyUnit:
                // 敵ユニット1体（ランダム選択）
                var enemyUnits = GetUnitsOnField(!eventData.isPlayerEvent);
                if (enemyUnits.Count > 0)
                {
                    var randomIndex = Random.Range(0, enemyUnits.Count);
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
                // 敵リーダー + 敵ユニット全体
                if (eventData.isPlayerEvent)
                    targets.Add(gameManager.enemyLeader);
                else
                    targets.Add(gameManager.playerLeader);
                targets.AddRange(GetUnitsOnField(!eventData.isPlayerEvent).Cast<object>());
                break;
        }
        
        return targets;
    }
    
    // フィールドのユニットを取得
    List<CardController> GetUnitsOnField(bool isPlayer)
    {
        var gameManager = GameManagerCardBattle.instance;
        Transform fieldTransform = isPlayer ? gameManager.PlayerFieldTransform : gameManager.EnemyFieldTransform;
        
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
    
    // 効果を実行（パネル表示なし版）
    public void ExecuteEffectDirect(CardEffectData effect, List<object> targets, GameEventData eventData)
    {
        Debug.Log($"ExecuteEffectDirect開始: {effect.description}, ターゲット数: {targets.Count}");
        
        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("ExecuteEffectDirect: ターゲットが存在しません");
            return;
        }
        
        // 効果を実際に適用（パネル表示なし）
        foreach (var target in targets)
        {
            Debug.Log($"ExecuteEffectDirect: ターゲット {target} に効果を適用中");
            ApplyEffectToTarget(effect, target, eventData);
        }
        
        Debug.Log("ExecuteEffectDirect完了");
    }
    
    // 効果を実行（パネル表示あり版）
    void ExecuteEffect(CardEffectData effect, List<object> targets, GameEventData eventData)
    {
        // 効果パネルを表示してから効果を実行
        GameManagerCardBattle.instance.StartCoroutine(ExecuteEffectWithPanel(effect, targets, eventData));
    }
    
    // パネル表示付きの効果実行
    System.Collections.IEnumerator ExecuteEffectWithPanel(CardEffectData effect, List<object> targets, GameEventData eventData)
    {
        // UIManagerがある場合は効果パネルを表示
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            // 効果の説明を表示
            yield return uiManager.ShowEffectPanel(effect.description);
        }
        
        // 効果を実際に適用
        foreach (var target in targets)
        {
            ApplyEffectToTarget(effect, target, eventData);
        }
    }
    
    // 個別の効果をターゲットに適用
    void ApplyEffectToTarget(CardEffectData effect, object target, GameEventData eventData)
    {
        Debug.Log($"ApplyEffectToTarget: {effect.effectType} を {target} に適用");
        Debug.Log($"ApplyEffectToTarget: 効果値={effect.value}, 効果の説明={effect.description}");
        
        // マルチプレイモード判定
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        // マルチプレイで自分のカードの効果の場合、相手に結果を同期
        bool shouldSyncResult = isMultiplayMode && 
                               eventData.sourceCard != null && 
                               eventData.sourceCard.model.isPlayerCard && 
                               eventData.isPlayerEvent;
        
        if (shouldSyncResult)
        {
            SyncEffectResult(effect, target, eventData);
        }
        
        switch (effect.effectType)
        {
            case EffectType.HealHP:
                ApplyHeal(target, effect.value);
                break;
                
            case EffectType.IncreaseHP:
                ApplyHPIncrease(target, effect.value);
                break;
                
            case EffectType.Damage:
                ApplyDamage(target, effect.value);
                break;
                
            case EffectType.IncreaseAttack:
                ApplyAttackIncrease(target, effect.value);
                break;
                
            case EffectType.DecreaseAttack:
                ApplyAttackDecrease(target, effect.value);
                break;
                
            case EffectType.ReviveCard:
                ApplyReviveCard(effect.value, eventData.isPlayerEvent);
                break;
        }
    }
    
    // マルチプレイ用効果結果同期
    void SyncEffectResult(CardEffectData effect, object target, GameEventData eventData)
    {
        var gameManager = GameManagerCardBattle.instance;
        if (gameManager == null || gameManager.photonView == null) return;
        
        string targetName = "";
        bool targetIsPlayerCard = false;
        
        if (target is LeaderModel leader)
        {
            // リーダーの判定（視点による違いを考慮）
            if (leader == gameManager.playerLeader)
                targetName = "PlayerLeader";
            else if (leader == gameManager.enemyLeader)
                targetName = "EnemyLeader";
        }
        else if (target is CardController card)
        {
            targetName = card.model.name;
            targetIsPlayerCard = card.model.isPlayerCard;
        }
        
        if (!string.IsNullOrEmpty(targetName))
        {
            gameManager.photonView.RPC("SyncEffectResult", RpcTarget.Others,
                                     effect.description,
                                     targetName,
                                     (int)effect.effectType,
                                     effect.value,
                                     targetIsPlayerCard);
        }
    }
    
    // HP回復効果
    void ApplyHeal(object target, int value)
    {
        if (target is LeaderModel leader)
        {
            leader.currentHp = Mathf.Min(leader.currentHp + value, leader.maxHp);
            GameManagerCardBattle.instance.ShowLeaderHP();
            Debug.Log($"Leader healed for {value} HP");
        }
        else if (target is CardController card)
        {
            card.model.hp = Mathf.Min(card.model.hp + value, card.model.hp + value); // 最大HPの概念がないため、そのまま回復
            Debug.Log($"Card {card.model.name} healed for {value} HP");
        }
    }
    
    // HP増加効果（永続）
    void ApplyHPIncrease(object target, int value)
    {
        if (target is CardController card)
        {
            card.model.hp += value;
            Debug.Log($"Card {card.model.name} HP increased by {value}");
        }
    }
    
    // ダメージ効果
    void ApplyDamage(object target, int value)
    {
        Debug.Log($"ApplyDamage開始: value={value}, target={target}");
        
        if (target is LeaderModel leader)
        {
            leader.currentHp = Mathf.Max(leader.currentHp - value, 0);
            GameManagerCardBattle.instance.ShowLeaderHP();
            Debug.Log($"Leader took {value} damage");
        }
        else if (target is CardController card)
        {
            if (card == null)
            {
                Debug.LogWarning("ApplyDamage: target CardController is null");
                return;
            }
            
            Debug.Log($"ApplyDamage: {card.model.name}に{value}ダメージを与えます（現在HP: {card.model.hp}）");
            card.model.hp = Mathf.Max(card.model.hp - value, 0);
            Debug.Log($"Card {card.model.name} took {value} damage, HP: {card.model.hp}");
            
            // UIを更新
            card.view.Show(card.model);
            
            // HPが0以下になったら破壊
            if (card.model.hp <= 0)
            {
                Debug.Log($"Card {card.model.name} destroyed by damage effect");
                card.DestroyCard(card);
            }
        }
    }
    
    // 攻撃力増加効果
    void ApplyAttackIncrease(object target, int value)
    {
        if (target is CardController card)
        {
            card.model.at += value;
            Debug.Log($"Card {card.model.name} attack increased by {value}");
        }
    }
    
    // 攻撃力減少効果
    void ApplyAttackDecrease(object target, int value)
    {
        if (target is CardController card)
        {
            card.model.at = Mathf.Max(card.model.at - value, 0);
            Debug.Log($"Card {card.model.name} attack decreased by {value}");
        }
    }
    
    // カード復活効果（墓地から手札に戻す）
    void ApplyReviveCard(int count, bool isPlayerCard)
    {
        // 墓地システムが実装されていないため、現在はログのみ
        Debug.Log($"Revive {count} card(s) from graveyard (Player: {isPlayerCard})");
        // TODO: 墓地システム実装後に実際の復活処理を追加
    }
}