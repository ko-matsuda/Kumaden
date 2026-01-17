using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SuperHorizonStudios.CoDev
{
    public static class GeminiClient
    {
        private const string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";
        private const string HistoryKey = "CoDev_Gemini_History";

        private static List<Part> conversationHistory = new List<Part>();
        private static UnityWebRequest currentRequest;
        private static Action<string> pendingCallback;
        private static bool isRequestActive = false;

        public static void GetGeminiResponse(string apiKey, string userInput, Action<string> onResponse)
        {
            if (isRequestActive)
            {
                Debug.LogWarning("Another request is already in progress. Please wait.");
                return;
            }

            if (conversationHistory.Count == 0)
                LoadHistory();

            conversationHistory.Add(new Part { text = userInput });

            var content = new Content { parts = new List<Part>(conversationHistory) };
            var requestBody = new RequestBody { contents = new List<Content> { content } };

            string jsonBody = JsonUtility.ToJson(requestBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            currentRequest = new UnityWebRequest(apiUrl + apiKey, "POST");
            currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            currentRequest.downloadHandler = new DownloadHandlerBuffer();
            currentRequest.SetRequestHeader("Content-Type", "application/json");
            
            currentRequest.SendWebRequest();
            pendingCallback = onResponse;
            isRequestActive = true;
            
            // Register for Editor updates to check the request status
            EditorApplication.update += CheckRequestStatus;
        }

        private static void CheckRequestStatus()
        {
            if (!isRequestActive || currentRequest == null)
            {
                EditorApplication.update -= CheckRequestStatus;
                return;
            }

            if (!currentRequest.isDone)
                return;

            // Request is done, process the result
            isRequestActive = false;
            EditorApplication.update -= CheckRequestStatus;

            if (currentRequest.result == UnityWebRequest.Result.Success)
            {
                string resultText = ParseGeminiResponse(currentRequest.downloadHandler.text);
                conversationHistory.Add(new Part { text = resultText });
                SaveHistory();
                pendingCallback?.Invoke(resultText);
            }
            else
            {
                string error = $"Error: {currentRequest.responseCode} - {currentRequest.error}\nResponse: {currentRequest.downloadHandler.text}";
                pendingCallback?.Invoke(error);
            }

            currentRequest.Dispose();
            currentRequest = null;
            pendingCallback = null;
        }

        public static void ResetHistory()
        {
            conversationHistory.Clear();
            EditorPrefs.DeleteKey(HistoryKey);
        }

        public static void SaveHistory()
        {
            var wrapper = new HistoryWrapper { parts = conversationHistory };
            string json = JsonUtility.ToJson(wrapper);
            EditorPrefs.SetString(HistoryKey, json);
        }

        public static void LoadHistory()
        {
            if (EditorPrefs.HasKey(HistoryKey))
            {
                string json = EditorPrefs.GetString(HistoryKey);
                try
                {
                    var wrapper = JsonUtility.FromJson<HistoryWrapper>(json);
                    conversationHistory = wrapper.parts ?? new List<Part>();
                }
                catch
                {
                    conversationHistory = new List<Part>();
                }
            }
        }

        private static string ParseGeminiResponse(string json)
        {
            try
            {
                var parsed = JsonUtility.FromJson<GeminiResponseWrapper>(json);
                return parsed.candidates[0].content.parts[0].text.Trim();
            }
            catch
            {
                return "Failed to parse Gemini response.";
            }
        }

        public static string GetLastErrorPrompt()
        {
            string lastError = EditorErrorLogger.GetLastError();
            return $"I encountered this error in Unity:\n\n{lastError}\n\nCan you help me understand and fix it?";
        }

        [Serializable]
        private class RequestBody
        {
            public List<Content> contents;
        }

        [Serializable]
        private class Content
        {
            public List<Part> parts;
        }

        [Serializable]
        private class Part
        {
            public string text;
        }

        [Serializable]
        private class GeminiResponseWrapper
        {
            public Candidate[] candidates;
        }

        [Serializable]
        private class Candidate
        {
            public Content content;
        }

        [Serializable]
        private class HistoryWrapper
        {
            public List<Part> parts;
        }
    }
}