#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace packt.FoodyGO.Mapping
{
    [InitializeOnLoad]
    public static class MapInitializerInstaller
    {
        static MapInitializerInstaller()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            if (EditorApplication.isPlaying)
                return;

            // Map_Tilesオブジェクトを探す
            GameObject mapTilesObj = GameObject.Find("Map_Tiles");
            
            if (mapTilesObj != null)
            {
                // MapInitializerがまだ追加されていない場合は追加
                if (mapTilesObj.GetComponent<MapInitializer>() == null)
                {
                    MapInitializer initializer = mapTilesObj.AddComponent<MapInitializer>();
                    Debug.Log("[MapInitializerInstaller] Added MapInitializer to Map_Tiles");
                    
                    // シーンを変更済みとしてマーク
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }
    }
}
#endif 