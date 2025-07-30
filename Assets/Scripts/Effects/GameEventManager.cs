using UnityEngine;
using System;

// ゲーム内イベントの種類
public enum GameEventType
{
    None,
    OnSummon,    // 登場時
    OnAttack,    // 攻撃時
    OnDeath,     // 死亡時
    OnTurnStart, // ターン開始時
    OnTurnEnd    // ターン終了時
}

// イベントデータを格納するクラス
[System.Serializable]
public class GameEventData
{
    public GameEventType eventType;
    public CardController sourceCard;    // イベントを発生させたカード
    public CardController targetCard;    // ターゲットカード（攻撃時など）
    public bool isPlayerEvent;           // プレイヤー側のイベントかどうか
    
    public GameEventData(GameEventType type, CardController source, bool isPlayer = true)
    {
        eventType = type;
        sourceCard = source;
        targetCard = null;
        isPlayerEvent = isPlayer;
    }
    
    public GameEventData(GameEventType type, CardController source, CardController target, bool isPlayer = true)
    {
        eventType = type;
        sourceCard = source;
        targetCard = target;
        isPlayerEvent = isPlayer;
    }
}

// イベント通知システム
public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }
    
    // イベント通知
    public static event Action<GameEventData> OnGameEvent;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // 静的イベントハンドラーをクリア
        OnGameEvent = null;
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    // イベントを発火
    public static void TriggerEvent(GameEventData eventData)
    {
        Debug.Log($"Event Triggered: {eventData.eventType} by {eventData.sourceCard?.model.name}");
        OnGameEvent?.Invoke(eventData);
    }
    
    // 登場時イベント
    public static void TriggerSummonEvent(CardController card)
    {
        Debug.Log($"TriggerSummonEvent: カード {card.model.name} の登場時イベントを開始");
        var eventData = new GameEventData(GameEventType.OnSummon, card, card.model.isPlayerCard);
        TriggerEvent(eventData);
    }
    
    // 攻撃時イベント
    public static void TriggerAttackEvent(CardController attacker, CardController target)
    {
        var eventData = new GameEventData(GameEventType.OnAttack, attacker, target, attacker.model.isPlayerCard);
        TriggerEvent(eventData);
    }
    
    // 死亡時イベント
    public static void TriggerDeathEvent(CardController card)
    {
        var eventData = new GameEventData(GameEventType.OnDeath, card, card.model.isPlayerCard);
        TriggerEvent(eventData);
    }
}