using UnityEngine;

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "TrainRoute_Data", menuName = "Scriptable Objects/TrainRoute_Data")]
    public class TrainRoute_Data : ScriptableObject
    {
        public Station_Data StartStation;
        public Station_Data EndStation;
        [Range(0, 1)] public float CompressionFactor;
        public bool IsExpress;
        public StationNetwork_Data Network;
    }
}
