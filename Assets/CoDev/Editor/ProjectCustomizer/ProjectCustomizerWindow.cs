using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SuperHorizonStudios.CoDev
{
    public class ProjectCustomizerWindow : EditorWindow
    {
        private Vector2 scroll;
        private string searchFilter = "";

        [MenuItem("Tools/Project Customizer/Quick Access Panel")]
        public static void ShowWindow()
        {
            GetWindow<ProjectCustomizerWindow>("Quick Access Panel");
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("📌 Pinned Assets by Category", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            var grouped = ProjectPinManager.GetPinnedItems()
            .Where(p => p.category == "QuickAccess")
            .Where(p => string.IsNullOrEmpty(searchFilter) || AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(p.guid))?.name.ToLower().Contains(searchFilter.ToLower()) == true)
            .GroupBy(p => p.category)
            .OrderBy(g => g.Key);
            foreach (var categoryGroup in grouped)
            {
                EditorGUILayout.LabelField($"▶ {categoryGroup.Key}", EditorStyles.boldLabel);

                foreach (var item in categoryGroup)
                {
                    string path = AssetDatabase.GUIDToAssetPath(item.guid);
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);

                    if (asset != null)
                    {
                        Rect rect = EditorGUILayout.BeginHorizontal(GUI.skin.box);

                        GUIContent pinIcon = new GUIContent("📌 " + asset.name);

                        if (GUILayout.Button(pinIcon, GUILayout.Width(200)))
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }

                        GUIContent removeIcon = new GUIContent("❌", "Unpin Asset");

                        if (GUILayout.Button(removeIcon, GUILayout.Width(40)))
                        {
                            ProjectPinManager.RemovePin(item.guid);
                            Repaint();
                        }

                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(2);

                        if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                        {
                            ShowContextMenu(item);
                            Event.current.Use();
                        }
                    }
                }

                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ShowContextMenu(ProjectPinManager.PinnedItem item)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("❌ Unpin Asset"), false, () =>
            {
                ProjectPinManager.RemovePin(item.guid);
                Repaint();
            });

            menu.AddItem(new GUIContent("✏ Change Category"), false, () =>
            {
                ChangeCategoryPopup(item);
            });

            menu.AddItem(new GUIContent("Edit Tags"), false, () =>
            {
                TagEditPopup.Show(item);
            });

            menu.ShowAsContext();
        }

        private void ChangeCategoryPopup(ProjectPinManager.PinnedItem item)
        {
            CategoryEditPopup.Show(item);
        }
    }
}
