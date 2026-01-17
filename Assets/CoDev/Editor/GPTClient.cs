using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SuperHorizonStudios.CoDev
{
    public static class GPTClient
    {
        [Serializable]
        private class Message
        {
            public string role;
            public string content;
            public Message(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        [Serializable]
        private class ChatRequest
        {
            public string model;
            public List<Message> messages;
            public float temperature = 0.7f;
        }

        [Serializable]
        private class ChatResponse
        {
            public List<Choice> choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        public static async Task<string> SendMessage(string apiKey, string model, string userMessage)
        {
            var requestData = new ChatRequest
            {
                model = model, // Accepts any GPT model name string (e.g., gpt-3.5-turbo, gpt-4, gpt-4o)
                messages = new List<Message> { new Message("user", userMessage) }
            };

            string json = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"GPT Request Failed: {request.error}");
                return $"[Error] {request.error}";
            }

            try
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);

                if (response?.choices != null && response.choices.Count > 0 && response.choices[0].message != null)
                    return response.choices[0].message.content.Trim();

                return "[Error] Empty or malformed response from GPT.";
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse GPT response: {e.Message}");
                return "[Error] Failed to parse response.";
            }
        }
    }
}
