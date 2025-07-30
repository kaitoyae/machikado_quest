using UnityEngine;
using packt.FoodyGO.Mapping;

namespace packt.FoodyGO.Services
{
    [System.Serializable]
    public class SimulationRoute
    {
        public string routeName;
        public MapLocation[] waypoints;
        public float speed = 0.0001f;
        public bool loop = true;
    }
}