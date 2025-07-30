using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
 
public class AttackedLeader : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("AttackedLeader.OnDrop called");
        
        /// 攻撃
        // attackerを選択　マウスポインターに重なったカードをアタッカーにする
        CardController attackCard = eventData.pointerDrag.GetComponent<CardController>();
        
        if (attackCard == null) 
        {
            Debug.Log("攻撃カードが見つかりません");
            return;
        }
        
        Debug.Log($"Leader Attack: {attackCard.model.name} → Leader");
        
        // マルチプレイの場合のみターン制限をチェック
        bool isMultiplayMode = (GameModeManager.Instance != null && 
                               GameModeManager.Instance.CurrentGameMode == GameModeManager.GameMode.Multi);
        
        if (isMultiplayMode && !GameManagerCardBattle.instance.isPlayerTurn)
        {
            Debug.Log("相手のターン中はリーダー攻撃できません");
            return;
        }
        
        // 敵のカードではプレイヤーがリーダー攻撃できない
        if (!attackCard.model.isPlayerCard)
        {
            Debug.Log("敵のカードでリーダー攻撃はできません");
            return;
        }
        
        // 手札のカードでは攻撃できない
        if (attackCard.model.summonedTurn == -1)
        {
            Debug.Log("手札のカードでリーダー攻撃はできません");
            return;
        }
        
        // canAttackチェック
        if (!attackCard.model.canAttack)
        {
            Debug.Log($"{attackCard.model.name} は攻撃できません (canAttack=false)");
            return;
        }
        
        Debug.Log("リーダー攻撃実行");
        GameManagerCardBattle.instance.AttackToLeader(attackCard, true);
    }
}