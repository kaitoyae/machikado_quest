using UnityEngine;
using System.Collections;
using packt.FoodyGO.Mapping;
using System.Collections.Generic;
using packt.FoodyGO.Database;
using packt.FoodyGo.Utils;

namespace packt.FoodyGO.Services
{
    public class MonsterService : MonoBehaviour
    {
        public GPSLocationService gpsLocationService;
        public GameObject monsterPrefab;
        private double lastTimestamp;

        [Header("Monster Spawn Parameters")]
        public float monsterSpawnRate = .75f;
        public float latitudeSpawnOffset = .001f;
        public float longitudeSpawnOffset = .001f;

        [Header("Monster Visibility")]
        public float monsterHearDistance = 200f;
        public float monsterSeeDistance = 100f;
        public float monsterLifetimeSeconds = 30;

        [Header("Monster Foot Step Range")]
        public float oneStepRange = 125f;
        public float twoStepRange = 150f;
        public float threeStepRange = 200f;

        public List<Monster> monsters;
        
        // Use this for initialization
        void Start()
        {
            monsters = new List<Monster>();

            // Initialize GPS service
            if (gpsLocationService != null)
            {
                #if UNITY_EDITOR
                // Force GPS simulation in editor
                gpsLocationService.Simulating = true;
                gpsLocationService.IsServiceStarted = true;
                
                // Set default coordinates if not set
                if (gpsLocationService.StartCoordinates.Latitude == 0 && gpsLocationService.StartCoordinates.Longitude == 0)
                {
                    gpsLocationService.StartCoordinates = new MapLocation(35.6812f, 139.7671f); // Tokyo Station (Lat, Lon)
                }
                
                gpsLocationService.Latitude = (float)gpsLocationService.StartCoordinates.Latitude;
                gpsLocationService.Longitude = (float)gpsLocationService.StartCoordinates.Longitude;
                gpsLocationService.PlayerTimestamp = 1.0; // Initialize timestamp
                #endif
            }

            StartCoroutine(CleanupMonsters());
            if (gpsLocationService != null)
            {
                gpsLocationService.OnMapRedraw += GpsLocationService_OnMapRedraw;
            }
        }

        private void GpsLocationService_OnMapRedraw(GameObject g)
        {
            // モンスターの移動を最小限に抑えるため、マップ再描画時の位置更新を制限
            foreach(Monster m in monsters)
            {
                if(m.gameObject != null)
                {
                    var newPosition = ConvertToWorldSpace(m.location.Longitude, m.location.Latitude);
                    
                    // Check if the new position is valid and not too far from current position
                    var currentPosition = m.gameObject.transform.position;
                    var distance = Vector3.Distance(currentPosition, newPosition);
                    
                    // より厳しい距離制限（5f以下のみ移動を許可）
                    if (distance < 5f && IsValidPosition(newPosition))
                    {
                        // Smooth transition instead of immediate snap
                        StartCoroutine(SmoothMoveMonster(m.gameObject, newPosition));
                    }
                    else
                    {
                        // 大きな移動の場合は現在位置を維持
                    }
                }
            }
        }
        
        private bool IsValidPosition(Vector3 position)
        {
            return float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z) &&
                   Mathf.Abs(position.x) < 1000f && Mathf.Abs(position.z) < 1000f;
        }
        
        private IEnumerator SmoothMoveMonster(GameObject monster, Vector3 targetPosition)
        {
            if (monster == null) yield break;
            
            Vector3 startPosition = monster.transform.position;
            float duration = 3.0f; // より長い時間をかけてゆっくり移動
            float elapsed = 0f;
            
            while (elapsed < duration && monster != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // より滑らかな補間を使用
                t = Mathf.SmoothStep(0f, 1f, t);
                monster.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            
            if (monster != null)
            {
                monster.transform.position = targetPosition;
            }
        }

        private IEnumerator CleanupMonsters()
        {
            while (true)
            {
                var now = Epoch.Now;
                var list = monsters.ToArray();
                for(int i = 0; i < list.Length; i++)
                {
                    if(list[i].spawnTimestamp + monsterLifetimeSeconds < now)
                    {
                        var monster = list[i];
                        print("Cleaning up monster");
                        if(monster.gameObject != null)
                        {
                            Destroy(monster.gameObject);
                        }
                        monsters.Remove(monster);
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (gpsLocationService == null || !gpsLocationService.IsServiceStarted)
            {
                return;
            }
            
            if (gpsLocationService.PlayerTimestamp <= lastTimestamp)
            {
                return;
            }
            
            lastTimestamp = gpsLocationService.PlayerTimestamp;

            //update the monsters around the player
            CheckMonsters();
        }

        private void CheckMonsters()
        {
            if (monsterPrefab == null)
            {
                return;
            }
            
            float randomValue = Random.value;
            
            if (randomValue > monsterSpawnRate)
            {
                // Fix coordinate swap issue - GPS service has swapped values
                var actualLat = gpsLocationService.Longitude; // Service Longitude is actually Latitude
                var actualLon = gpsLocationService.Latitude;  // Service Latitude is actually Longitude
                
                var mlat = actualLat + Random.Range(-latitudeSpawnOffset, latitudeSpawnOffset);
                var mlon = actualLon + Random.Range(-longitudeSpawnOffset, longitudeSpawnOffset);
                
                // Convert GPS coordinates to world position
                Vector3 worldPosition = ConvertToWorldSpace((float)mlon, (float)mlat);
                
                // Instantiate the monster GameObject with random rotation
                Quaternion randomRotation = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
                GameObject monsterGameObject = Instantiate(monsterPrefab, worldPosition, randomRotation);
                
                var monster = new Monster
                {
                    location = new MapLocation(mlon, mlat), // mlon=longitude, mlat=latitude
                    spawnTimestamp = gpsLocationService.PlayerTimestamp,
                    gameObject = monsterGameObject
                };
                monsters.Add(monster);
                
                print("Monster spawned at position: " + worldPosition + ". Total monsters: " + monsters.Count);
            }

            //store players location for easy access in distance calculations
            // Fix coordinate swap issue for player location too
            var playerLat = gpsLocationService.Longitude; // Service Longitude is actually Latitude  
            var playerLon = gpsLocationService.Latitude;  // Service Latitude is actually Longitude
            var playerLocation = new MapLocation(playerLon, playerLat); // Correct order: Lon, Lat
            //get the current Epoch time in seconds
            var now = Epoch.Now;

            foreach (Monster m in monsters)
            {
                var d = MathG.Distance(m.location, playerLocation);
                if (MathG.Distance(m.location, playerLocation) < monsterSeeDistance)
                {
                    m.lastSeenTimestamp = now;
                    m.footstepRange = 4;
                    if (m.gameObject == null)
                    {
                        print("Monster seen, distance " + d + " started at " + m.spawnTimestamp);
                        SpawnMonster(m);
                    }
                    else
                    {
                        m.gameObject.SetActive(true);  //make sure the monster is visible
                    }                        
                    continue;
                }

                if (MathG.Distance(m.location, playerLocation) < monsterHearDistance)
                {
                    m.lastHeardTimestamp = now;
                    var footsteps = CalculateFootstepRange(d);
                    m.footstepRange = footsteps;
                    print("Monster heard, footsteps " + footsteps );                    
                }

                //hide monsters that can't be seen 
                if(m.gameObject != null)
                {
                    m.gameObject.SetActive(false);
                }
            }              
        }

        private int CalculateFootstepRange(float distance)
        {
            if (distance < oneStepRange) return 1;
            if (distance < twoStepRange) return 2;
            if (distance < threeStepRange) return 3;
            return 4;
        }

        private Vector3 ConvertToWorldSpace(float longitude, float latitude)
        {
            // Check if GPS service has valid map data
            if (gpsLocationService == null || 
                gpsLocationService.mapWorldCenter == null || 
                gpsLocationService.mapScale == null)
            {
                // Simple conversion for testing - place monsters near player with small random offset
                var randomX = Random.Range(-10f, 10f);
                var randomZ = Random.Range(-10f, 10f);
                return new Vector3(randomX, 0, randomZ);
            }
            
            // Convert GPS coordinates to world position
            var lonX = GoogleMapUtils.LonToX(longitude);
            var latY = GoogleMapUtils.LatToY(latitude);
            
            //convert GPS lat/long to world x/y 
            var x = ((lonX - gpsLocationService.mapWorldCenter.x) * gpsLocationService.mapScale.x);
            var y = ((latY - gpsLocationService.mapWorldCenter.y) * gpsLocationService.mapScale.y);
            
            var position = new Vector3(-x, 0, y);
            
            // Validate the position to prevent Invalid AABB errors
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) ||
                Mathf.Abs(position.x) > 10000f || Mathf.Abs(position.z) > 10000f)
            {
                print("ERROR: Invalid position generated from GPS coords, using fallback");
                print($"Position was: {position}");
                var randomX = Random.Range(-10f, 10f);
                var randomZ = Random.Range(-10f, 10f);
                position = new Vector3(randomX, 0, randomZ);
            }
            
            // Clamp position to reasonable bounds to prevent extreme values
            position.x = Mathf.Clamp(position.x, -1000f, 1000f);
            position.z = Mathf.Clamp(position.z, -1000f, 1000f);
            position.y = 0f; // Always keep on ground level
            
            return position;
        }

        private void SpawnMonster(Monster monster)
        {
            var lon = monster.location.Longitude;
            var lat = monster.location.Latitude;
            var position = ConvertToWorldSpace(lon, lat);
            var rotation = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up);
            monster.gameObject = (GameObject)Instantiate(monsterPrefab, position, rotation);
        }
    }
}
