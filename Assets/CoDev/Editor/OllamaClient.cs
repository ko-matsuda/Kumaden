using System;
using System.Text;
using System.Net;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;


namespace SuperHorizonStudios.CoDev
{
    public static class OllamaClient
    {
        private class OllamaResponseData
        {
            public string message;
        }

        public static void GetOllamaResponse(string userInput, string model, Action<string> callback)
        {
            string endpoint = EditorPrefs.GetString("CoDev_Ollama_URL", "http://localhost:11434/api/chat");

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = userInput }
                }
            };

            string json = JsonUtility.ToJson(new Wrapper { model = model, messages = new[] { new Message { role = "user", content = userInput } } });

            var request = WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/json";

            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.GetRequestStream().Write(bodyRaw, 0, bodyRaw.Length);

            request.BeginGetResponse(ar =>
            {
                try
                {
                    var response = request.EndGetResponse(ar);
                    using var reader = new StreamReader(response.GetResponseStream());
                    string responseText = reader.ReadToEnd();

                    // Some Ollama implementations return a streaming response; you may need to parse last chunk
                    string content = ExtractContentFromResponse(responseText);

                    callback?.Invoke(content);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ollama API error: {ex.Message}");
                    callback?.Invoke("Error calling Ollama API.");
                }
            }, null);
        }

        private static string ExtractContentFromResponse(string raw)
{
    try
    {
        var lines = raw.Split('\n').Reverse().ToList();
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("{"))
            {
                var parsed = JsonUtility.FromJson<OllamaStreamMessage>(line.Trim());
                if (parsed != null && parsed.message != null && !string.IsNullOrEmpty(parsed.message.content))
                    return parsed.message.content;
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"Failed to parse Ollama response: {ex.Message}");
    }

    return raw;
}

        [Serializable]
        private class Wrapper
        {
            public string model;
            public Message[] messages;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class OllamaStreamMessage
        {
            public Message message;
        }
    }
}
