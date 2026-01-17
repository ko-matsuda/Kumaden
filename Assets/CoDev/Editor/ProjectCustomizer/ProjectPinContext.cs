using UnityEditor;
using UnityEngine;

namespace SuperHorizonStudios.CoDev
{
    public static class ProjectPinMenuItems
    {
        [MenuItem("Assets/📌 Pin Asset", true)]
        private static bool ValidatePinAsset()
        {
            string path = GetSelectedAssetPath();
            return !string.IsNullOrEmpty(path) && !ProjectPinManager.IsPinned(AssetDatabase.AssetPathToGUID(path), "Pinned");
        }

        [MenuItem("Assets/📌 Pin Asset")]
        private static void PinAsset()
        {
            string path = GetSelectedAssetPath();
            ProjectPinManager.AddPin(AssetDatabase.AssetPathToGUID(path), "Pinned");
        }

        [MenuItem("Assets/➕ Add to Quick Access", true)]
        private static bool ValidateQuickAccess()
        {
            string path = GetSelectedAssetPath();
            return !string.IsNullOrEmpty(path) && !ProjectPinManager.IsPinned(AssetDatabase.AssetPathToGUID(path), "QuickAccess");
        }

        [MenuItem("Assets/➕ Add to Quick Access")]
        private static void AddToQuickAccess()
        {
            string path = GetSelectedAssetPath();
            ProjectPinManager.AddPin(AssetDatabase.AssetPathToGUID(path), "QuickAccess");
        }

        private static string GetSelectedAssetPath()
        {
            Object selected = Selection.activeObject;
            return selected != null ? AssetDatabase.GetAssetPath(selected) : null;
        }
    }
}
