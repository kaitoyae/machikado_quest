using UnityEditor;
using UnityEngine;

/// <summary>
/// プロジェクトの定義シンボル管理
/// Cinemachineの存在確認とマクロ定義
/// </summary>
[InitializeOnLoad]
public class DefineSymbolsManager
{
    static DefineSymbolsManager()
    {
        SetupDefineSymbols();
    }

    private static void SetupDefineSymbols()
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        
        // Cinemachineパッケージの存在確認
        bool hasCinemachine = HasPackage("com.unity.cinemachine");
        
        if (hasCinemachine && !defines.Contains("CINEMACHINE_PRESENT"))
        {
            defines += ";CINEMACHINE_PRESENT";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            Debug.Log("[DefineSymbolsManager] CINEMACHINE_PRESENT マクロを追加しました");
        }
        else if (!hasCinemachine && defines.Contains("CINEMACHINE_PRESENT"))
        {
            defines = defines.Replace(";CINEMACHINE_PRESENT", "").Replace("CINEMACHINE_PRESENT", "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            Debug.Log("[DefineSymbolsManager] CINEMACHINE_PRESENT マクロを削除しました");
        }
    }

    private static bool HasPackage(string packageName)
    {
        var request = UnityEditor.PackageManager.Client.List();
        while (!request.IsCompleted)
        {
            System.Threading.Thread.Sleep(10);
        }
        
        if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
        {
            foreach (var package in request.Result)
            {
                if (package.name == packageName)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
}