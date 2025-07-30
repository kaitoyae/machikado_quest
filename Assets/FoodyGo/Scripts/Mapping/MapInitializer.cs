using UnityEngine;
using packt.FoodyGO.Services;

namespace packt.FoodyGO.Mapping
{
    [AddComponentMenu("Mapping/MapInitializer")]
    public class MapInitializer : MonoBehaviour
    {
        [Header("初期設定")]
        [Tooltip("デフォルトの緯度（東京）")]
        public float defaultLatitude = 35.68169F;
        
        [Tooltip("デフォルトの経度（東京）")]
        public float defaultLongitude = 139.76608F;
        
        [Tooltip("すべてのマップタイルを初期化")]
        public bool initializeAllMapTiles = true;
        
        private void Awake()
        {
            // 最初に実行されるように設定
            Debug.Log("[MapInitializer] Initializing map tiles...");
        }
        
        private void Start()
        {
            if (initializeAllMapTiles)
            {
                InitializeAllMapTiles();
            }
        }
        
        private void InitializeAllMapTiles()
        {
            // シーン内のすべてのGoogleMapTileコンポーネントを取得
            GoogleMapTile[] mapTiles = FindObjectsOfType<GoogleMapTile>();
            
            if (mapTiles.Length == 0)
            {
                Debug.LogWarning("[MapInitializer] No GoogleMapTile components found in the scene");
                return;
            }
            
            Debug.Log($"[MapInitializer] Found {mapTiles.Length} GoogleMapTile components");
            
            // GPSLocationServiceを取得
            GPSLocationService gpsService = FindObjectOfType<GPSLocationService>();
            
            if (gpsService == null)
            {
                Debug.LogWarning("[MapInitializer] GPSLocationService not found");
            }
            
            // すべてのマップタイルの初期座標を設定
            foreach (GoogleMapTile tile in mapTiles)
            {
                // worldCenterLocationの座標を東京に設定
                tile.worldCenterLocation.Latitude = defaultLatitude;
                tile.worldCenterLocation.Longitude = defaultLongitude;
                
                // GPSLocationServiceの参照を設定
                if (gpsService != null && tile.gpsLocationService == null)
                {
                    tile.gpsLocationService = gpsService;
                }
                
                Debug.Log($"[MapInitializer] Initialized map tile {tile.name} to Lat={defaultLatitude}, Long={defaultLongitude}");
            }
        }
    }
} 