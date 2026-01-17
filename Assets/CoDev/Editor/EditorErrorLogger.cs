using UnityEditor;
using UnityEngine;

namespace SuperHorizonStudios.CoDev
{
    [InitializeOnLoad]
    public static class EditorErrorLogger
    {
        private const string LastErrorKey = "CoDev_LastUnityError";

        static EditorErrorLogger()
        {
            Application.logMessageReceived += HandleLog;
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                string error = $"{condition}\n{stackTrace}";
                EditorPrefs.SetString(LastErrorKey, error);
            }
        }

        public static string GetLastError()
        {
            return EditorPrefs.GetString(LastErrorKey, "No recent error found.");
        }

        public static void ClearLastError()
        {
            EditorPrefs.DeleteKey(LastErrorKey);
        }
    }
}
