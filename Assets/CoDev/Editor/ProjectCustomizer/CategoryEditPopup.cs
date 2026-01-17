using UnityEditor;
using UnityEngine;

namespace SuperHorizonStudios.CoDev
{
    public class CategoryEditPopup : EditorWindow
    {
        private string newCategory;
        private ProjectPinManager.PinnedItem item;

        public static void Show(ProjectPinManager.PinnedItem item)
        {
            CategoryEditPopup window = CreateInstance<CategoryEditPopup>();
            window.item = item;
            window.newCategory = item.category;
            window.titleContent = new GUIContent("Change Category");
            window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 300, 90);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Edit Category", EditorStyles.boldLabel);
            newCategory = EditorGUILayout.TextField("New Category", newCategory);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                var items = ProjectPinManager.GetPinnedItems();
                var target = items.Find(i => i.guid == item.guid);
                if (target != null)
                {
                    target.category = newCategory;
                    typeof(ProjectPinManager).GetMethod("Save", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.Invoke(null, new object[] { items });
                }

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
