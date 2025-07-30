using UnityEngine;

// カードのデータを管理するクラス
public class CardModel
{
    public int id;        // カードID
    public string name;   // カード名
    public int hp;        // HP（体力）
    public int at;        // AT（攻撃力）
    public int cost;      // コスト
    public Sprite icon;   // 画像（アイコン）
    public CardEffects cardEffects; // カード効果

    public bool canUse = false;

    public bool canAttack = false;  //「いま攻撃を出来る状態なのか」という情報を管理 bool型
    public int summonedTurn; // 場に出されたターン数
    public bool isPlayerCard = false; // プレイヤーのカードかどうか

    // コンストラクタ（カードIDとプレイヤーカードかどうかを引数にしてデータを読み込む）
    public CardModel(int cardID, bool playerCard) 
    {
        // カードIDを保存
        id = cardID;
        
        // Resourcesフォルダからカードデータを取得
        CardEntity cardEntity = Resources.Load<CardEntity>("CardEntityList/Card" + cardID);
        
        // 取得したデータをCardModelに反映
        name = cardEntity.name;
        hp = cardEntity.hp;
        at = cardEntity.at;
        cost = cardEntity.cost;
        icon = cardEntity.icon;
        summonedTurn = -1; // まだ場に出されていない
        isPlayerCard = playerCard; // プレイヤーのカードかどうかを設定
        
        // 効果データの解析
        cardEffects = new CardEffects();
        
        // 複数効果に対応
        if (cardEntity.multipleEffects != null && cardEntity.multipleEffects.Count > 0)
        {
            // multipleEffectsがある場合はそれを使用
            foreach (string effectText in cardEntity.multipleEffects)
            {
                if (!string.IsNullOrEmpty(effectText) && effectText != "※効果なし")
                {
                    try
                    {
                        var effect = CardEffectData.ParseFromString(effectText);
                        cardEffects.AddEffect(effect);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"効果の解析に失敗しました: {effectText}, エラー: {e.Message}");
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(cardEntity.effectText) && cardEntity.effectText != "※効果なし" && cardEntity.effectText != "効果なし")
        {
            // 従来のeffectTextを使用（後方互換性）
            try
            {
                var effect = CardEffectData.ParseFromString(cardEntity.effectText);
                cardEffects.AddEffect(effect);
            }
            catch (System.Exception e)
            {
                // 効果解析失敗時は静かに処理を継続
            }
        }
        // 効果なしの場合は何もしない
        
        // 検証済みフラグのチェック（警告なしで効果は発動）
        
        // 効果数とリストの確認（デバッグ用ログ削除）
    }
}
