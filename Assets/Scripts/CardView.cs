using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text atText;
    [SerializeField] TMP_Text cosText;
    [SerializeField] Image iconImage;
    [SerializeField] GameObject canAttackPanel;
    [SerializeField] GameObject canUsePanel;
    [SerializeField] GameObject canEffectPanel;
    [SerializeField] TMP_Text effectText;
    
    void Awake()
    {
        // 初期状態では全てのパネルを非表示
    }
    
    void Start()
    {
        // 初期状態では全てのパネルを非表示
        if (canAttackPanel != null) canAttackPanel.SetActive(false);
        if (canUsePanel != null) canUsePanel.SetActive(false);
        if (canEffectPanel != null) canEffectPanel.SetActive(false);
    }

    public void Show(CardModel cardModel)
    {
        nameText.text = cardModel.name;
        hpText.text = cardModel.hp.ToString();
        atText.text = cardModel.at.ToString();
        cosText.text = cardModel.cost.ToString();
        iconImage.sprite = cardModel.icon;
        
        // 特殊効果テキストを表示
        if (effectText != null)
        {
            if (cardModel.cardEffects != null && cardModel.cardEffects.effects.Count > 0)
            {
                effectText.text = cardModel.cardEffects.effects[0].description;
            }
            else
            {
                effectText.text = "※効果なし";
            }
        }
        
    }

    public void SetCanAttackPanel(bool flag)
    {
        if (canAttackPanel != null)
        {
            canAttackPanel.SetActive(flag);
            if (flag)
            {

            }
        }
    }
        public void SetCanUsePanel(bool flag) // フラグに合わせてCanUsePanelを付けるor消す
    {
        canUsePanel.SetActive(flag);
    }
    
    public void SetCanEffectPanel(bool flag) // 特殊効果の対象選択時に青色の縁を表示
    {
        Debug.Log($"SetCanEffectPanel呼び出し: flag={flag}, カード名={nameText?.text}");
        
        if (canEffectPanel != null)
        {
            canEffectPanel.SetActive(flag);
            Debug.Log($"CanEffectPanel設定完了: {flag} for {nameText?.text}, activeInHierarchy={canEffectPanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError($"canEffectPanelがnullです! カード名={nameText?.text}。Inspectorで設定してください。");
        }
    }
    
    
}