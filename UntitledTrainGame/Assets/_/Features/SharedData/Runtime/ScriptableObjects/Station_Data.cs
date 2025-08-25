using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "Station_Data", menuName = "Scriptable Objects/Station_Data")]
    public class Station_Data : ScriptableObject
    {
        public StationPrefix LinePrefix;
        public int Id;
        public string StationScene;
        public string DisplayName;
        // [CanBeNull] public string Description;
        [CanBeNull] public Sprite Icon;
        public bool IsDiscovered;
        
        public string GetStationName() => $"{LinePrefix.ToString()}{Id}";
    }
}
