using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

public class DropPlace : MonoBehaviourPun, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"OnDrop開始: ドロップイベント発生");
        
        // マルチプレイの場合のみターン制限をチェック
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (isMultiplayMode && !GameManagerCardBattle.instance.isPlayerTurn)
        {
            Debug.Log("相手のターン中はカード操作できません");
            return;
        }
        
        // カードがドロップされた時に親を変更する
        CardController card = eventData.pointerDrag.GetComponent<CardController>();
        if (card != null) 
        {
            Debug.Log($"OnDrop: カード {card.model.name} がドロップされました");
            
            // 手札からフィールドに出す場合かどうかを判定（summonedTurnで判定）
            bool isFromHand = (card.model.summonedTurn == -1);
            // プレイヤーフィールドへのドロップかどうかを判定
            bool isToPlayerField = (this.transform == GameManagerCardBattle.instance.PlayerFieldTransform);
            // 敵フィールドへのドロップかどうかを判定
            bool isToEnemyField = (this.transform == GameManagerCardBattle.instance.EnemyFieldTransform);
            // プレイヤー手札へのドロップかどうかを判定
            bool isToPlayerHand = (this.transform == GameManagerCardBattle.instance.PlayerHandTransform);
            // 敵手札へのドロップかどうかを判定
            bool isToEnemyHand = (this.transform == GameManagerCardBattle.instance.EnemyHandTransform);
            
            Debug.Log($"OnDrop判定: isFromHand={isFromHand}, isToPlayerField={isToPlayerField}, isToEnemyField={isToEnemyField}, isToPlayerHand={isToPlayerHand}, isToEnemyHand={isToEnemyHand}");
            
            // プレイヤーカードかどうかを判定
            bool isPlayerCard = card.model.isPlayerCard;
            
            // === 移動制限チェック ===
            
            // 1. 敵の手札への移動を禁止
            if (isToEnemyHand)
            {
                Debug.Log($"敵の手札への移動は禁止されています - {card.model.name}");
                return;
            }
            
            // 2. 敵のカードをプレイヤー手札に移動することを禁止
            if (!isPlayerCard && isToPlayerHand)
            {
                Debug.Log($"敵のカードをプレイヤー手札に移動することは禁止されています - {card.model.name}");
                return;
            }
            
            // 3. フィールドから手札への移動を禁止
            if (!isFromHand && (isToPlayerHand || isToEnemyHand))
            {
                Debug.Log($"フィールドから手札への移動は禁止されています - {card.model.name}");
                return;
            }
            
            // 4. 手札内でのドロップを禁止（意図しない操作を防ぐ）
            if (isFromHand && (isToPlayerHand || isToEnemyHand))
            {
                Debug.Log($"手札内でのドロップは無効です - {card.model.name}");
                return;
            }
            
            // 5. プレイヤーのカードのみプレイヤーフィールドに出せる
            if (isFromHand && isToPlayerField && !isPlayerCard)
            {
                Debug.Log($"敵のカードをプレイヤーフィールドに出すことはできません - {card.model.name}");
                return;
            }
            
            // 6. 敵フィールドへの直接配置を禁止（攻撃のみ許可）
            if (isToEnemyField)
            {
                Debug.Log($"敵フィールドへの直接配置は禁止されています。攻撃のみ可能です - {card.model.name}");
                return;
            }
            
            // 手札からプレイヤーフィールドに出す場合のみマナコストチェック
            if (isFromHand && isToPlayerField && card.model.canUse == false)
            {
                Debug.Log($"マナ不足のためカードを出せません - {card.model.name}");
                return;
            }

            // 手札からプレイヤーフィールドに出す場合のみコストを引く
            if (isFromHand && isToPlayerField)
            {
                Debug.Log($"手札からフィールド: DropField()呼び出し - {card.model.name}");
                card.DropField(); // マナコストを引く
                
                // マルチプレイの場合のみ召喚情報を同期
                if (isMultiplayMode)
                {
                    bool isPlayerField = (this.transform == GameManagerCardBattle.instance.PlayerFieldTransform);
                    Debug.Log($"RPC送信準備: SummonCard(ID:{card.model.id}, isPlayerField:{isPlayerField}) → {this.transform.name}");
                    
                    // ネットワーク接続確認
                    if (photonView != null && PhotonNetwork.IsConnected)
                    {
                        // 召喚したカードの情報を相手に送信
                        photonView.RPC("SummonCard", RpcTarget.Others, card.model.id, isPlayerField);
                        Debug.Log($"✅ RPC送信成功: {card.model.name}");
                    }
                    else
                    {
                        Debug.LogError($"❌ RPC送信失敗: PhotonView={photonView}, Connected={PhotonNetwork.IsConnected}");
                    }
                }
                else
                {
                    Debug.Log("シングルプレイモード: RPC送信なし");
                }
            }
            else
            {
                Debug.Log($"マナコスト処理スキップ: isFromHand={isFromHand}, isToPlayerField={isToPlayerField} - {card.model.name}");
            }
            
            // SetCardTransformを使用して召喚ターンを記録（必ず最後に実行）
            card.movement.SetCardTransform(this.transform);
        }
        else
        {
            Debug.LogWarning("OnDrop: ドロップされたオブジェクトにCardControllerが見つかりません");
        }
    }
    
    [PunRPC]
    void SummonCard(int cardID, bool isPlayerField)
    {
        Debug.Log($"召喚同期受信: カードID {cardID}, プレイヤーフィールド: {isPlayerField}");
        
        // 相手側のフィールドに召喚
        Transform targetField = isPlayerField ? GameManagerCardBattle.instance.EnemyFieldTransform 
                                              : GameManagerCardBattle.instance.PlayerFieldTransform;
        
        // 移動元の手札を特定（視点逆転を考慮）
        Transform sourceHand = isPlayerField ? GameManagerCardBattle.instance.EnemyHandTransform 
                                             : GameManagerCardBattle.instance.PlayerHandTransform;
        
        if (GameManagerCardBattle.instance != null)
        {
            // 1. まず手札から該当カードを削除
            CardController handCard = FindCardInHand(sourceHand, cardID);
            if (handCard != null)
            {
                Debug.Log($"手札カード削除: {handCard.model.name} from {sourceHand.name}");
                Destroy(handCard.gameObject);
            }
            else
            {
                Debug.LogWarning($"手札カードが見つかりません: ID {cardID} in {sourceHand.name}");
            }
            
            // 2. フィールドに新しいカードを生成
            GameManagerCardBattle.instance.CreateCard(targetField, cardID);
            
            // 3. 生成されたカードを取得してsummonedTurnを設定
            CardController[] fieldCards = targetField.GetComponentsInChildren<CardController>();
            if (fieldCards.Length > 0)
            {
                CardController newCard = fieldCards[fieldCards.Length - 1]; // 最後に生成されたカード
                newCard.model.summonedTurn = GameManagerCardBattle.instance.GetCurrentTurn();
                newCard.model.canAttack = false;
                Debug.Log($"RPC召喚カード設定: {newCard.model.name}, ターン{newCard.model.summonedTurn}, フィールド{targetField.name}");
            }
        }
    }
    
    // 手札から指定IDのカードを検索するヘルパーメソッド
    CardController FindCardInHand(Transform handTransform, int cardID)
    {
        CardController[] handCards = handTransform.GetComponentsInChildren<CardController>();
        
        Debug.Log($"FindCardInHand: {handTransform.name}で{handCards.Length}枚からID{cardID}を検索");
        
        foreach (CardController card in handCards)
        {
            Debug.Log($"手札カード: {card.model.name}, ID {card.model.id}");
            if (card.model.id == cardID)
            {
                Debug.Log($"手札カード発見: {card.model.name}");
                return card;
            }
        }
        
        Debug.LogWarning($"手札カード未発見: ID {cardID} in {handTransform.name}");
        return null;
    }
}