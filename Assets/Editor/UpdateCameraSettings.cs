using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using StarterAssets;
using packt.FoodyGO.Controllers;
using System.Collections.Generic;
using Cinemachine;

/// <summary>
/// エディタツールとして、シーン内のカメラとプレイヤー設定を更新するスクリプト
/// </summary>
public class UpdateCameraSettings : EditorWindow
{
    // ポケモンGOスタイルのカメラ設定
    private float cameraHeight = 8.0f;
    private float cameraDistance = 15.0f;
    private float cameraFieldOfView = 50.0f;
    private float cameraAngle = 60.0f;
    
    // 3rdPersonFollow設定
    private float topRigHeight = 8.0f;
    private float topRigRadius = 15.0f;
    private float middleRigHeight = 5.0f;
    private float middleRigRadius = 10.0f;
    private float bottomRigHeight = 2.0f;
    private float bottomRigRadius = 6.0f;
    private float splineCurvature = 0.5f;
    
    // キャッシュされたオブジェクト参照
    private List<CinemachineVirtualCamera> cachedCameras;
    private List<ThirdPersonController> cachedControllers;
    private List<CharacterGPSCompassController> cachedGPSControllers;
    private List<GameObject> cachedPlayerCameraRoots;
    
    // 更新操作のフラグ
    private enum UpdateOperation {
        None,
        UpdateCamera,
        UpdatePlayer,
        UpdateAll
    }
    
    private UpdateOperation pendingOperation = UpdateOperation.None;
    private bool isProcessing = false;
    
    [MenuItem("Tools/Update Camera Settings")]
    public static void ShowWindow()
    {
        GetWindow<UpdateCameraSettings>("カメラ設定更新");
    }
    
    private void OnEnable()
    {
        // 保留中の操作があれば解除
        ResetPendingOperation();
        // シーン変更イベントを購読
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }
    
    private void OnDisable()
    {
        // イベント購読を解除
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        // 保留中の操作をキャンセル
        ResetPendingOperation();
        // キャッシュをクリア
        ClearCache();
    }
    
    private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        // シーンが変更されたらキャッシュをクリア
        ClearCache();
    }
    
    private void ClearCache()
    {
        cachedCameras = null;
        cachedControllers = null;
        cachedGPSControllers = null;
        cachedPlayerCameraRoots = null;
    }
    
    private void ResetPendingOperation()
    {
        if (pendingOperation != UpdateOperation.None)
        {
            EditorApplication.delayCall -= ExecutePendingOperation;
            pendingOperation = UpdateOperation.None;
        }
        isProcessing = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ポケモンGOスタイルのカメラ設定を適用", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 基本カメラ設定
        EditorGUILayout.LabelField("基本カメラ設定", EditorStyles.boldLabel);
        cameraHeight = EditorGUILayout.FloatField("カメラ高さ", cameraHeight);
        cameraDistance = EditorGUILayout.FloatField("カメラ距離", cameraDistance);
        cameraFieldOfView = EditorGUILayout.FloatField("視野角 (FOV)", cameraFieldOfView);
        cameraAngle = EditorGUILayout.FloatField("カメラ角度", cameraAngle);
        
        EditorGUILayout.Space(10);
        
        // 3rdPersonFollow設定
        EditorGUILayout.LabelField("3rdPersonFollow設定", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.LabelField("TopRig", EditorStyles.miniBoldLabel);
        topRigHeight = EditorGUILayout.FloatField("高さ", topRigHeight);
        topRigRadius = EditorGUILayout.FloatField("距離", topRigRadius);
        
        EditorGUILayout.LabelField("MiddleRig", EditorStyles.miniBoldLabel);
        middleRigHeight = EditorGUILayout.FloatField("高さ", middleRigHeight);
        middleRigRadius = EditorGUILayout.FloatField("距離", middleRigRadius);
        
        EditorGUILayout.LabelField("BottomRig", EditorStyles.miniBoldLabel);
        bottomRigHeight = EditorGUILayout.FloatField("高さ", bottomRigHeight);
        bottomRigRadius = EditorGUILayout.FloatField("距離", bottomRigRadius);
        
        splineCurvature = EditorGUILayout.Slider("スプラインの曲率", splineCurvature, 0f, 1f);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space(20);

        // 処理中かつオブジェクトが見つからない場合、キャッシュ更新ボタンを表示
        if (!isProcessing && IsAnyCacheEmpty())
        {
            if (GUILayout.Button("シーン内のオブジェクトを検索"))
            {
                // キャッシュを更新
                UpdateObjectCache();
            }
            
            EditorGUILayout.HelpBox("設定を適用する前に、シーン内のオブジェクトを検索してください。", MessageType.Info);
        }
        
        // オブジェクトキャッシュの状態を表示
        if (cachedCameras != null)
            EditorGUILayout.LabelField($"検出されたカメラ: {cachedCameras.Count}");
        if (cachedControllers != null)
            EditorGUILayout.LabelField($"検出されたコントローラー: {cachedControllers.Count}");
        if (cachedGPSControllers != null)
            EditorGUILayout.LabelField($"検出されたGPSコントローラー: {cachedGPSControllers.Count}");
        if (cachedPlayerCameraRoots != null)
            EditorGUILayout.LabelField($"検出されたカメラルート: {cachedPlayerCameraRoots.Count}");

        EditorGUILayout.Space(10);
        
        EditorGUI.BeginDisabledGroup(isProcessing || IsAnyCacheEmpty());
        
        // ボタンクリック時にフラグを設定
        if (GUILayout.Button("シーン内のカメラ設定を更新"))
        {
            pendingOperation = UpdateOperation.UpdateCamera;
            isProcessing = true;
            EditorApplication.delayCall += ExecutePendingOperation;
        }

        if (GUILayout.Button("シーン内のプレイヤー設定を更新"))
        {
            pendingOperation = UpdateOperation.UpdatePlayer;
            isProcessing = true;
            EditorApplication.delayCall += ExecutePendingOperation;
        }

        if (GUILayout.Button("全ての設定を更新"))
        {
            pendingOperation = UpdateOperation.UpdateAll;
            isProcessing = true;
            EditorApplication.delayCall += ExecutePendingOperation;
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (isProcessing)
        {
            EditorGUILayout.HelpBox("更新処理を実行中です...", MessageType.Info);
        }
    }
    
    // キャッシュがいずれかが空かどうかチェック
    private bool IsAnyCacheEmpty()
    {
        return cachedCameras == null || cachedControllers == null || 
               cachedGPSControllers == null || cachedPlayerCameraRoots == null;
    }
    
    // オブジェクトキャッシュの更新
    private void UpdateObjectCache()
    {
        // プログレスバーを表示
        EditorUtility.DisplayProgressBar("オブジェクト検索中", "シーン内のオブジェクトを検索しています...", 0.0f);
        
        try
        {
            // バッチ処理で全てのオブジェクトタイプを一度に検索
            cachedCameras = new List<CinemachineVirtualCamera>(Object.FindObjectsOfType<CinemachineVirtualCamera>());
            EditorUtility.DisplayProgressBar("オブジェクト検索中", "シーン内のオブジェクトを検索しています...", 0.25f);
            
            cachedControllers = new List<ThirdPersonController>(Object.FindObjectsOfType<ThirdPersonController>());
            EditorUtility.DisplayProgressBar("オブジェクト検索中", "シーン内のオブジェクトを検索しています...", 0.5f);
            
            cachedGPSControllers = new List<CharacterGPSCompassController>(Object.FindObjectsOfType<CharacterGPSCompassController>());
            EditorUtility.DisplayProgressBar("オブジェクト検索中", "シーン内のオブジェクトを検索しています...", 0.75f);
            
            // PlayerCameraRootの検索
            cachedPlayerCameraRoots = new List<GameObject>();
            GameObject[] playerCameraRoots = GameObject.FindGameObjectsWithTag("CinemachineTarget");
            foreach (var root in playerCameraRoots)
            {
                if (root != null)
                {
                    cachedPlayerCameraRoots.Add(root);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        // ウィンドウを再描画
        Repaint();
    }
    
    // OnGUI外で実行される遅延処理
    private void ExecutePendingOperation()
    {
        // delayCallを解除
        EditorApplication.delayCall -= ExecutePendingOperation;
        
        try
        {
            // 処理の実行
            switch (pendingOperation)
            {
                case UpdateOperation.UpdateCamera:
                    UpdateCameraSettingsInScene();
                    break;
                case UpdateOperation.UpdatePlayer:
                    UpdatePlayerSettingsInScene();
                    break;
                case UpdateOperation.UpdateAll:
                    UpdateCameraSettingsInScene();
                    UpdatePlayerSettingsInScene();
                    break;
            }
            
            // 変更を保存
            EditorSceneManager.MarkAllScenesDirty();
            
            EditorUtility.DisplayDialog("更新完了", "カメラ設定の更新が完了しました。", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"設定更新中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("エラー", $"設定更新中にエラーが発生しました: {e.Message}", "OK");
        }
        finally
        {
            // 処理完了後にフラグをリセット
            pendingOperation = UpdateOperation.None;
            isProcessing = false;
            
            // ウィンドウを再描画
            Repaint();
        }
    }
    
    // カメラ設定の更新
    private void UpdateCameraSettingsInScene()
    {
        // カメラの更新
        UpdateCinemachineCameras();
        
        // ThirdPersonControllerの更新
        UpdateThirdPersonControllers();
        
        // CharacterGPSCompassControllerの更新
        UpdateGPSControllers();
    }
    
    // プレイヤー設定の更新
    private void UpdatePlayerSettingsInScene()
    {
        if (cachedPlayerCameraRoots == null || cachedPlayerCameraRoots.Count == 0)
        {
            Debug.LogWarning("No player camera roots found to update in the current scene.");
            return;
        }
        
        Undo.RecordObjects(cachedPlayerCameraRoots.ToArray(), "Update Player Camera Root Positions");
        
        foreach (var root in cachedPlayerCameraRoots)
        {
            if (root == null) continue;
            root.transform.localPosition = new Vector3(0, 2.5f, -1.0f);
        }
        
        Debug.Log($"Updated {cachedPlayerCameraRoots.Count} player camera roots!");
    }
    
    // Cinemachineカメラの更新
    private void UpdateCinemachineCameras()
    {
        if (cachedCameras == null || cachedCameras.Count == 0)
        {
            Debug.LogWarning("No Cinemachine cameras found in the scene.");
            return;
        }
        
        int updatedCount = 0;
        
        // プログレスバーを表示
        EditorUtility.DisplayProgressBar("カメラ設定更新", "Cinemachineカメラを更新中...", 0.0f);
        
        try
        {
            // 一括でUndo登録
            Undo.RecordObjects(cachedCameras.ToArray(), "Update Cinemachine Cameras");
            
            // すべてのカメラを効率的に処理
            for (int i = 0; i < cachedCameras.Count; i++)
            {
                var camera = cachedCameras[i];
                if (camera == null) continue;
                
                // 進捗表示を更新
                float progress = (float)i / cachedCameras.Count;
                EditorUtility.DisplayProgressBar("カメラ設定更新", $"カメラ {i+1}/{cachedCameras.Count} を更新中...", progress);
                
                // 基本設定の更新
                camera.transform.position = new Vector3(0.5f, cameraHeight, -cameraDistance);
                camera.m_Lens.FieldOfView = cameraFieldOfView;
                camera.m_Lens.FarClipPlane = 1000;
                
                // Cinemachineコンポーネント設定の更新
                UpdateCameraComponents(camera);
                
                updatedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        Debug.Log($"Updated {updatedCount} Cinemachine cameras.");
    }
    
    // カメラコンポーネントの更新
    private void UpdateCameraComponents(CinemachineVirtualCamera camera)
    {
        // Transposer
        var transposer = camera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            Undo.RecordObject(transposer, "Update Transposer");
            transposer.m_FollowOffset = new Vector3(0.3f, cameraHeight, -cameraDistance);
        }
        
        // OrbitalTransposer
        var orbitalTransposer = camera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        if (orbitalTransposer != null)
        {
            Undo.RecordObject(orbitalTransposer, "Update Orbital Transposer");
            orbitalTransposer.m_FollowOffset = new Vector3(0, cameraHeight, -cameraDistance);
        }
        
        // 3rdPersonFollow - 直接編集可能なプロパティを使用
        var thirdPersonFollow = camera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (thirdPersonFollow != null)
        {
            Undo.RecordObject(thirdPersonFollow, "Update 3rd Person Follow");
            
            thirdPersonFollow.ShoulderOffset = new Vector3(0.3f, 0, 0);
            thirdPersonFollow.VerticalArmLength = 2.5f;
            thirdPersonFollow.CameraDistance = cameraDistance;
            
            // 非公開フィールドを更新
            UpdateThirdPersonFollowRigs(thirdPersonFollow);
        }
        
        // POV
        var pov = camera.GetCinemachineComponent<CinemachinePOV>();
        if (pov != null)
        {
            Undo.RecordObject(pov, "Update POV");
            pov.m_VerticalAxis.Value = cameraAngle;
        }
    }
    
    // サードパーソンカメラのRig設定を最適化
    private void UpdateThirdPersonFollowRigs(Cinemachine3rdPersonFollow thirdPersonFollow)
    {
        SerializedObject serializedObject = new SerializedObject(thirdPersonFollow);
        
        // 一度に複数のプロパティを変更して、ApplyModifiedPropertiesの呼び出しを1回にする
        var topRig = serializedObject.FindProperty("m_TopRig");
        var middleRig = serializedObject.FindProperty("m_MiddleRig");
        var bottomRig = serializedObject.FindProperty("m_BottomRig");
        var splineCurve = serializedObject.FindProperty("m_SplineCurvature");
        
        if (topRig != null)
        {
            topRig.FindPropertyRelative("m_Height").floatValue = topRigHeight;
            topRig.FindPropertyRelative("m_Radius").floatValue = topRigRadius;
        }
        
        if (middleRig != null)
        {
            middleRig.FindPropertyRelative("m_Height").floatValue = middleRigHeight;
            middleRig.FindPropertyRelative("m_Radius").floatValue = middleRigRadius;
        }
        
        if (bottomRig != null)
        {
            bottomRig.FindPropertyRelative("m_Height").floatValue = bottomRigHeight;
            bottomRig.FindPropertyRelative("m_Radius").floatValue = bottomRigRadius;
        }
        
        if (splineCurve != null)
        {
            splineCurve.floatValue = splineCurvature;
        }
        
        // 変更を一度に適用
        serializedObject.ApplyModifiedProperties();
    }
    
    // ThirdPersonControllerの更新
    private void UpdateThirdPersonControllers()
    {
        if (cachedControllers == null || cachedControllers.Count == 0)
        {
            Debug.LogWarning("No ThirdPersonController found in the scene.");
            return;
        }
        
        // 一括でUndo登録
        Undo.RecordObjects(cachedControllers.ToArray(), "Update ThirdPersonControllers");
        
        foreach (var controller in cachedControllers)
        {
            if (controller == null) continue;
            
            controller.TopClamp = 89;
            controller.BottomClamp = 0;
            controller.CameraAngleOverride = cameraAngle;
        }
        
        Debug.Log($"Updated {cachedControllers.Count} ThirdPersonControllers.");
    }
    
    // CharacterGPSCompassControllerの更新
    private void UpdateGPSControllers()
    {
        if (cachedGPSControllers == null || cachedGPSControllers.Count == 0)
        {
            Debug.LogWarning("No CharacterGPSCompassController found in the scene.");
            return;
        }
        
        // 一括でUndo登録
        Undo.RecordObjects(cachedGPSControllers.ToArray(), "Update GPS Controllers");
        
        foreach (var controller in cachedGPSControllers)
        {
            if (controller == null) continue;
            
            controller.pokemonStyleCameraHeight = cameraHeight;
            controller.pokemonStyleCameraDistance = cameraDistance;
            controller.pokemonStyleCameraAngle = cameraAngle;
        }
        
        Debug.Log($"Updated {cachedGPSControllers.Count} GPS Controllers.");
    }
} 