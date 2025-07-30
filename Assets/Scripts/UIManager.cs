
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
 
public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject changeTurnPanel;
    [SerializeField] TextMeshProUGUI changeTurnText;
    [SerializeField] GameObject turnEndButton; // ターンエンドボタン
    [SerializeField] GameObject gameEndPanel; // ゲーム終了パネル
    [SerializeField] TextMeshProUGUI gameEndText; // ゲーム終了テキスト
    [SerializeField] GameObject effectPanel; // ユニット効果表示パネル
    [SerializeField] TextMeshProUGUI effectText; // ユニット効果テキスト
    [SerializeField] GameObject targetSelectPanel; // 対象選択中表示パネル
    [SerializeField] TextMeshProUGUI targetSelectText; // 対象選択中テキスト

    void Start()
    {
        // ゲーム開始時は表示したままにする
        // changeTurnPanel.SetActive(false);
        
        // ターンエンドボタンは最初は非表示
        if (turnEndButton != null)
        {
            turnEndButton.SetActive(false);
        }
        
        // ゲーム終了パネルは最初は非表示
        if (gameEndPanel != null)
        {
            gameEndPanel.SetActive(false);
        }
        
        // 効果パネルは最初は非表示
        if (effectPanel != null)
        {
            effectPanel.SetActive(false);
        }
        
        // 対象選択パネルは最初は非表示
        if (targetSelectPanel != null)
        {
            targetSelectPanel.SetActive(false);
        }
    }
 
    public IEnumerator ShowChangeTurnPanel(bool isPlayerTurn)
    {
        
        if (changeTurnPanel == null)
        {
            Debug.LogError("changeTurnPanel is null! Please set it in the Inspector.");
            yield break;
        }
        
        changeTurnPanel.SetActive(true);

        if (changeTurnText == null)
        {
            Debug.LogError("changeTurnText is null! Please set it in the Inspector.");
        }
        else
        {
            if (isPlayerTurn == true)
            {
                changeTurnText.text = "Your Turn";
            }
            else
            {
                changeTurnText.text = "Enemy Turn";
            }
        }
 
        yield return new WaitForSecondsRealtime(2f);
        
        changeTurnPanel.SetActive(false);
    }
    
    // ターンエンドボタンの表示/非表示を制御
    public void ShowTurnEndButton(bool show)
    {
        if (turnEndButton != null)
        {
            turnEndButton.SetActive(show);
        }
    }
    
    // ターンエンドボタンがクリックされたときの処理
    public void OnClickTurnEnd()
    {
        // プレイヤーターンの時のみターンを終了できる
        if (GameManagerCardBattle.instance != null && GameManagerCardBattle.instance.isPlayerTurn)
        {
            // ボタンを非表示にして、ターンを終了
            ShowTurnEndButton(false);
            GameManagerCardBattle.instance.ChangeTurn();
        }
    }
    
    // ゲーム終了パネルを表示（ChangeTurnPanelと同じ仕組み）
    public IEnumerator ShowGameEndPanel(bool playerWon)
    {
        Debug.Log("ShowGameEndPanel開始");
        
        if (gameEndPanel == null)
        {
            Debug.LogError("gameEndPanel is null! Please set it in the Inspector.");
            yield break;
        }
        
        gameEndPanel.SetActive(true);

        if (gameEndText == null)
        {
            Debug.LogError("gameEndText is null! Please set it in the Inspector.");
        }
        else
        {
            if (playerWon)
            {
                gameEndText.text = "You Win!";
            }
            else
            {
                gameEndText.text = "You Lose!";
            }
        }
 
        Debug.Log($"2秒待機開始");
        yield return new WaitForSecondsRealtime(2f);
        
        Debug.Log("HomeScreenシーンに遷移します");
        // HomeScreenシーンに遷移
        UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScreen");
    }
    
    // ユニット効果パネルを表示
    public IEnumerator ShowEffectPanel(string effectName)
    {
        Debug.Log($"ShowEffectPanel開始: '{effectName}' (文字数: {effectName?.Length})");
        
        if (effectPanel == null)
        {
            Debug.LogError("effectPanel is null! Please set it in the Inspector.");
            yield break;
        }
        
        effectPanel.SetActive(true);
        Debug.Log("effectPanel.SetActive(true) 完了");

        if (effectText == null)
        {
            Debug.LogError("effectText is null! Please set it in the Inspector.");
        }
        else
        {
            Debug.Log($"effectTextにテキストを設定中: '{effectName}'");
            effectText.text = effectName;
            Debug.Log($"effectText.text設定完了: '{effectText.text}'");
            Debug.Log($"effectTextの状態: enabled={effectText.enabled}, gameObject.activeInHierarchy={effectText.gameObject.activeInHierarchy}");
        }
 
        Debug.Log("1.5秒待機開始");
        yield return new WaitForSecondsRealtime(1.5f);
        
        Debug.Log("効果パネルを非表示にします");
        effectPanel.SetActive(false);
        Debug.Log("ShowEffectPanel終了");
    }
    
    // 対象選択パネルの表示/非表示を制御
    public void ShowTargetSelectPanel(bool show, string message = "対象を選択してください")
    {
        Debug.Log($"ShowTargetSelectPanel: {show}, message: {message}");
        
        if (targetSelectPanel != null)
        {
            targetSelectPanel.SetActive(show);
            Debug.Log($"targetSelectPanel設定完了: {show}, activeInHierarchy={targetSelectPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("targetSelectPanelがnullです! Inspectorで設定してください。");
        }
        
        if (targetSelectText != null)
        {
            if (show)
            {
                targetSelectText.text = message;
                Debug.Log($"targetSelectText設定完了: '{message}', enabled={targetSelectText.enabled}");
            }
        }
        else
        {
            Debug.LogError("targetSelectTextがnullです! Inspectorで設定してください。");
        }
    }
}
