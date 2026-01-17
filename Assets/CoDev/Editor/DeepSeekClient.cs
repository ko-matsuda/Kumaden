using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SuperHorizonStudios.CoDev
{
    public static class DeepSeekClient
    {
        private const string apiUrl = "https://api.deepseek.com/v1/chat/completions";
        private static UnityWebRequest currentRequest;
        private static Action<string> pendingCallback;
        private static bool isRequestActive = false;

        public static void GetDeepSeekResponse(string apiKey, string prompt, Action<string> onResponse)
        {
            if (isRequestActive)
            {
                Debug.LogWarning("DeepSeek request already in progress.");
                return;
            }

            // Manual JSON construction (JsonUtility can't handle arrays properly)
            string jsonBody = $@"
{{
  ""model"": ""deepseek-chat"",
  ""messages"": [
    {{
      ""role"": ""user"",
      ""content"": ""{EscapeJson(prompt)}""
    }}
  ],
  ""temperature"": 0.7
}}";

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            currentRequest = new UnityWebRequest(apiUrl, "POST");
            currentRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            currentRequest.downloadHandler = new DownloadHandlerBuffer();

            currentRequest.SetRequestHeader("Content-Type", "application/json");
            currentRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

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
                string parsed = ParseDeepSeekResponse(result);
                pendingCallback?.Invoke(parsed);
            }
            else
            {
                string error = $"Error: {currentRequest.responseCode} - {currentRequest.error}\n{currentRequest.downloadHandler.text}";
                pendingCallback?.Invoke(error);
            }

            currentRequest.Dispose();
            currentRequest = null;
            pendingCallback = null;
        }

        private static string ParseDeepSeekResponse(string json)
        {
            try
            {
                var parsed = JsonUtility.FromJson<DeepSeekResponseWrapper>(json);
                return parsed.choices[0].message.content.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse DeepSeek response: " + ex.Message);
                return "Failed to parse DeepSeek response.";
            }
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        [Serializable]
        private class DeepSeekResponseWrapper
        {
            public List<Choice> choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }
    }
}
