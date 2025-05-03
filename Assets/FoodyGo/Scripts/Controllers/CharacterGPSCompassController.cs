using UnityEngine;
using StarterAssets;
using packt.FoodyGO.Mapping;
using packt.FoodyGO.Services;

namespace packt.FoodyGO.Controllers
{
    public class CharacterGPSCompassController : MonoBehaviour
    {
        public GPSLocationService gpsLocationService;
        private double lastTimestamp;        
        private ThirdPersonController thirdPersonController;
        private Vector3 target;
        
        // Use this for initialization
        void Start()
        {
            Input.compass.enabled = true;
            thirdPersonController = GetComponent<ThirdPersonController>();
            if (gpsLocationService != null)
            {
                gpsLocationService.OnMapRedraw += GpsLocationService_OnMapRedraw;
            }
        }

        private void GpsLocationService_OnMapRedraw(GameObject g)
        {
            transform.position = Vector3.zero;
            target = Vector3.zero;
        }

        // Update is called once per frame
        void Update()
        {
            if (gpsLocationService != null &&
                gpsLocationService.IsServiceStarted &&
                gpsLocationService.PlayerTimestamp > lastTimestamp)
            {
                //convert GPS lat/long to world x/y 
                var x = ((GoogleMapUtils.LonToX(gpsLocationService.Longitude)
                    - gpsLocationService.mapWorldCenter.x) * gpsLocationService.mapScale.x);
                var y = (GoogleMapUtils.LatToY(gpsLocationService.Latitude)
                    - gpsLocationService.mapWorldCenter.y) * gpsLocationService.mapScale.y;
                target = new Vector3(-x, 0, y);
            }

            //check if the character has reached the new point
            if (Vector3.Distance(target, transform.position) > .025f)
            {
                var move = target - transform.position;
                thirdPersonController.Move();
            }
            else
            {
                //stop moving
                thirdPersonController.Move();

                // Orient an object to point to magnetic north and adjust for map reversal
                var heading = 180 + Input.compass.magneticHeading;
                var rotation = Quaternion.AngleAxis(heading, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.fixedTime * .001f);
            }
        }
    }
}
