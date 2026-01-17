using UnityEngine;
using UnityEditor;

namespace SuperHorizonStudios.CoDev
{
    [InitializeOnLoad]
    public static class ErrorCatcher
    {
        static ErrorCatcher()
        {
            Application.logMessageReceivedThreaded += HandleLog;
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                CoDevRuntimeErrorCache.LogRuntimeError(logString, stackTrace);
            }
        }
    }
}
