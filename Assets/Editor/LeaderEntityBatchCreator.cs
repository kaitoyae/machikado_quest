using UnityEngine;
using UnityEditor;
using System.IO;

public class LeaderEntityBatchCreator : EditorWindow
{
    private TextAsset csvFile;
    
    [MenuItem("Tools/LeaderEntityList/Import from CSV")]
    public static void ShowWindow()
    {
        GetWindow<LeaderEntityBatchCreator>("Leader Entity CSV Importer");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Leader Entity CSV Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File:", csvFile, typeof(TextAsset), false);
        
        EditorGUILayout.Space();
        
        if (csvFile != null)
        {
            if (GUILayout.Button("Import Leaders from CSV"))
            {
                ImportLeadersFromCSV();
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("CSV Format: id,name,hp,ability\nExample: 1,Fire Lord,20,Deal 1 damage to all enemies at turn start\nNote: Image files should be named as {id}.png and placed in Assets/Images/Leaders/", MessageType.Info);
    }
    
    void ImportLeadersFromCSV()
    {
        if (csvFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a CSV file", "OK");
            return;
        }
        
        string csvText = csvFile.text;
        string[] lines = csvText.Split('\n');
        
        string folderPath = "Assets/Resources/LeaderEntityList";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder("Assets/Resources", "LeaderEntityList");
        }
        
        // Create image folder if not exists
        string imageFolderPath = "Assets/Images/Leaders";
        if (!AssetDatabase.IsValidFolder(imageFolderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Images"))
            {
                AssetDatabase.CreateFolder("Assets", "Images");
            }
            AssetDatabase.CreateFolder("Assets/Images", "Leaders");
        }
        
        int createdCount = 0;
        
        for (int i = 1; i < lines.Length; i++) // Skip header line
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length < 4) continue;
            
            // Parse ID
            if (!int.TryParse(values[0].Trim(), out int leaderId)) continue;
            
            LeaderEntity newLeader = ScriptableObject.CreateInstance<LeaderEntity>();
            newLeader.name = values[1].Trim();
            
            if (int.TryParse(values[2].Trim(), out int hp))
                newLeader.hp = hp;
            
            newLeader.ability = values[3].Trim();
            
            // Load image using ID
            string imagePath = $"Assets/Images/Leaders/{leaderId}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            if (sprite != null)
            {
                newLeader.icon = sprite;
            }
            else
            {
                Debug.LogWarning($"Leader image not found at path: {imagePath}");
            }
            
            string assetPath = $"{folderPath}/Leader{leaderId}.asset";
            AssetDatabase.CreateAsset(newLeader, assetPath);
            createdCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"Created {createdCount} leader assets!", "OK");
    }
}