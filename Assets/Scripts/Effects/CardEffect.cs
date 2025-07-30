using UnityEngine;
using System.Collections.Generic;

// 効果のターゲット種類
public enum EffectTarget
{
    None,
    SelfLeader,        // 自リーダー
    EnemyLeader,       // 敵リーダー
    SelfUnit,          // 自ユニット1体
    EnemyUnit,         // 敵ユニット1体
    AllSelfUnits,      // 自ユニット全体
    AllEnemyUnits,     // 敵ユニット全体
    AllEnemies,        // 敵全体（リーダー+ユニット）
    Graveyard          // 墓地
}

// 効果の種類
public enum EffectType
{
    None,
    HealHP,            // HP回復
    IncreaseHP,        // HP増加（永続）
    Damage,            // ダメージ
    IncreaseAttack,    // 攻撃力増加
    DecreaseAttack,    // 攻撃力減少
    ReviveCard         // カード復活
}

// カード効果データ
[System.Serializable]
public class CardEffectData
{
    public GameEventType trigger;    // いつ発動するか
    public EffectTarget target;      // 誰に効果をかけるか
    public EffectType effectType;    // どんな効果か
    public int value;                // 効果の値
    public string description;       // 効果の説明文
    
    public CardEffectData()
    {
        trigger = GameEventType.None;
        target = EffectTarget.None;
        effectType = EffectType.None;
        value = 0;
        description = "";
    }
    
    public CardEffectData(GameEventType triggerType, EffectTarget targetType, EffectType effect, int effectValue, string desc = "")
    {
        trigger = triggerType;
        target = targetType;
        effectType = effect;
        value = effectValue;
        description = desc;
    }
    
    // CSVの文字列から効果データを解析
    public static CardEffectData ParseFromString(string effectString)
    {
        if (string.IsNullOrEmpty(effectString) || effectString == "※効果なし" || effectString == "効果なし" || effectString.Trim() == "")
        {
            return new CardEffectData();
        }
        
        try
        {
            var effect = new CardEffectData();
            effect.description = effectString;
        
        // 効果文字列の解析
        if (effectString.Contains("登場時"))
        {
            effect.trigger = GameEventType.OnSummon;
        }
        else if (effectString.Contains("攻撃時"))
        {
            effect.trigger = GameEventType.OnAttack;
        }
        else if (effectString.Contains("死亡時"))
        {
            effect.trigger = GameEventType.OnDeath;
        }
        
        // ターゲットの解析
        if (effectString.Contains("自リーダー"))
        {
            effect.target = EffectTarget.SelfLeader;
        }
        else if (effectString.Contains("敵リーダー"))
        {
            effect.target = EffectTarget.EnemyLeader;
        }
        else if (effectString.Contains("自ユニット1体"))
        {
            effect.target = EffectTarget.SelfUnit;
        }
        else if (effectString.Contains("敵ユニット1体"))
        {
            effect.target = EffectTarget.EnemyUnit;
        }
        else if (effectString.Contains("自ユニット全体"))
        {
            effect.target = EffectTarget.AllSelfUnits;
        }
        else if (effectString.Contains("敵ユニット全体") || effectString.Contains("敵全体"))
        {
            effect.target = EffectTarget.AllEnemyUnits;
        }
        else if (effectString.Contains("墓地"))
        {
            effect.target = EffectTarget.Graveyard;
        }
        
        // 効果タイプと値の解析
        if (effectString.Contains("回復"))
        {
            effect.effectType = EffectType.HealHP;
            effect.value = ExtractNumber(effectString);
        }
        else if (effectString.Contains("ダメージ"))
        {
            effect.effectType = EffectType.Damage;
            effect.value = ExtractNumber(effectString);
        }
        else if (effectString.Contains("HP+") || effectString.Contains("HPを+"))
        {
            effect.effectType = EffectType.IncreaseHP;
            effect.value = ExtractNumber(effectString);
        }
        else if (effectString.Contains("攻撃力+") || (effectString.Contains("攻撃力") && effectString.Contains("+")))
        {
            effect.effectType = EffectType.IncreaseAttack;
            effect.value = ExtractNumber(effectString);
        }
        else if (effectString.Contains("攻撃力−") || effectString.Contains("攻撃力-") || (effectString.Contains("攻撃力") && (effectString.Contains("−") || effectString.Contains("-"))))
        {
            effect.effectType = EffectType.DecreaseAttack;
            effect.value = ExtractNumber(effectString);
        }
        else if (effectString.Contains("手札に戻す"))
        {
            effect.effectType = EffectType.ReviveCard;
            effect.value = ExtractNumber(effectString);
        }
        
        // 効果が正しく解析されたかを検証
        if (effect.trigger == GameEventType.None)
        {
            Debug.LogWarning($"効果のトリガーが解析できませんでした: {effectString}");
        }
        else
        {
            Debug.Log($"トリガー解析成功: {effect.trigger}");
        }
        
        if (effect.target == EffectTarget.None)
        {
            Debug.LogWarning($"効果のターゲットが解析できませんでした: {effectString}");
        }
        else
        {
            Debug.Log($"ターゲット解析成功: {effect.target}");
        }
        
        if (effect.effectType == EffectType.None)
        {
            Debug.LogWarning($"効果のタイプが解析できませんでした: {effectString}");
        }
        else
        {
            Debug.Log($"効果タイプ解析成功: {effect.effectType}, 値: {effect.value}");
        }
        
        return effect;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"効果の解析中にエラーが発生しました: {effectString}, エラー: {e.Message}");
            var errorEffect = new CardEffectData();
            errorEffect.description = effectString + " (解析エラー)";
            return errorEffect;
        }
    }
    
    // 文字列から数値を抽出
    private static int ExtractNumber(string text)
    {
        try
        {
            var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+");
            if (numbers.Count > 0)
            {
                if (int.TryParse(numbers[0].Value, out int result))
                {
                    return result;
                }
            }
            Debug.LogWarning($"数値の抽出に失敗しました: {text}、デフォルト値 1 を使用します");
            return 1; // デフォルト値
        }
        catch (System.Exception e)
        {
            Debug.LogError($"数値の抽出中にエラーが発生しました: {text}, エラー: {e.Message}");
            return 1; // デフォルト値
        }
    }
}

// カードに付与される効果のリスト
[System.Serializable]
public class CardEffects
{
    public List<CardEffectData> effects = new List<CardEffectData>();
    
    public void AddEffect(CardEffectData effect)
    {
        effects.Add(effect);
    }
    
    public List<CardEffectData> GetEffectsForTrigger(GameEventType trigger)
    {
        return effects.FindAll(e => e.trigger == trigger);
    }
}