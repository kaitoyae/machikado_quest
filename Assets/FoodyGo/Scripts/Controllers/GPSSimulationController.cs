using UnityEngine;
using packt.FoodyGO.Services;
using packt.FoodyGO.Mapping;

namespace packt.FoodyGO.Controllers
{
    public class GPSSimulationController : MonoBehaviour
    {
        [Header("Simulation Controls")]
        public GPSLocationService gpsService;
        public float moveSpeed = 0.0002f;
        
        [Header("Keyboard Controls")]
        [Tooltip("Use arrow keys to move around")]
        public bool enableKeyboardControl = true;
        
        [Header("Preset Locations")]
        public PresetLocation[] presetLocations = new PresetLocation[]
        {
            new PresetLocation("東京駅", 35.6812f, 139.7671f),
            new PresetLocation("渋谷駅", 35.6580f, 139.7016f),
            new PresetLocation("新宿駅", 35.6896f, 139.7006f),
            new PresetLocation("秋葉原", 35.6984f, 139.7731f)
        };
        
        void Update()
        {
            if (!gpsService || !gpsService.Simulating) return;
            
            // キーボード操作でGPS座標を移動
            if (enableKeyboardControl)
            {
                bool moved = false;
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    gpsService.Latitude += moveSpeed * Time.deltaTime;
                    moved = true;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    gpsService.Latitude -= moveSpeed * Time.deltaTime;
                    moved = true;
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    gpsService.Longitude += moveSpeed * Time.deltaTime;
                    moved = true;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    gpsService.Longitude -= moveSpeed * Time.deltaTime;
                    moved = true;
                }
                    
                // Shift押しながらで高速移動
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float boostMultiplier = 5f;
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        gpsService.Latitude += moveSpeed * boostMultiplier * Time.deltaTime;
                        moved = true;
                    }
                    if (Input.GetKey(KeyCode.DownArrow))
                    {
                        gpsService.Latitude -= moveSpeed * boostMultiplier * Time.deltaTime;
                        moved = true;
                    }
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        gpsService.Longitude += moveSpeed * boostMultiplier * Time.deltaTime;
                        moved = true;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        gpsService.Longitude -= moveSpeed * boostMultiplier * Time.deltaTime;
                        moved = true;
                    }
                }
                
                // 移動があった場合のみタイムスタンプを更新
                if (moved)
                {
                    gpsService.PlayerTimestamp = System.DateTime.Now.Ticks;
                }
                
                // 数字キーでプリセット位置にジャンプ
                for (int i = 0; i < presetLocations.Length && i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        JumpToLocation(presetLocations[i]);
                    }
                }
            }
        }
        
        void JumpToLocation(PresetLocation location)
        {
            gpsService.Latitude = location.latitude;
            gpsService.Longitude = location.longitude;
            Debug.Log($"Jumped to {location.name}: {location.latitude}, {location.longitude}");
        }
        
        void OnGUI()
        {
            if (!gpsService || !gpsService.Simulating) return;
            
            // GPS情報を画面に表示
            GUI.Box(new Rect(10, 10, 300, 150), "GPS Simulation");
            GUI.Label(new Rect(20, 40, 280, 20), $"Lat: {gpsService.Latitude:F6}");
            GUI.Label(new Rect(20, 60, 280, 20), $"Lon: {gpsService.Longitude:F6}");
            GUI.Label(new Rect(20, 80, 280, 20), "Arrow Keys: Move");
            GUI.Label(new Rect(20, 100, 280, 20), "Shift + Arrow: Fast Move");
            GUI.Label(new Rect(20, 120, 280, 20), "Number Keys: Jump to Preset");
        }
    }
    
    [System.Serializable]
    public class PresetLocation
    {
        public string name;
        public float latitude;
        public float longitude;
        
        public PresetLocation(string name, float lat, float lon)
        {
            this.name = name;
            this.latitude = lat;
            this.longitude = lon;
        }
    }
}