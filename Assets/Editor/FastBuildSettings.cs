using UnityEngine;
using UnityEditor;

public class FastBuildSettings : EditorWindow
{
    [MenuItem("Tools/Fast Build Settings")]
    static void ShowWindow()
    {
        GetWindow<FastBuildSettings>("Fast Build");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Fast Build Configuration", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Apply Fast Build Settings"))
        {
            // テクスチャ圧縮を無効化
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
            
            // 開発ビルド設定
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildScriptsOnly = false;
            
            // iOS固有の設定
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Disabled);
            
            Debug.Log("Fast build settings applied!");
        }
        
        if (GUILayout.Button("Build and Run (iOS)"))
        {
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = new[] { "Assets/Map.unity" };
            buildOptions.locationPathName = "Builds/iOS";
            buildOptions.target = BuildTarget.iOS;
            buildOptions.options = BuildOptions.AutoRunPlayer | BuildOptions.Development;
            
            BuildPipeline.BuildPlayer(buildOptions);
        }
    }
}