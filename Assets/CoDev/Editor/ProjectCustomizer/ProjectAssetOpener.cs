using UnityEditor;
using UnityEngine;
using System.IO;

namespace SuperHorizonStudios.CoDev
{
    public static class ProjectAssetOpener
    {
        public static void OpenFolderFromGUID(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Directory.Exists(path))
            {
                EditorUtility.FocusProjectWindow();
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(path);
                Selection.activeObject = folder;
            }
            else if (File.Exists(path))
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogWarning($"Path does not exist: {path}");
            }
        }
    }
}
