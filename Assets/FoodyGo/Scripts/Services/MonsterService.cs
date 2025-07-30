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
        // 10m ≈ 0.00009 degrees (1 degree ≈ 111km)
        public float latitudeSpawnOffset = .00009f;
        public float longitudeSpawnOffset = .00009f;

        [Header("Monster Visibility")]
        public float monsterHearDistance;
        public float monsterSeeDistance;
        public float monsterLifetimeSeconds = 30;
        public List<Monster> monsters;
        
        // Use this for initialization
        void Start()
        {
            monsters = new List<Monster>();
            print("MonsterService: Starting");

            StartCoroutine(CleanupMonsters());
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
                        print("Cleaning up monster");
                        monsters.Remove(list[i]);
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (gpsLocationService != null &&
                gpsLocationService.IsServiceStarted &&
                gpsLocationService.PlayerTimestamp > lastTimestamp)
            {
                lastTimestamp = gpsLocationService.PlayerTimestamp;
                print($"MonsterService: Checking monsters at {gpsLocationService.Latitude}, {gpsLocationService.Longitude}");

                //update the monsters around the player
                CheckMonsters();
            }
        }

        private void CheckMonsters()
        {
            var randomValue = Random.value;
            print($"CheckMonsters: Random value={randomValue}, spawnRate={monsterSpawnRate}, will spawn={(randomValue < monsterSpawnRate)}");
            
            if (randomValue < monsterSpawnRate)
            {
                var mlat = gpsLocationService.Latitude + Random.Range(-latitudeSpawnOffset, latitudeSpawnOffset);
                var mlon = gpsLocationService.Longitude + Random.Range(-longitudeSpawnOffset, longitudeSpawnOffset);
                var monster = new Monster
                {
                    location = new MapLocation(mlon, mlat),
                    spawnTimestamp = gpsLocationService.PlayerTimestamp
                };
                monsters.Add(monster);
                print($"MonsterService: Created monster at lat: {mlat}, lon: {mlon}, total monsters: {monsters.Count}");
            }
            else
            {
                print($"CheckMonsters: No monster spawned this frame. Total monsters: {monsters.Count}");
            }

            //store players location for easy access in distance calculations
            var playerLocation = new MapLocation(gpsLocationService.Longitude, gpsLocationService.Latitude);
            //get the current Epoch time in seconds
            var now = Epoch.Now;

            foreach (Monster m in monsters)
            {
                var d = MathG.Distance(m.location, playerLocation);
                print($"Monster distance check: {d}m (see: {monsterSeeDistance}m, hear: {monsterHearDistance}m)");
                
                // Calculate footstep range (1-3 based on distance)
                if (d < 50f) m.footstepRange = 1;
                else if (d < 100f) m.footstepRange = 2;
                else if (d < 150f) m.footstepRange = 3;
                else m.footstepRange = 4;
                
                if (d < monsterSeeDistance)
                {
                    m.lastSeenTimestamp = now;
                    if (m.gameObject == null) 
                    {
                        print($"Spawning monster at distance {d}m");
                        SpawnMonster(m);
                    }
                    
                    print("Monster seen, distance " + d + " started at " + m.spawnTimestamp);
                    continue;
                }

                if (d < monsterHearDistance)
                {
                    m.lastHeardTimestamp = now;
                    print("Monster heard, distance " + d + " started at " + m.spawnTimestamp);
                    continue;
                }

                //hide monsters that can't be seen or heard

            }               
        }

        private Vector3 ConvertToWorldSpace(float longitude, float latitude)
        {
            //convert GPS lat/long to world x/y relative to player
            var playerLon = gpsLocationService.Longitude;
            var playerLat = gpsLocationService.Latitude;
            
            var deltaLon = longitude - playerLon;
            var deltaLat = latitude - playerLat;
            
            // Convert GPS differences to Unity world units
            // Using approximate conversion: 1 degree ≈ 111,000 meters
            var metersPerDegree = 111000f;
            var x = deltaLon * metersPerDegree;
            var z = deltaLat * metersPerDegree;
            
            print($"Monster GPS: lon={longitude:F6}, lat={latitude:F6}");
            print($"Player GPS: lon={playerLon:F6}, lat={playerLat:F6}");
            print($"Delta: dLon={deltaLon:F6}, dLat={deltaLat:F6}");
            print($"World position: x={x:F2}, z={z:F2}");
            
            return new Vector3(x, 1f, z);
        }

        private void SpawnMonster(Monster monster)
        {
            var lon = monster.location.Longitude;
            var lat = monster.location.Latitude;
            var position = ConvertToWorldSpace(lon, lat);
            monster.gameObject = (GameObject)Instantiate(monsterPrefab, position, Quaternion.identity);
            
            // MonsterControllerコンポーネントを追加
            var monsterController = monster.gameObject.GetComponent<packt.FoodyGO.Controllers.MonsterController>();
            if (monsterController == null)
            {
                monsterController = monster.gameObject.AddComponent<packt.FoodyGO.Controllers.MonsterController>();
                Debug.Log($"SpawnMonster: Added MonsterController to {monster.gameObject.name}");
            }
            
            // Configure Rigidbody for grounded monsters
            var rb = monster.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearDamping = 5f;  // Add drag to prevent sliding
                rb.angularDamping = 5f;
                rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                
                // Add downward force to ensure landing
                rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
            }
            
            // Ensure collider is properly configured
            var collider = monster.gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;  // Make sure it's not a trigger
            }
            
            print($"SpawnMonster: Spawned at world position {position}");
        }
    }
}
