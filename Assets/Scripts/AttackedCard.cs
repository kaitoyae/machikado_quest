using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
 
// 攻撃される側のコード
public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("AttackedCard.OnDrop called");
        
        /// 攻撃
        // attackerを選択　マウスポインターに重なったカードをアタッカーにする
        CardController attackCard = eventData.pointerDrag.GetComponent<CardController>();
 
        // defenderを選択　
        CardController defenceCard = GetComponent<CardController>();
 
        Debug.Log($"Attack: {attackCard?.model?.name}(Player:{attackCard?.model?.isPlayerCard}) → {defenceCard?.model?.name}(Player:{defenceCard?.model?.isPlayerCard})");

        // 手札にあるカードへの攻撃を事前にブロック
        if (defenceCard.model.summonedTurn == -1)
        {
            Debug.Log($"Cannot attack hand card: {defenceCard.model.name} (summonedTurn=-1)");
            return;
        }

        // 味方同士の場合はバトルしない
        if (attackCard.model.isPlayerCard == defenceCard.model.isPlayerCard)
        {
            Debug.Log("Same team - no battle");
            return;
        }

        // canAttackチェック
        if (!attackCard.model.canAttack)
        {
            Debug.Log($"{attackCard.model.name} cannot attack (canAttack=false)");
            return;
        }

        Debug.Log("Starting CardBattle");
        // バトルする
        GameManagerCardBattle.instance.CardBattle(attackCard, defenceCard);
 
    }
}