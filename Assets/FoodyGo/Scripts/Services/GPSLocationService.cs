using packt.FoodyGo.Utils;
using packt.FoodyGO.Mapping;
using System.Collections;
using UnityEngine;

namespace packt.FoodyGO.Services
{
    [AddComponentMenu("Services/GPSLocationService")]
    public class GPSLocationService : MonoBehaviour
    {
        //Redraw Event
        public delegate void OnRedrawEvent(GameObject g);
        public event OnRedrawEvent OnMapRedraw;
        
        // Map redraw control
        private float lastMapRedrawTime = 0f;
        private MapLocation lastMapCenter = new MapLocation(0, 0);
        private float minDistanceForRedraw = 0.001f;
        [Header("GPS Accuracy")]
        public float DesiredAccuracyInMeters = 10f;
        public float UpdateAccuracyInMeters = 10f;

        [Header("Map Tile Parameters")]
        public int MapTileScale = 1;
        public int MapTileSizePixels = 640;
        public int MapTileZoomLevel = 17;

        [Header("GPS Simulation Settings")]
        public bool Simulating;
        public MapLocation StartCoordinates;        
        public float Rate = 1f;
        public Vector2[] SimulationOffsets;
        private int simulationIndex;
        public bool RandomSimulation = false;

        [Header("Exposed for GPS Debugging Purposes Only")]
        public bool IsServiceStarted;
        public float Latitude;
        public float Longitude;
        public float Altitude;
        public float Accuracy;
        public double Timestamp;
        public double PlayerTimestamp;
        public MapLocation mapCenter;
        public MapEnvelope mapEnvelope;        
        public Vector3 mapWorldCenter;
        public Vector2 mapScale;
        

		//initialize the object
        void Start()
        {
            print("Starting GPSLocationService");

#if !UNITY_EDITOR
            StartCoroutine(StartService());
            Simulating = false;
#else            
            // Set default coordinates if not configured in Inspector
            if (StartCoordinates.Latitude == 0 && StartCoordinates.Longitude == 0)
            {
                StartCoordinates = new MapLocation(139.7671f, 35.6812f); // Tokyo Station (Lon, Lat)
                print("GPS: Using default Tokyo coordinates");
            }
            
            Simulating = true; // Enable simulation in Unity Editor
            StartCoroutine(StartSimulationService());
            Latitude = StartCoordinates.Latitude;
            Longitude = StartCoordinates.Longitude;
            Accuracy = 10;
            Timestamp = 0;
            CenterMap();
#endif
        }

        IEnumerator StartSimulationService()
        {
            while (Simulating)
            {
                IsServiceStarted = true;

                // Only increment index if SimulationOffsets array is valid
                if (SimulationOffsets != null && SimulationOffsets.Length > 0)
                {
                    if (simulationIndex++ >= SimulationOffsets.Length-1)
                    {
                        simulationIndex = 0;
                    }
                }

                // Check if SimulationOffsets array is valid
                if (SimulationOffsets != null && SimulationOffsets.Length > 0)
                {
                    Longitude += SimulationOffsets[simulationIndex].x;
                    Latitude += SimulationOffsets[simulationIndex].y;
                }
                else
                {
                    // Use small random movement if no offsets are defined
                    Longitude += Random.Range(-0.00001f, 0.00001f);
                    Latitude += Random.Range(-0.00001f, 0.00001f);
                }

                PlayerTimestamp = Epoch.Now;

                yield return new WaitForSeconds(Rate);
            }
            IsServiceStarted = false;
        }

		//StartService is a coroutine, to avoid blocking as the location service is started
        IEnumerator StartService()
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                print("location not enabled by user, exiting");
                yield break;
            }

            // Start service before querying location            
            Input.location.Start(DesiredAccuracyInMeters, UpdateAccuracyInMeters);

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                print("Timed out");
                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                print("Unable to determine device location.");
                yield break;
            }
            else
            {
                //make sure simulation is disbaled
                Simulating = false;
                print("GSPLocationService started");                
                // Access granted and location value could be retrieved
                print("Location initialized at: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
                CenterMap();
                IsServiceStarted = true;
            }

           
        }

		//called once per frame
        void Update()
        {
            if(Input.location.status == LocationServiceStatus.Running  && IsServiceStarted)
            {
                //updates the public values that can be consumed by other game objects
                Latitude = Input.location.lastData.latitude;
                Longitude = Input.location.lastData.longitude;
                Altitude = Input.location.lastData.altitude;
                Accuracy = Input.location.lastData.horizontalAccuracy;
                PlayerTimestamp = Input.location.lastData.timestamp;
                MapLocation loc = new MapLocation(Input.location.lastData.longitude, Input.location.lastData.latitude);
                if (mapEnvelope.Contains(loc) == false)
                {
                    Timestamp = Input.location.lastData.timestamp;
                    CenterMap();
                }
            }
            else if (Simulating && IsServiceStarted)
            {                                
                MapLocation loc = new MapLocation(Longitude, Latitude);
                float timeSinceLastRedraw = Time.time - lastMapRedrawTime;
                float distanceFromLastCenter = (float)MathG.Distance(loc, lastMapCenter);
                
                // Only redraw if outside envelope AND enough time passed AND moved significant distance
                if (mapEnvelope.Contains(loc) == false && 
                    timeSinceLastRedraw > 3.0f && 
                    distanceFromLastCenter > minDistanceForRedraw)
                {
                    print(string.Format("Map redraw triggered: distance={0:F6}, time={1:F2}s", distanceFromLastCenter, timeSinceLastRedraw));
                    Timestamp = PlayerTimestamp;
                    lastMapRedrawTime = Time.time;
                    lastMapCenter = new MapLocation(Longitude, Latitude);
                    CenterMap();
                }
            }
        }

        public void MapRedrawn()
        {
            if(OnMapRedraw != null)
            {
                OnMapRedraw(this.gameObject);
            }
        }

        public void SwitchToSimulation(bool enable)
        {
            if (enable)
            {
                Simulating = true;
                IsServiceStarted = false;
                print("Switching to GPS simulation mode");
                StartCoroutine(StartSimulationService());
            }
            else
            {
                Simulating = false;
                IsServiceStarted = false;
                print("Switching to real GPS mode");
                StartCoroutine(StartService());
            }
        }

        private void CenterMap()
        {
            // Validate input parameters
            if (MapTileSizePixels <= 0) MapTileSizePixels = 640;
            if (MapTileScale <= 0) MapTileScale = 1;
            if (MapTileZoomLevel <= 0) MapTileZoomLevel = 17;
            
            mapCenter.Latitude = Latitude;
            mapCenter.Longitude = Longitude;
            mapWorldCenter.x = GoogleMapUtils.LonToX(mapCenter.Longitude);
            mapWorldCenter.y = GoogleMapUtils.LatToY(mapCenter.Latitude);

            print($"DEBUG: CenterMap params - Size:{MapTileSizePixels}, Scale:{MapTileScale}, Zoom:{MapTileZoomLevel}");
            print($"DEBUG: GPS coords for scale calc - Lat:{Latitude}, Lon:{Longitude}");

            // Fix coordinate system: X=longitude, Y=latitude
            mapScale.x = GoogleMapUtils.CalculateScaleY(Longitude, MapTileSizePixels, MapTileScale, MapTileZoomLevel);
            mapScale.y = GoogleMapUtils.CalculateScaleX(Latitude, MapTileSizePixels, MapTileScale, MapTileZoomLevel);
            
            // Add safety check for NaN values
            if (float.IsNaN(mapScale.x) || mapScale.x == 0)
            {
                print("WARNING: mapScale.x is invalid, using fallback value");
                mapScale.x = 0.001f; // Use smaller fallback for better positioning
            }
            if (float.IsNaN(mapScale.y) || mapScale.y == 0)
            {
                print("WARNING: mapScale.y is invalid, using fallback value");
                mapScale.y = 0.001f; // Use smaller fallback for better positioning
            }
            
            print($"DEBUG: MapScale calculated - X:{mapScale.x}, Y:{mapScale.y}");

            var lon1 = GoogleMapUtils.adjustLonByPixels(Longitude, -MapTileSizePixels/2, MapTileZoomLevel);
            var lat1 = GoogleMapUtils.adjustLatByPixels(Latitude, MapTileSizePixels/2, MapTileZoomLevel);

            var lon2 = GoogleMapUtils.adjustLonByPixels(Longitude, MapTileSizePixels/2, MapTileZoomLevel);
            var lat2 = GoogleMapUtils.adjustLatByPixels(Latitude, -MapTileSizePixels/2, MapTileZoomLevel);

            mapEnvelope = new MapEnvelope(lon1, lat1, lon2, lat2);
        }

        //called when the object is destroyed
        void OnDestroy()
        {
            if (IsServiceStarted)
                Input.location.Stop();
        }
    }
}
