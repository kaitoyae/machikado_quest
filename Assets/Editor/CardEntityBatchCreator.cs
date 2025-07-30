using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public class CardEntityBatchCreator : EditorWindow
{
    private string csvFilePath = "";
    private TextAsset csvFile;
    
    [MenuItem("Tools/CardEntityList/Import from CSV")]
    public static void ShowWindow()
    {
        GetWindow<CardEntityBatchCreator>("Card Entity CSV Importer");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Card Entity CSV Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File:", csvFile, typeof(TextAsset), false);
        
        EditorGUILayout.Space();
        
        if (csvFile != null)
        {
            if (GUILayout.Button("Import Cards from CSV"))
            {
                ImportCardsFromCSV();
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("CSV Format: id,name,at,hp,cost,effect\nExample: 1,Fire Dragon,50,100,5,登場時：敵ユニット１体に３ダメージ\nNote: Image files should be named as {id}.png and placed in Assets/Images/Cards/", MessageType.Info);
    }
    
    void ImportCardsFromCSV()
    {
        if (csvFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a CSV file", "OK");
            return;
        }
        
        string csvText = csvFile.text;
        string[] lines = csvText.Split('\n');
        
        Debug.Log($"CSV lines count: {lines.Length}");
        
        string folderPath = "Assets/Resources/CardEntityList";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateFolder("Assets/Resources", "CardEntityList");
        }
        
        int createdCount = 0;
        
        for (int i = 1; i < lines.Length; i++) // Skip header line
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            Debug.Log($"Processing line {i}: {line}");
            
            string[] values = line.Split(',');
            Debug.Log($"Values count: {values.Length}");
            
            if (values.Length < 5)
            {
                Debug.LogWarning($"Line {i} has only {values.Length} values, skipping...");
                continue;
            }
            
            // Parse ID
            if (!int.TryParse(values[0].Trim(), out int cardId)) continue;
            
            CardEntity newCard = ScriptableObject.CreateInstance<CardEntity>();
            newCard.name = values[1].Trim();
            
            if (int.TryParse(values[2].Trim(), out int at))
                newCard.at = at;
            
            if (int.TryParse(values[3].Trim(), out int hp))
                newCard.hp = hp;
            
            if (int.TryParse(values[4].Trim(), out int cost))
                newCard.cost = cost;
            
            // 効果テキストの設定
            if (values.Length >= 6)
            {
                newCard.effectText = values[5].Trim();
            }
            else
            {
                newCard.effectText = "※効果なし";
            }
            
            // 複数効果の初期化（現在は単一効果のみ対応）
            newCard.multipleEffects = new List<string>();
            if (!string.IsNullOrEmpty(newCard.effectText) && newCard.effectText != "※効果なし")
            {
                newCard.multipleEffects.Add(newCard.effectText);
            }
            
            // 効果の検証（ParseFromStringでテスト）
            newCard.isEffectValidated = ValidateEffect(newCard.effectText);
            
            // Load image using ID
            string imagePath = $"Assets/Images/Cards/{cardId}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            if (sprite != null)
            {
                newCard.icon = sprite;
            }
            else
            {
                Debug.LogWarning($"Image not found at path: {imagePath}");
            }
            
            string assetPath = $"{folderPath}/Card{cardId}.asset";
            AssetDatabase.CreateAsset(newCard, assetPath);
            createdCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Success", $"Created {createdCount} card assets!", "OK");
    }
    
    private bool ValidateEffect(string effectText)
    {
        if (string.IsNullOrEmpty(effectText) || effectText == "※効果なし")
        {
            return true; // 効果なしは有効
        }
        
        try
        {
            // CardEffectDataクラスのParseFromStringメソッドでテスト
            // 実際にはCardEffectDataクラスを参照できない場合があるので、
            // 基本的な効果文字列のパターンマッチングで検証
            
            // 基本的なパターン検証
            if (effectText.Contains("登場時") || effectText.Contains("攻撃時") || effectText.Contains("死亡時"))
            {
                if (effectText.Contains("ダメージ") || effectText.Contains("回復") || 
                    effectText.Contains("HP+") || effectText.Contains("攻撃力+") ||
                    effectText.Contains("攻撃力-"))
                {
                    return true;
                }
            }
            
            return false; // パターンにマッチしない場合は無効
        }
        catch
        {
            return false; // 例外が発生した場合は無効
        }
    }
}
