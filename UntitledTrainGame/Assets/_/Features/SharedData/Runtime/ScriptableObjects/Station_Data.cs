using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
#endif

namespace SharedData.Runtime
{
    [CreateAssetMenu(fileName = "Station_Data", menuName = "Scriptable Objects/Station_Data")]
    public class Station_Data : ScriptableObject
    {
        public StationPrefix LinePrefix;
        public int Id;
        
        // public string StationScene;
        public SceneReference StationScene;
        
        public string DisplayName;
        // [CanBeNull] public string Description;
        // [CanBeNull] public Sprite Icon;
        public bool IsDiscovered;
        
        public string GetStationName() => $"{LinePrefix.ToString()}{Id}";
        
        #if UNITY_EDITOR

        private void OnValidate()
        {
            Assert.IsTrue(StationScene == null || !string.IsNullOrEmpty(StationScene.SceneName), $"{name}: StationScene is assigned but has no SceneName");
            
            var stationGuids = AssetDatabase.FindAssets($"t:{typeof(Station_Data)}");
            foreach (var guid in stationGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var station = AssetDatabase.LoadAssetAtPath<Station_Data>(path);
                if (station == null || station == this) continue;
                
                if (station.StationScene != null)
                    Assert.IsTrue(station.StationScene != StationScene, $"Duplicate StationScene found: {name} and {station.name} both reference {StationScene?.name}");
            }
        }
       
        #endif
    }
    
    #if UNITY_EDITOR

    [CustomEditor(typeof(Station_Data))]
    public class Station_DataEditor : Editor
    {
        SerializedProperty _linePrefix;
        SerializedProperty _id;
        SerializedProperty _stationScene;
        SerializedProperty _displayName;
        SerializedProperty _isDiscovered;

        private SceneReference[] _allSceneRefs = new SceneReference[0];
        private Station_Data[] _allStations = new Station_Data[0];
        private SceneReference[] _unassignedSceneRefs = new SceneReference[0];
        private string[] _unassignedSceneNames = new string[0];

        public static event Action SceneRefsUpdated;
        public static void NotifySceneRefsUpdated() => SceneRefsUpdated?.Invoke();
        
        private void OnEnable()
        {
            _linePrefix = serializedObject.FindProperty(nameof(Station_Data.LinePrefix));
            _id = serializedObject.FindProperty(nameof(Station_Data.Id));
            _stationScene = serializedObject.FindProperty(nameof(Station_Data.StationScene));
            _displayName = serializedObject.FindProperty(nameof(Station_Data.DisplayName));
            _isDiscovered = serializedObject.FindProperty(nameof(Station_Data.IsDiscovered));

            SceneRefsUpdated += RefreshLists;
            RefreshLists();
        }

        private void OnDisable() => SceneRefsUpdated -= RefreshLists;

        private void RefreshLists()
        {
            // Load all SceneReference assets
            var sceneRefGuids = AssetDatabase.FindAssets($"t:{typeof(SceneReference)}");
            _allSceneRefs = sceneRefGuids
                .Select(guid => AssetDatabase.LoadAssetAtPath<SceneReference>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(sceneRef => sceneRef != null)
                .OrderBy(sceneRef => sceneRef.name)
                .ToArray();
            
            // Assert that all SceneReference assets are unique
            Assert.IsTrue(_allSceneRefs.Distinct().Count() == _allSceneRefs.Length, "Duplicate SceneReference assets found");
            
            // Load all Station_Data assets
            var stationGuids = AssetDatabase.FindAssets($"t:{typeof(Station_Data)}");
            _allStations = stationGuids
                .Select(guid => AssetDatabase.LoadAssetAtPath<Station_Data>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(station => station != null)
                .ToArray();
            
            var assigned = new HashSet<SceneReference>();
            var stationTarget = (Station_Data)target;
            foreach( var st in _allStations )
            {
                if(st == stationTarget) continue;
                if(st.StationScene != null) assigned.Add(st.StationScene);
            }
            
            //Build unassigned list
            var unassignedList = new List<SceneReference>();
            foreach (var sr in _allSceneRefs)
            {
                if(!assigned.Contains(sr) || sr == stationTarget.StationScene)
                    unassignedList.Add(sr);
            }
            
             _unassignedSceneRefs = unassignedList.ToArray();
             _unassignedSceneNames = _unassignedSceneRefs.Select(x => x.SceneName).ToArray();
             
             Assert.IsNotNull(_unassignedSceneRefs, "Unassigned scene refs list should never be null after RefreshLists()");

             RepaintInspectorWindows();
        }

        private void RepaintInspectorWindows()
        {
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Name == "InspectorWindow"))
                window.Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_linePrefix);
            EditorGUILayout.PropertyField(_id);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Station Scene", EditorStyles.boldLabel);

            if (_unassignedSceneRefs.Length == 0)
            {
                EditorGUILayout.HelpBox("No missing scenes found.", MessageType.Info);
                EditorGUILayout.PropertyField(_stationScene);
            }
            else
            {
                var options = new string[_unassignedSceneRefs.Length + 1];
                options[0] = "<none>";
                
                for(int i = 0; i < _unassignedSceneNames.Length; i++)
                    options[i + 1] = _unassignedSceneNames[i];

                int currentPopup = 0;
                if (_stationScene.objectReferenceValue != null)
                {
                    var currentSceneRef = (SceneReference)_stationScene.objectReferenceValue;
                    int idx = Array.IndexOf(_unassignedSceneRefs, currentSceneRef);
                    if (idx >= 0) currentPopup = idx + 1;
                }
                
                int newPopup = EditorGUILayout.Popup("Assign SceneReference", currentPopup, options);
                if (newPopup != currentPopup)
                {
                    if(newPopup == 0)
                        _stationScene.objectReferenceValue = null;
                    else
                        _stationScene.objectReferenceValue = _unassignedSceneRefs[newPopup - 1];
                    
                    EditorUtility.SetDirty(target);
                }
                
                if(GUILayout.Button("Refresh Scenes"))
                    RefreshLists();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_isDiscovered);
            
            serializedObject.ApplyModifiedProperties();
        }
    }

    public static class SceneReferenceGenerator
    {
        [MenuItem("Assets/Create/SceneReferences/Generate Missing")]
        public static void GenerateMissing()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:SceneAsset");
            var allSceneNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var guid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                allSceneNames.Add(name);
            }

            var existingSceneRefs = AssetDatabase.FindAssets($"t:{typeof(SceneReference)}")
                .Select(g => AssetDatabase.LoadAssetAtPath<SceneReference>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(sr => sr != null)
                .Select(sr => sr.SceneName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var created = new List<string>();
            foreach (var scene in allSceneNames)
            {
                if (existingSceneRefs.Contains(scene)) continue;
                
                var asset = ScriptableObject.CreateInstance<SceneReference>();
                asset.SceneName = scene;
                
                var sceneAssetGuid = AssetDatabase.FindAssets($"t:SceneAsset {scene}").FirstOrDefault();
                if (!string.IsNullOrEmpty(sceneAssetGuid))
                {
                    var sceneAssetPath = AssetDatabase.GUIDToAssetPath(sceneAssetGuid);
                    asset.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneAssetPath);
                }
                
                Assert.IsNotNull(asset, $"Failed to create SceneReference for scene {scene}");

                var dir = "Assets/_/Database/ScriptableObjects/SceneReferences";
                if(!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                var path = $"{dir}/{scene}.asset";
                
                AssetDatabase.CreateAsset(asset, path);
                created.Add(scene);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Station_DataEditor.NotifySceneRefsUpdated();
            
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>().Where(w => w.GetType().Name == "InspectorWindow"))
                window.Repaint();
            
            if(created.Count == 0) Debug.Log("No missing SceneReferences found. All scenes already referenced.");
            else Debug.Log($"Created {created.Count} missing SceneReferences: {string.Join(", ", created)}");
        }
    }
    #endif
}
