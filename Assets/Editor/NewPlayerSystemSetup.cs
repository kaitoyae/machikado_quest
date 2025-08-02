using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// 新しいPlayer Physics & Movement Systemセットアップツール
/// 既存PlayerオブジェクトやPrefabに新コンポーネントを追加し、
/// 古いThirdPersonControllerを無効化する
/// </summary>
public class NewPlayerSystemSetup : EditorWindow
{
    [MenuItem("Stravent/Setup New Player System")]
    public static void ShowWindow()
    {
        GetWindow<NewPlayerSystemSetup>("New Player System Setup");
    }

    private GameObject targetPlayer;
    private bool setupCompleted = false;

    void OnGUI()
    {
        GUILayout.Label("新Player Physics & Movement System セットアップ", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // プレイヤーオブジェクト選択
        targetPlayer = (GameObject)EditorGUILayout.ObjectField("Target Player:", targetPlayer, typeof(GameObject), true);

        if (targetPlayer == null)
        {
            EditorGUILayout.HelpBox("Playerオブジェクトまたはプレハブを選択してください", MessageType.Info);
            
            if (GUILayout.Button("自動でPlayerプレハブを検索"))
            {
                FindPlayerPrefab();
            }
        }
        else
        {
            GUILayout.Space(10);
            
            // 現在の設定表示
            ShowCurrentConfiguration();
            
            GUILayout.Space(10);
            
            // セットアップボタン
            if (GUILayout.Button("新システムをセットアップ", GUILayout.Height(30)))
            {
                SetupNewPlayerSystem();
            }
            
            if (setupCompleted)
            {
                EditorGUILayout.HelpBox("セットアップ完了！新しいPlayer Systemが有効になりました。", MessageType.Info);
                
                if (GUILayout.Button("旧ThirdPersonControllerを無効化"))
                {
                    DisableOldComponents();
                }
            }
        }
    }

    /// <summary>
    /// Playerプレハブを自動検索
    /// </summary>
    private void FindPlayerPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("Player t:GameObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("StarterAssets") && path.Contains("Player.prefab"))
            {
                targetPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[NewPlayerSystemSetup] Player.prefabを発見: {path}");
                return;
            }
        }
        
        Debug.LogWarning("[NewPlayerSystemSetup] Player.prefabが見つかりません");
    }

    /// <summary>
    /// 現在の設定表示
    /// </summary>
    private void ShowCurrentConfiguration()
    {
        EditorGUILayout.LabelField("現在の設定:", EditorStyles.boldLabel);
        
        // 既存コンポーネント確認
        var thirdPersonController = targetPlayer.GetComponent<StarterAssets.ThirdPersonController>();
        var starterAssetsInputs = targetPlayer.GetComponent<StarterAssetsInputs>();
        MonoBehaviour gpsController = null;
        var components = targetPlayer.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component.GetType().Name == "CharacterGPSCompassController")
            {
                gpsController = component;
                break;
            }
        }
        
        EditorGUILayout.LabelField($"ThirdPersonController: {(thirdPersonController != null ? "有" : "無")}");
        EditorGUILayout.LabelField($"StarterAssetsInputs: {(starterAssetsInputs != null ? "有" : "無")}");
        EditorGUILayout.LabelField($"GPS Controller: {(gpsController != null ? "有" : "無")}");
        
        // 新コンポーネント確認
        var inputCoordinator = targetPlayer.GetComponent<InputCoordinator>();
        var simpleMovement = targetPlayer.GetComponent<SimpleMovementController>();
        var simpleCamera = targetPlayer.GetComponent<SimpleCameraController>();
        var uiActionManager = targetPlayer.GetComponent<UIActionManager>();
        
        EditorGUILayout.LabelField($"InputCoordinator: {(inputCoordinator != null ? "有" : "無")}");
        EditorGUILayout.LabelField($"SimpleMovementController: {(simpleMovement != null ? "有" : "無")}");
        EditorGUILayout.LabelField($"SimpleCameraController: {(simpleCamera != null ? "有" : "無")}");
        EditorGUILayout.LabelField($"UIActionManager: {(uiActionManager != null ? "有" : "無")}");
    }

    /// <summary>
    /// 新プレイヤーシステムセットアップ
    /// </summary>
    private void SetupNewPlayerSystem()
    {
        if (targetPlayer == null)
        {
            Debug.LogError("[NewPlayerSystemSetup] Target Playerが設定されていません");
            return;
        }

        Debug.Log("[NewPlayerSystemSetup] 新Player Systemセットアップ開始...");

        // Undo記録
        Undo.RegisterCompleteObjectUndo(targetPlayer, "Setup New Player System");

        try
        {
            // 1. 必要なコンポーネント追加
            AddNewComponents();
            
            // 2. InputActionsアセット設定
            SetupInputActions();
            
            // 3. コンポーネント間の参照設定
            ConfigureComponentReferences();
            
            // 4. プレハブの場合は保存
            if (PrefabUtility.IsPartOfPrefabAsset(targetPlayer))
            {
                EditorUtility.SetDirty(targetPlayer);
                AssetDatabase.SaveAssets();
            }
            
            setupCompleted = true;
            Debug.Log("[NewPlayerSystemSetup] セットアップ完了！");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NewPlayerSystemSetup] セットアップエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 新コンポーネント追加
    /// </summary>
    private void AddNewComponents()
    {
        // InputCoordinator追加
        if (targetPlayer.GetComponent<InputCoordinator>() == null)
        {
            targetPlayer.AddComponent<InputCoordinator>();
        }

        // SimpleMovementController追加
        if (targetPlayer.GetComponent<SimpleMovementController>() == null)
        {
            targetPlayer.AddComponent<SimpleMovementController>();
        }

        // SimpleCameraController追加
        if (targetPlayer.GetComponent<SimpleCameraController>() == null)
        {
            targetPlayer.AddComponent<SimpleCameraController>();
        }

        // UIActionManager追加
        if (targetPlayer.GetComponent<UIActionManager>() == null)
        {
            targetPlayer.AddComponent<UIActionManager>();
        }

        Debug.Log("[NewPlayerSystemSetup] 新コンポーネント追加完了");
    }

    /// <summary>
    /// InputActionsアセット設定
    /// </summary>
    private void SetupInputActions()
    {
        var playerInput = targetPlayer.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = targetPlayer.AddComponent<PlayerInput>();
        }

        // PlayerControls.inputactionsを検索して設定
        string[] guids = AssetDatabase.FindAssets("PlayerControls t:InputActionAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            playerInput.actions = inputActions;
            
            Debug.Log($"[NewPlayerSystemSetup] InputActions設定完了: {path}");
        }
        else
        {
            Debug.LogWarning("[NewPlayerSystemSetup] PlayerControls.inputactionsが見つかりません");
        }
    }

    /// <summary>
    /// コンポーネント間参照設定
    /// </summary>
    private void ConfigureComponentReferences()
    {
        var inputCoordinator = targetPlayer.GetComponent<InputCoordinator>();
        if (inputCoordinator != null)
        {
            // GPS Controller検索・設定
            MonoBehaviour gpsController = null;
            var allComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                if (component.GetType().Name == "CharacterGPSCompassController")
                {
                    gpsController = component;
                    break;
                }
            }
            if (gpsController != null)
            {
                var serializedObject = new SerializedObject(inputCoordinator);
                serializedObject.FindProperty("gpsController").objectReferenceValue = gpsController;
                serializedObject.ApplyModifiedProperties();
            }

            // Virtual Joystick検索・設定
            var virtualJoystick = FindObjectOfType<UIVirtualJoystick>();
            if (virtualJoystick != null)
            {
                var serializedObject = new SerializedObject(inputCoordinator);
                serializedObject.FindProperty("virtualJoystick").objectReferenceValue = virtualJoystick;
                serializedObject.ApplyModifiedProperties();
            }
        }

        Debug.Log("[NewPlayerSystemSetup] コンポーネント参照設定完了");
    }

    /// <summary>
    /// 旧コンポーネント無効化
    /// </summary>
    private void DisableOldComponents()
    {
        Undo.RegisterCompleteObjectUndo(targetPlayer, "Disable Old Components");

        var thirdPersonController = targetPlayer.GetComponent<StarterAssets.ThirdPersonController>();
        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = false;
            Debug.Log("[NewPlayerSystemSetup] ThirdPersonController無効化");
        }

        // プレハブの場合は保存
        if (PrefabUtility.IsPartOfPrefabAsset(targetPlayer))
        {
            EditorUtility.SetDirty(targetPlayer);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[NewPlayerSystemSetup] 旧コンポーネント無効化完了");
    }
}