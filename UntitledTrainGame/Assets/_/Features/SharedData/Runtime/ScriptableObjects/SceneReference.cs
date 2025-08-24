using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "SceneReference", menuName = "Game/SceneReference", order = 1)]
    public class SceneReference : ScriptableObject
    {
        [SerializeField] private string _sceneName;
        public string SceneName => _sceneName;
        
        #if UNITY_EDITOR
        [SerializeField] SceneAsset _sceneAsset;

        private void OnValidate()
        {
            if (_sceneAsset != null) _sceneName = _sceneAsset.name;
            
            var guids = AssetDatabase.FindAssets($"t:{typeof(SceneAsset)}");
            var seen = new HashSet<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (asset == null) continue;

                var key = (asset.name ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(key)) continue;
                
                if(!seen.Add(key) && asset != this)
                    Debug.LogWarning($"Duplicate scene name found: {key}");
            }
        }
        #endif
    }
}
