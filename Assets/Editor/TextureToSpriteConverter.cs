using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureToSpriteConverter : EditorWindow
{
    [MenuItem("Tools/CardEntityList/Convert Textures to Sprites")]
    public static void ShowWindow()
    {
        GetWindow<TextureToSpriteConverter>("Texture to Sprite Converter");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Texture to Sprite Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("This will convert all textures in Assets/Images/Cards/ folder to Sprite type.", MessageType.Info);
        
        if (GUILayout.Button("Convert All Card Images to Sprites"))
        {
            ConvertTexturesToSprites();
        }
    }
    
    void ConvertTexturesToSprites()
    {
        string folderPath = "Assets/Images/Cards";
        
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Folder not found: " + folderPath, "OK");
            return;
        }
        
        string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { folderPath });
        int convertedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                convertedCount++;
                Debug.Log($"Converted to Sprite: {path}");
            }
        }
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Complete", $"Converted {convertedCount} textures to sprites!", "OK");
    }
    
    // Auto-convert on import
    class TexturePostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/Images/Cards/"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
            }
        }
    }
}