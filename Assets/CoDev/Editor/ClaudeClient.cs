using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SuperHorizonStudios.CoDev
{
    public static class ClaudeClient
    {
        private const string apiUrl = "https://api.anthropic.com/v1/messages";
        private static UnityWebRequest currentRequest;
        private static Action<string> pendingCallback;
        private static bool isRequestActive = false;

        public static void GetClaudeResponse(string apiKey, string prompt, Action<string> onResponse)
        {
            if (isRequestActive)
            {
                Debug.LogWarning("Claude request already in progress.");
                return;
            }

            var requestData = new
            {
                model = "claude-3-opus-20240229",
                max_tokens = 1024,
                temperature = 0.7f,
                messages = new[] {
                    new { role = "user", content = prompt }
                }
            };

            string jsonBody = JsonUtility.ToJson(requestData);
            jsonBody = jsonBody.Replace("\"role\"", "\"role\"").Replace("\"content\"", "\"content\""); // JsonUtility fix

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            currentRequest = new UnityWebRequest(apiUrl, "POST");
            currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            currentRequest.downloadHandler = new DownloadHandlerBuffer();
            currentRequest.SetRequestHeader("Content-Type", "application/json");
            currentRequest.SetRequestHeader("x-api-key", apiKey);
            currentRequest.SetRequestHeader("anthropic-version", "2023-06-01");

            currentRequest.SendWebRequest();
            pendingCallback = onResponse;
            isRequestActive = true;

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

            isRequestActive = false;
            EditorApplication.update -= CheckRequestStatus;

            if (currentRequest.result == UnityWebRequest.Result.Success)
            {
                string result = currentRequest.downloadHandler.text;
                string parsed = ParseClaudeResponse(result);
                pendingCallback?.Invoke(parsed);
            }
            else
            {
                string error = $"Error: {currentRequest.responseCode} - {currentRequest.error}";
                pendingCallback?.Invoke(error);
            }

            currentRequest.Dispose();
            currentRequest = null;
            pendingCallback = null;
        }

        private static string ParseClaudeResponse(string json)
        {
            try
            {
                var parsed = JsonUtility.FromJson<ClaudeResponseWrapper>(json);
                return parsed.content[0].text.Trim();
            }
            catch
            {
                return "Failed to parse Claude response.";
            }
        }

        [Serializable]
        private class ClaudeResponseWrapper
        {
            public List<ClaudeContent> content;
        }

        [Serializable]
        private class ClaudeContent
        {
            public string text;
        }
    }
}
