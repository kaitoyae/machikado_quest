using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        Single,
        Multi
    }
    
    public static GameModeManager Instance { get; private set; }
    public GameMode CurrentGameMode { get; private set; } = GameMode.Single;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void SetGameMode(GameMode mode)
    {
        CurrentGameMode = mode;
        Debug.Log($"Game mode set to: {mode}");
    }
}