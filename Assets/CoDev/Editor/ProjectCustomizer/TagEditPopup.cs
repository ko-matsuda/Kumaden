using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using SuperHorizonStudios.CoDev;

namespace SuperHorizonStudios.CoDev
{
    public class TagEditPopup : EditorWindow
    {
        private static ProjectPinManager.PinnedItem currentItem;
        private string tagInput = "";

        public static void Show(ProjectPinManager.PinnedItem item)
        {
            currentItem = item;
            TagEditPopup window = CreateInstance<TagEditPopup>();
            window.titleContent = new GUIContent("Edit Tags");
            window.position = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), new Vector2(250, 100));
            window.tagInput = string.Join(", ", currentItem.tags ?? new System.Collections.Generic.List<string>());
            window.ShowPopup();
        }

        private void OnGUI()
        {

            GUILayout.Label("Edit Tags (comma-separated):", EditorStyles.boldLabel);
            tagInput = EditorGUILayout.TextField(tagInput);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save"))
            {
                currentItem.tags = tagInput.Split(',')
                                           .Select(t => t.Trim())
                                           .Where(t => !string.IsNullOrEmpty(t))
                                           .ToList();
                ProjectPinManager.SavePins();
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
