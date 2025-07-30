using UnityEngine;
using UnityEngine.EventSystems;

// 拡大カードのクリック処理を担当するクラス
public class EnlargedCardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public CardController originalCard;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (originalCard != null)
        {
            originalCard.ShrinkCard();
        }
    }
}