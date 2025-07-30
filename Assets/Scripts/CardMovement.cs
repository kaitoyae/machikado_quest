using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

public class CardMovement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Transform defaultParent;
    public Canvas canvas; // Inspectorでセット or 自動取得
    private Vector3 originalScale;
    private Vector2 dragOffset;
    bool canDrag = true; // カードを動かせるかどうかのフラグ

    void Awake()
    {
        // Inspectorで未設定なら親階層から自動取得
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("CardMovement: Canvas reference is missing and could not be found in parent hierarchy.", this);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CardController card = GetComponent<CardController>();
        canDrag = true;
 
        // 敵のカードはドラッグ不可
        if (card.model != null && !card.model.isPlayerCard)
        {
            canDrag = false;
        }
        
        // 敵のターン中はプレイヤーのカードもドラッグ不可
        if (GameManagerCardBattle.instance != null && !GameManagerCardBattle.instance.isPlayerTurn)
        {
            canDrag = false;
        }
        
        // 手札のカードの場合のみマナコストチェック
        if (card.model.summonedTurn == -1 && card.model.canUse == false) // 手札かつマナが足りない場合
        {
            canDrag = false;
        }
 
        if (canDrag == false)
        {
            return;
        }

        // カードをドラッグし始めた時の処理
        defaultParent = transform.parent;
        originalScale = transform.localScale;
        // ドラッグ中はCanvas直下に移動
        transform.SetParent(canvas.transform, true); // worldPositionStays = true
        transform.localScale = originalScale; // スケール維持
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        // オフセット計算
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        dragOffset = GetComponent<RectTransform>().anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData) //ドラッグした時に起こす処理
    {
        if (canDrag == false)
        {
            return;
        }

        if (canvas == null) return; // 参照がなければ何もしない
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            GetComponent<RectTransform>().anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData) //カードを話したときに起こす処理

    {
        if (canDrag == false)
        {
            return;
        }
        // 元の親に戻す
        transform.SetParent(defaultParent, true); // worldPositionStays = true
        transform.localScale = originalScale;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }


    // 手札からフィールドにカードの位置を変更する
    public void SetCardTransform(Transform parentTransform) 
    {
        CardController cardController = GetComponent<CardController>();
        bool wasOnField = (cardController != null && cardController.model != null && cardController.model.summonedTurn >= 0);
        
        defaultParent = parentTransform;  // 新しい親（フィールド）を設定
        transform.SetParent(defaultParent, false);  // 親オブジェクトを変更
        
        // 出したターンを記録
        if (cardController != null && cardController.model != null)
        {
            // 手札からフィールドに出した場合のみ召喚ターンを記録し、canAttackをfalseに設定
            if (!wasOnField)
            {
                cardController.model.summonedTurn = GameManagerCardBattle.instance.GetCurrentTurn();
                cardController.model.canAttack = false;
                Debug.Log($"カード{cardController.model.name}をターン{cardController.model.summonedTurn}に召喚、canAttack=falseに設定");
            }
            else
            {
                Debug.Log($"カード{cardController.model.name}をフィールド内で移動、canAttackは変更しない");
            }
        }
    }

    // 手札からフィールドにカードをアニメーションで移動する
    public IEnumerator MoveToFieldWithAnimation(Transform targetFieldTransform)
    {
        CardController cardController = GetComponent<CardController>();
        bool wasOnField = (cardController != null && cardController.model != null && cardController.model.summonedTurn >= 0);
        
        // 現在の位置を記録
        Vector3 startPosition = transform.position;
        
        // 新しい親を設定（アニメーション完了後に適用するため、まだ変更しない）
        defaultParent = targetFieldTransform;
        
        // 一時的に親をCanvasに変更（アニメーション中の階層問題を回避）
        Transform originalParent = transform.parent;
        transform.SetParent(canvas.transform, true);
        
        // ターゲット位置を計算
        Transform tempTransform = new GameObject("TempTransform").transform;
        tempTransform.SetParent(targetFieldTransform, false);
        tempTransform.localPosition = Vector3.zero;
        Vector3 targetPosition = tempTransform.position;
        Destroy(tempTransform.gameObject);
        
        // アニメーション実行（0.5秒かけて移動）
        float duration = 0.5f;
        yield return transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad).WaitForCompletion();
        
        // 親を最終的な位置に設定
        transform.SetParent(defaultParent, true);
        transform.localPosition = Vector3.zero;
        
        // 出したターンを記録
        if (cardController != null && cardController.model != null)
        {
            // 手札からフィールドに出した場合のみ召喚ターンを記録し、canAttackをfalseに設定
            if (!wasOnField)
            {
                cardController.model.summonedTurn = GameManagerCardBattle.instance.GetCurrentTurn();
                cardController.model.canAttack = false;
                Debug.Log($"カード{cardController.model.name}をターン{cardController.model.summonedTurn}に召喚、canAttack=falseに設定");
            }
            else
            {
                Debug.Log($"カード{cardController.model.name}をフィールド内で移動、canAttackは変更しない");
            }
        }
    }

    public IEnumerator AttackMotion(Transform target)
    {
        // null チェックを追加
        if (target == null || transform == null)
        {
            Debug.LogError("AttackMotion: target or transform is null");
            yield break;
        }

        Vector3 currentPosition = transform.position;
        Transform cardParent = transform.parent;
        
        // 親がnullでないことを確認
        if (cardParent == null || cardParent.parent == null)
        {
            Debug.LogError("AttackMotion: cardParent or cardParent.parent is null");
            yield break;
        }
 
        transform.SetParent(cardParent.parent); // cardの親を一時的にCanvasにする
 
        // DOTweenが完了する前にオブジェクトが破棄されないようにする
        Tween moveTween1 = transform.DOMove(target.position, 0.25f);
        yield return moveTween1.WaitForCompletion();
        
        // オブジェクトが破棄されていないか確認
        if (transform == null)
        {
            yield break;
        }
        
        Tween moveTween2 = transform.DOMove(currentPosition, 0.25f);
        yield return moveTween2.WaitForCompletion();
        
        // オブジェクトが破棄されていないか確認
        if (transform == null || cardParent == null)
        {
            yield break;
        }
 
        transform.SetParent(cardParent); // cardの親を元に戻す
    }
}