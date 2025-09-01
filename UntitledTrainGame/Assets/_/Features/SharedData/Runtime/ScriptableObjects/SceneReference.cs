using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedData.Runtime
{
    public class SceneReference : ScriptableObject
    {
        [SerializeField] public string SceneName;
        
        #if UNITY_EDITOR
        [SerializeField] public SceneAsset SceneAsset;

        private static List<SceneAsset> _missingSceneAssets = new List<SceneAsset>();

        private void OnValidate()
        {
            if (SceneAsset != null) SceneName = SceneAsset.name;

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

            RefreshMissingScenes();
        }

        private static void RefreshMissingScenes()
        {
            _missingSceneAssets.Clear();

            var sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");
            var sceneReferenceGuids = AssetDatabase.FindAssets("t:SceneReference");

            HashSet<string> referencedSceneNames = new HashSet<string>();
            foreach (var refGuid in sceneReferenceGuids)
            {
                var refPath = AssetDatabase.GUIDToAssetPath(refGuid);
                var sceneRef = AssetDatabase.LoadAssetAtPath<SceneReference>(refPath);
                if (sceneRef != null && sceneRef.SceneAsset != null)
                {
                    referencedSceneNames.Add(sceneRef.SceneAsset.name);
                }
            }

            foreach (var sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null && !referencedSceneNames.Contains(sceneAsset.name))
                {
                    _missingSceneAssets.Add(sceneAsset);
                }
            }
        }

        [CustomEditor(typeof(SceneReference))]
        private class SceneReferenceEditor : Editor
        {
            private int _selectedIndex = -1;

            public override void OnInspectorGUI()
            {
                var sceneReference = (SceneReference)target;

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("SceneAsset"));

                if (sceneReference.SceneAsset == null)
                {
                    if (_missingSceneAssets.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No missing scenes found.", MessageType.Info);
                    }
                    else
                    {
                        string[] options = new string[_missingSceneAssets.Count];
                        for (int i = 0; i < _missingSceneAssets.Count; i++)
                        {
                            options[i] = _missingSceneAssets[i].name;
                        }

                        _selectedIndex = EditorGUILayout.Popup("Assign Missing Scene", _selectedIndex, options);
                        if (_selectedIndex >= 0 && _selectedIndex < _missingSceneAssets.Count)
                        {
                            Undo.RecordObject(sceneReference, "Assign Scene Asset");
                            sceneReference.SceneAsset = _missingSceneAssets[_selectedIndex];
                            sceneReference.SceneName = sceneReference.SceneAsset.name;
                            EditorUtility.SetDirty(sceneReference);
                            _selectedIndex = -1;
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
        #endif
    }
}
