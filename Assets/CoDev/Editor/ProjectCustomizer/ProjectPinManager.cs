using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;


namespace SuperHorizonStudios.CoDev
{
    public static class ProjectPinManager
    {
        private const string SaveKey = "CoDev_ProjectPinnedItems";

        [System.Serializable]
        public class PinnedItem
        {
            public string guid;
            public string category;
            public bool isQuickAccess;
            public List<string> tags = new List<string>();

            public PinnedItem(string guid, string category, bool isQuickAccess = false)
            {
                this.guid = guid;
                this.category = category;
                this.isQuickAccess = isQuickAccess;
            }
        }

        private static List<PinnedItem> cachedPins;
        private static List<PinnedItem> pinnedItems = new List<PinnedItem>();

        static ProjectPinManager()
        {
            Load();
        }

        private static void Load()
        {
            string json = EditorPrefs.GetString(SaveKey, JsonUtility.ToJson(new Wrapper()));
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
            cachedPins = wrapper?.items ?? new List<PinnedItem>();
            pinnedItems = new List<PinnedItem>(cachedPins); // Sync pinnedItems
        }

        private static void Save()
        {
            Wrapper wrapper = new Wrapper { items = cachedPins };
            string json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(SaveKey, json);
        }

        public static void SavePins()
        {
            cachedPins = new List<PinnedItem>(pinnedItems); // Sync cachedPins
            Save();
        }

        public static bool IsPinned(string guid)
        {
            return pinnedItems.Any(item => item.guid == guid);
        }

        public static bool IsPinned(string guid, string category)
        {
            return pinnedItems.Any(item => item.guid == guid && item.category == category);
        }

        public static void AddPin(string guid, string category = "General", bool isQuickAccess = false)
        {
            if (!IsPinned(guid))
            {
                PinnedItem item = new PinnedItem(guid, category, isQuickAccess);
                pinnedItems.Add(item);
                SavePins();
            }
        }

        public static void RemovePin(string guid)
        {
            pinnedItems.RemoveAll(p => p.guid == guid);
            SavePins();
        }

        public static List<string> GetPinnedGUIDs()
        {
            return cachedPins.Select(p => p.guid).ToList();
        }

        public static List<PinnedItem> GetPinnedItems()
        {
            return new List<PinnedItem>(cachedPins);
        }

            private class Wrapper
        {
            public List<PinnedItem> items = new List<PinnedItem>();
        }

        [System.Serializable]
        private class PinnedDataWrapper
        {
            public List<PinnedItem> items;
        }
    }
}
