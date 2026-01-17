using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuperHorizonStudios.CoDev
{
    public class SessionManager
    {
        public static void SaveSession(string sessionName, List<ChatMessage> messages)
        {
            string path = Path.Combine(Application.persistentDataPath, $"{sessionName}.json");
            File.WriteAllText(path, JsonUtility.ToJson(new SessionData { messages = messages }));
        }

        public static List<string> GetSavedSessions()
        {
            return Directory.GetFiles(Application.persistentDataPath, "*.json")
                            .Select(Path.GetFileNameWithoutExtension).ToList();
        }

        public static SessionData LoadSession(string sessionName)
            {
                string path = Path.Combine(Application.persistentDataPath, $"{sessionName}.json");
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"Session file not found at path: {path}");
                    return new SessionData { messages = new List<ChatMessage>() };
                }

                return JsonUtility.FromJson<SessionData>(File.ReadAllText(path));
            }
        
        
    }


}
