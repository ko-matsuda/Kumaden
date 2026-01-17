using UnityEngine;
using UnityEditor;

namespace SuperHorizonStudios.CoDev
{
    public static class CoDevRuntimeErrorCache
    {
        private static string lastError = "";
        private static string lastStackTrace = "";

        public static void LogRuntimeError(string error, string stack)
        {
            lastError = error;
            lastStackTrace = stack;
        }

        public static string GetLatestError()
        {
            return lastError + "\n" + lastStackTrace;
        }

        public static bool HasError()
        {
            return !string.IsNullOrEmpty(lastError);
        }

        public static void Clear()
        {
            lastError = "";
            lastStackTrace = "";
        }
    }
}
