using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 特殊効果の対象選択を管理するシステム
public class EffectTargetSelector : MonoBehaviour
{
    public static EffectTargetSelector instance;
    
    private bool isWaitingForSelection = false;
    private List<CardController> selectableCards = new List<CardController>();
    private Action<CardController> onTargetSelected;
    private CardEffectData currentEffect;
    private GameEventData currentEventData;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 初期化
            isWaitingForSelection = false;
        }
    }
    
    // 対象選択を開始
    public void StartTargetSelection(EffectTarget targetType, CardEffectData effect, GameEventData eventData, Action<CardController> callback)
    {
        Debug.Log($"対象選択開始: {targetType}");
        Debug.Log($"StartTargetSelection: callback={callback != null}, effect={effect?.description}");
        
        isWaitingForSelection = true;
        currentEffect = effect;
        currentEventData = eventData;
        onTargetSelected = callback;
        
        Debug.Log($"StartTargetSelection: onTargetSelected設定完了 = {onTargetSelected != null}");
        
        // 選択可能なカードを取得
        selectableCards = GetSelectableCards(targetType, eventData);
        
        Debug.Log($"選択可能カード数: {selectableCards.Count}");
        
        // 選択可能なカードがない場合は効果をキャンセル
        if (selectableCards.Count == 0)
        {
            Debug.Log("選択可能なカードがありません。効果をキャンセルします。");
            EndTargetSelection();
            return;
        }
        
        // 敵のターンかどうかチェック
        bool isEnemyTurn = GameManagerCardBattle.instance != null && !GameManagerCardBattle.instance.isPlayerTurn;
        
        if (isEnemyTurn)
        {
            Debug.Log("敵のターンのため、自動選択を開始します");
            StartCoroutine(AutoSelectForEnemy());
        }
        else
        {
            Debug.Log("プレイヤーのターンのため、手動選択を開始します");
            // 選択可能カードに青い縁を表示
            ShowSelectableCards(true);
        }
    }
    
    // カードが選択された時の処理
    public void OnCardSelected(CardController selectedCard)
    {
        if (!isWaitingForSelection) return;
        
        // 選択可能なカードかチェック
        if (!selectableCards.Contains(selectedCard))
        {
            Debug.Log("選択できないカードです");
            return;
        }
        
        Debug.Log($"カードが選択されました: {selectedCard.model.name}");
        
        // UI状態のみ先に更新（コールバックは保持）
        ShowSelectableCards(false);
        
        // エフェクト表示
        var uiManager = GameManagerCardBattle.instance?.UIManager;
        if (uiManager != null && currentEffect != null)
        {
            StartCoroutine(ShowEffectAndExecute(selectedCard, uiManager));
        }
        else
        {
            // UIManagerまたは効果がない場合は直接実行
            ExecuteEffect(selectedCard);
        }
    }
    
    // 効果表示後に実行するコルーチン
    private IEnumerator ShowEffectAndExecute(CardController target, UIManager uiManager)
    {
        // 効果名を表示
        if (currentEffect != null)
        {
            yield return StartCoroutine(uiManager.ShowEffectPanel($"{currentEffect.description}発動!"));
        }
        
        // 効果を実行
        ExecuteEffect(target);
    }
    
    // 実際の効果実行
    private void ExecuteEffect(CardController target)
    {
        try
        {
            Debug.Log($"ExecuteEffect開始: target={target.model.name}, onTargetSelected={onTargetSelected != null}");
            
            // コールバックを実行（効果発動）
            if (onTargetSelected != null)
            {
                Debug.Log("EffectTargetSelector: コールバック実行中...");
                onTargetSelected.Invoke(target);
                Debug.Log("EffectTargetSelector: コールバック実行完了");
            }
            else
            {
                Debug.LogError("EffectTargetSelector: onTargetSelectedがnullです！");
            }
            
            Debug.Log($"効果発動完了: {currentEffect?.description} -> {target.model.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"効果実行エラー: {e.Message}");
        }
        finally
        {
            // 効果発動後に選択状態を完全に終了
            EndTargetSelection();
            
            // 効果発動後にゲーム進行を続ける
            ResumeGameFlow();
        }
    }
    
    // ゲーム進行を再開
    private void ResumeGameFlow()
    {
        Debug.Log("ゲーム進行を再開");
        
        // 必要に応じて、ここでゲームマネージャーに通知
        // 例：次のカードの効果処理、ターン進行など
        if (GameManagerCardBattle.instance != null)
        {
            // 手札やフィールドの状態を更新
            GameManagerCardBattle.instance.SetCanUsePanelHand();
            GameManagerCardBattle.instance.SetCanUsePanelEnemyHand();
        }
    }
    
    // 対象選択を終了
    private void EndTargetSelection()
    {
        if (!isWaitingForSelection) return; // 既に終了している場合は何もしない
        
        Debug.Log("EndTargetSelection: 選択状態を終了します");
        isWaitingForSelection = false;
        ShowSelectableCards(false);
        selectableCards.Clear();
        onTargetSelected = null;
        currentEffect = null;
        currentEventData = null;
    }
    
    // 緑色の縁の保存用（選択中は一時的にオフにするため）
    private List<CardController> cardsWithGreenBorder = new List<CardController>();
    
    // 選択可能なカードを取得
    private List<CardController> GetSelectableCards(EffectTarget targetType, GameEventData eventData)
    {
        var cards = new List<CardController>();
        var gameManager = GameManagerCardBattle.instance;
        
        switch (targetType)
        {
            case EffectTarget.SelfUnit:
                // 自ユニット（自分のフィールドのカード）
                if (eventData.isPlayerEvent)
                    cards.AddRange(gameManager.PlayerFieldTransform.GetComponentsInChildren<CardController>());
                else
                    cards.AddRange(gameManager.EnemyFieldTransform.GetComponentsInChildren<CardController>());
                break;
                
            case EffectTarget.EnemyUnit:
                // 敵ユニット（相手のフィールドのカード）
                if (eventData.isPlayerEvent)
                    cards.AddRange(gameManager.EnemyFieldTransform.GetComponentsInChildren<CardController>());
                else
                    cards.AddRange(gameManager.PlayerFieldTransform.GetComponentsInChildren<CardController>());
                break;
        }
        
        return cards;
    }
    
    // 選択可能カードの表示処理
    private void ShowSelectableCards(bool show)
    {
        Debug.Log($"ShowSelectableCards: {show}, 対象カード数: {selectableCards.Count}");
        
        if (show)
        {
            // 現在緑色の縁が表示されているカードを保存
            SaveCurrentGreenBorders();
            
            // 全ての緑色の縁を一時的にオフ
            HideAllGreenBorders();
            
            // 選択可能カードに青色の縁を表示
            foreach (CardController card in selectableCards)
            {
                if (card != null && card.view != null)
                {
                    card.view.SetCanEffectPanel(true);
                    Debug.Log($"青い縁を表示: {card.model.name}");
                }
            }
            
            // UIManager経由で対象選択パネルを表示
            var uiManager = GameManagerCardBattle.instance?.UIManager;
            if (uiManager != null)
            {
                uiManager.ShowTargetSelectPanel(true, "対象を選択してください");
            }
        }
        else
        {
            // 青色の縁を非表示
            foreach (CardController card in selectableCards)
            {
                if (card != null && card.view != null)
                {
                    card.view.SetCanEffectPanel(false);
                    Debug.Log($"青い縁を非表示: {card.model.name}");
                }
            }
            
            // 緑色の縁を復元
            RestoreGreenBorders();
            
            // 対象選択パネルを非表示
            var uiManager = GameManagerCardBattle.instance?.UIManager;
            if (uiManager != null)
            {
                uiManager.ShowTargetSelectPanel(false);
            }
        }
    }
    
    // 現在緑色の縁が表示されているカードを保存
    private void SaveCurrentGreenBorders()
    {
        cardsWithGreenBorder.Clear();
        var gameManager = GameManagerCardBattle.instance;
        
        if (gameManager != null)
        {
            // プレイヤーフィールドのカードをチェック
            var playerCards = gameManager.PlayerFieldTransform.GetComponentsInChildren<CardController>();
            foreach (var card in playerCards)
            {
                if (card != null && card.model != null && card.model.canAttack)
                {
                    cardsWithGreenBorder.Add(card);
                }
            }
            
            // 敵フィールドのカードをチェック
            var enemyCards = gameManager.EnemyFieldTransform.GetComponentsInChildren<CardController>();
            foreach (var card in enemyCards)
            {
                if (card != null && card.model != null && card.model.canAttack)
                {
                    cardsWithGreenBorder.Add(card);
                }
            }
        }
        
        Debug.Log($"緑色の縁のカードを保存: {cardsWithGreenBorder.Count}枚");
    }
    
    // 全ての緑色の縁を一時的にオフ
    private void HideAllGreenBorders()
    {
        foreach (var card in cardsWithGreenBorder)
        {
            if (card != null && card.view != null)
            {
                card.view.SetCanAttackPanel(false);
                Debug.Log($"緑色の縁を一時的にオフ: {card.model.name}");
            }
        }
    }
    
    // 緑色の縁を復元
    private void RestoreGreenBorders()
    {
        foreach (var card in cardsWithGreenBorder)
        {
            if (card != null && card.view != null && card.model != null && card.model.canAttack)
            {
                card.view.SetCanAttackPanel(true);
                Debug.Log($"緑色の縁を復元: {card.model.name}");
            }
        }
        cardsWithGreenBorder.Clear();
    }
    
    // 現在選択待ちかどうか
    public bool IsWaitingForSelection()
    {
        return isWaitingForSelection;
    }
    
    // 指定したカードが選択可能かどうか
    public bool IsCardSelectable(CardController card)
    {
        if (!isWaitingForSelection || card == null)
        {
            return false;
        }
        
        bool isSelectable = selectableCards.Contains(card);
        Debug.Log($"IsCardSelectable: {card.model?.name} -> {isSelectable}");
        return isSelectable;
    }
    
    // 敵の自動選択コルーチン
    private IEnumerator AutoSelectForEnemy()
    {
        Debug.Log("敵の自動選択開始: 0.5秒待機");
        
        // 0.5秒待機
        yield return new WaitForSeconds(0.5f);
        
        // 選択可能なカードがあるかチェック
        if (selectableCards.Count > 0)
        {
            // ランダムに選択
            int randomIndex = UnityEngine.Random.Range(0, selectableCards.Count);
            CardController selectedCard = selectableCards[randomIndex];
            
            Debug.Log($"敵が自動選択: {selectedCard.model.name} (選択肢{selectableCards.Count}の中から{randomIndex + 1}番目)");
            
            // 選択処理を実行
            OnCardSelected(selectedCard);
        }
        else
        {
            Debug.Log("敵の自動選択: 選択可能なカードがありません。効果をキャンセルします。");
            // 選択状態を終了
            EndTargetSelection();
        }
    }
    
    // 強制的に選択状態をリセットする（デバッグ用）
    public void ForceReset()
    {
        Debug.Log("EffectTargetSelector: 強制リセット実行");
        isWaitingForSelection = false;
        ShowSelectableCards(false);
        selectableCards.Clear();
        cardsWithGreenBorder.Clear();
        onTargetSelected = null;
        currentEffect = null;
        currentEventData = null;
    }
}