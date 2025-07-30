using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreenController : MonoBehaviour
{
    [SerializeField] private Button exploreButton;
    [SerializeField] private Button singleCardBattleButton;
    [SerializeField] private Button multiCardBattleButton;
    
    private void Start()
    {
        if (exploreButton != null)
            exploreButton.onClick.AddListener(OnExploreButtonClick);
            
        if (singleCardBattleButton != null)
            singleCardBattleButton.onClick.AddListener(OnSingleCardBattleButtonClick);
            
        if (multiCardBattleButton != null)
            multiCardBattleButton.onClick.AddListener(OnMultiCardBattleButtonClick);
    }
    
    public void OnExploreButtonClick()
    {
        Debug.Log("Explore button clicked!");
        OrientationHelper.SetPortraitOrientation();
        SceneManager.LoadScene("Map");
    }
    
    public void OnSingleCardBattleButtonClick()
    {
        Debug.Log("Single Card Battle button clicked!");
        GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.Single);
        OrientationHelper.SetLandscapeOrientation();
        SceneManager.LoadScene("CardBattleScene");
    }
    
    public void OnMultiCardBattleButtonClick()
    {
        Debug.Log("Multi Card Battle button clicked!");
        GameModeManager.Instance.SetGameMode(GameModeManager.GameMode.Multi);
        OrientationHelper.SetLandscapeOrientation();
        SceneManager.LoadScene("CardBattleScene");
    }
}