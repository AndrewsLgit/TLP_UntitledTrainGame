using UnityEngine;
using UnityEditor;
using System.IO;

namespace Foundation.Editor
{

    public static class CreateScriptWithTemplate
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        private const string TEMPLATE_PATH = "Assets/_/Features/Framework/_Foundation/Editor/Templates/MonoBehaviourTemplate.cs.txt";
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Main Methods

        [MenuItem("Assets/Create/MonoBehaviour Script", false, 80)]
        private static void CreateScriptWithRegions()
        {
            string folderPath = GetSelectedPath();
            string scriptPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, "NewScript.cs"));
            
            string templateText = File.ReadAllText(TEMPLATE_PATH);
            File.WriteAllText(scriptPath, templateText);
            AssetDatabase.Refresh();
         
            Object scriptAsset = AssetDatabase.LoadAssetAtPath<Object>(scriptPath);
            ProjectWindowUtil.ShowCreatedAsset(scriptAsset);
        }
        #endregion

        #region Helpers/Utils

        private static string GetSelectedPath()
        {
            string path = "Assets";
            Object obj = Selection.activeObject;
            
            if (obj != null)
            {
                path = AssetDatabase.GetAssetPath(obj);
                if(!string.IsNullOrEmpty(path) && File.Exists(path))
                    path = Path.GetDirectoryName(path);
            }
            return path;
        }
        #endregion
    }
}
