using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IPointerClickHandler
{
    // カードデータを表示する
    public CardView view;
    // カードデータを管理する
    public CardModel model { get; private set; }

    public CardMovement movement; //カードの移動を管理するための変数を追加
    
    private bool isEnlarged = false;
    private GameObject enlargedCardCopy; // 拡大表示用のコピー
    private float lastClickTime = 0f; // 最後のクリック時間
    
    private void Awake()
    {
        // CardViewを取得
        view = GetComponent<CardView>();
        movement = GetComponent<CardMovement>();
    }

    public void Init(int cardID, bool playerCard) 
    {
        // CardModelを作成し、データを適用
        model = new CardModel(cardID, playerCard);
        view.Show(model);
        // 初期状態では攻撃不可
        view.SetCanAttackPanel(false);
        // 初期状態では使用不可（後でGameManagerが更新する）
        view.SetCanUsePanel(false);
    }


    private bool isDestroying = false;
    
    public void DestroyCard(CardController card)
    {
        if (isDestroying)
        {
            Debug.Log($"DestroyCard: {card.model.name} は既に破壊処理中です。重複実行を防止。");
            return;
        }
        
        isDestroying = true;
        Debug.Log($"DestroyCard: {card.model.name} の破壊処理を開始");
        
        // 死亡時イベントを発火
        GameEventManager.TriggerDeathEvent(card);
        
        Destroy(card.gameObject);
    }

    public void DropField()
    {
        if (model.isPlayerCard)
        {
            GameManagerCardBattle.instance.ReduceManaPoint(model.cost);
        }
        else
        {
            GameManagerCardBattle.instance.ReduceEnemyManaPoint(model.cost);
        }
        model.canUse = false;
        view.SetCanUsePanel(model.canUse); // 出した時にCanUsePanelを消す
        
        // 登場時イベントを発火
        Debug.Log($"DropField: カード {model.name} の登場時イベントを発火します。効果数: {model.cardEffects.effects.Count}");
        GameEventManager.TriggerSummonEvent(this);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        
        // 敵のカードはクリック不可（特殊効果の対象選択時は除く）
        bool isEffectTargetWaiting = false;
        if (EffectTargetSelector.instance != null)
        {
            try
            {
                isEffectTargetWaiting = EffectTargetSelector.instance.IsWaitingForSelection();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EffectTargetSelector状態取得エラー: {e.Message}");
                isEffectTargetWaiting = false;
            }
        }
        
        // エフェクト選択中の場合、選択可能かどうかをチェック
        if (isEffectTargetWaiting && EffectTargetSelector.instance != null)
        {
            bool isSelectableCard = EffectTargetSelector.instance.IsCardSelectable(this);
            if (!isSelectableCard)
            {
                        return;
            }
            }
        
        lastClickTime = Time.time;
        
        // 特殊効果の対象選択中の場合（プレイヤー・敵カード問わず）
        if (isEffectTargetWaiting)
        {
                try
            {
                EffectTargetSelector.instance.OnCardSelected(this);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EffectTargetSelector選択処理エラー: {e.Message}");
            }
            return;
        }
        
        // 通常のカード拡大/縮小処理
        
        // 拡大コピーの状態を確認して同期
        if (enlargedCardCopy == null && isEnlarged)
        {
            Debug.Log("状態不整合を発見: enlargedCardCopyがnullなのにisEnlargedがtrue。修正します。");
            isEnlarged = false;
        }
        
        if (!isEnlarged)
        {
            try
            {
                EnlargeCard();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"カード拡大エラー: {e.Message}");
                isEnlarged = false;
            }
        }
        else
        {
            try
            {
                ShrinkCard();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"カード縮小エラー: {e.Message}");
                isEnlarged = false;
                enlargedCardCopy = null;
            }
        }
    }
    
    private void EnlargeCard()
    {
        Debug.Log("EnlargeCard開始");
        
        if (enlargedCardCopy != null)
        {
            Debug.LogWarning("既に拡大コピーが存在します。先に削除します。");
            Destroy(enlargedCardCopy);
            enlargedCardCopy = null;
        }
        
        isEnlarged = true;
        
        // カードのコピーを作成
        enlargedCardCopy = Instantiate(gameObject);
        
        // コピーからドラッグ機能などを無効化
        var cardMovement = enlargedCardCopy.GetComponent<CardMovement>();
        var cardController = enlargedCardCopy.GetComponent<CardController>();
        var eventTrigger = enlargedCardCopy.GetComponent<EventTrigger>();
        
        if (cardMovement != null) Destroy(cardMovement);
        if (cardController != null) Destroy(cardController);
        if (eventTrigger != null) Destroy(eventTrigger);
        
        Debug.Log("不要なコンポーネントを削除完了");
        
        // コピーを4倍に拡大
        enlargedCardCopy.transform.localScale = transform.localScale * 4f;
        Debug.Log($"スケール設定完了: {enlargedCardCopy.transform.localScale}");
        
        // 画面中央に配置
        Canvas parentCanvas = transform.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Debug.Log($"親Canvas発見: {parentCanvas.name}");
            // コピーをCanvasの直下に配置
            enlargedCardCopy.transform.SetParent(parentCanvas.transform, false);
            enlargedCardCopy.transform.position = parentCanvas.GetComponent<RectTransform>().position;
            Debug.Log($"Canvas配置完了: {enlargedCardCopy.transform.position}");
        }
        else
        {
            Debug.LogError("親Canvasが見つかりません");
        }
        
        // 最前面に表示するためのCanvas設定
        Canvas copyCanvas = enlargedCardCopy.GetComponent<Canvas>();
        if (copyCanvas != null)
        {
            copyCanvas.sortingOrder = 1000; // 非常に高い値で最前面に
            copyCanvas.overrideSorting = true;
            Debug.Log("既存Canvasの設定完了");
        }
        else
        {
            // Canvasコンポーネントがなければ追加
            copyCanvas = enlargedCardCopy.AddComponent<Canvas>();
            copyCanvas.overrideSorting = true;
            copyCanvas.sortingOrder = 1000;
            enlargedCardCopy.AddComponent<GraphicRaycaster>();
            Debug.Log("新規Canvas追加完了");
        }
        
        // コピーのクリックイベントを設定（縮小用）
        // IPointerClickHandlerを実装するコンポーネントを追加
        var clickHandler = enlargedCardCopy.AddComponent<EnlargedCardClickHandler>();
        clickHandler.originalCard = this;
        Debug.Log("クリックハンドラー設定完了");
        
    }
    
    public void ShrinkCard()
    {
        isEnlarged = false;
        
        // 拡大コピーを削除
        if (enlargedCardCopy != null)
        {
            Destroy(enlargedCardCopy);
            enlargedCardCopy = null;
        }
        
    }
    
}