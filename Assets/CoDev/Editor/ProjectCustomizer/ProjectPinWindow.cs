using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SuperHorizonStudios.CoDev
{
    public class ProjectPinWindow : EditorWindow
    {
        private Vector2 scroll;
        private string tagSearch = "";

        [MenuItem("Tools/Project Folder Pinner")]
        public static void ShowWindow()
        {
            GetWindow<ProjectPinWindow>("Project Folder Pinner");
        }

        private void OnGUI()
        {
        

            GUILayout.Space(5);
            EditorGUILayout.LabelField("🔍 Filter by Tag", EditorStyles.boldLabel);
            tagSearch = EditorGUILayout.TextField(tagSearch);
            EditorGUILayout.Space();

            GUIStyle folderLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white } 
            };
            EditorGUILayout.LabelField("📁 Pinned Folders", folderLabelStyle);
            EditorGUILayout.Space();

            List<ProjectPinManager.PinnedItem> filteredPinnedItems = ProjectPinManager.GetPinnedItems()
                .Where(p => p.category == "Pinned")
                .Where(p => AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(p.guid)))
                .Where(p => string.IsNullOrEmpty(tagSearch) || (p.tags != null && p.tags.Any(tag => tag.ToLower().Contains(tagSearch.ToLower()))))
                .ToList();

            var groupedByTag = filteredPinnedItems
                .GroupBy(p => (p.tags != null && p.tags.Count > 0) ? p.tags[0] : "Untagged")
                .OrderBy(g => g.Key);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var group in groupedByTag)
            {
                EditorGUILayout.LabelField("🏷️ " + group.Key, EditorStyles.boldLabel);
                GUILayout.Space(2);

                foreach (var item in group)
                {
                    string guid = item.guid;
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string folderName = Path.GetFileName(path);

                    EditorGUILayout.BeginHorizontal(GUI.skin.box);

                    GUIContent folderIcon = new GUIContent("📁 " + folderName);

                    if (GUILayout.Button(folderIcon, GUILayout.Height(25)))
                    {
                        ProjectAssetOpener.OpenFolderFromGUID(guid);
                    }

                    if (GUILayout.Button("🏷️", GUILayout.Width(30)))
                    {
                        TagEditPopup.Show(item);
                    }

                    GUIContent unpinIcon = new GUIContent("❌", "Unpin Folder");

                    if (GUILayout.Button(unpinIcon, GUILayout.Width(30)))
                    {
                        ProjectPinManager.RemovePin(guid);
                        Repaint();
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }

                GUILayout.Space(6);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            DrawDropArea();
        }

        private void DrawDropArea()
        {
            GUILayout.Space(10);
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "📥 Drag Folders or Assets Here to Pin");

            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            if (!string.IsNullOrEmpty(path))
                            {
                                string guid = AssetDatabase.AssetPathToGUID(path);
                                ProjectPinManager.AddPin(guid, "QuickAccess");
                            }
                        }
                    }

                    Event.current.Use();
                }
            }
        }
    }
}
