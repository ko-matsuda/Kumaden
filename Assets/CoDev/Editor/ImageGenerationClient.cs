using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace SuperHorizonStudios.CoDev
{
    public static class ImageGenerationClient
    {

        public static async Task<Texture2D> GenerateImageWithOpenAI(string apiKey, string prompt, string size, string model)
        {
            string json = JsonUtility.ToJson(new
            {
                model = model.ToLower(),           // "dall-e-2" or "dall-e-3"
                prompt = prompt,
                size = size,                       // "256x256", "512x512", "1024x1024"
                response_format = "url"
            });

            using var request = new UnityWebRequest("https://api.openai.com/v1/images/generations", "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"OpenAI image generation failed: {request.error}");
                return null;
            }

            var resultJson = JsonUtility.FromJson<OpenAIImageResponse>(request.downloadHandler.text);
            return await DownloadImageFromURL(resultJson.data[0].url);
        }

        public static async Task<Texture2D> GenerateImageWithOpenAI(string apiKey, string prompt)
        {
            return await GenerateImageWithOpenAI(apiKey, prompt, "1024x1024", "dall-e-3");
        }

public static async Task<Texture2D> GenerateImageWithStableDiffusion(string apiKey, string prompt)
{
    string json = JsonUtility.ToJson(new StabilityRequest
    {
        text_prompts = new[] { new TextPrompt { text = prompt } },
        cfg_scale = 7,
        clip_guidance_preset = "FAST_BLUE",  // Optional but recommended
        height = 1024,
        width = 1024,
        samples = 1,
        steps = 30
    });

    using var request = new UnityWebRequest(
        "https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image", 
        "POST"
    );

    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");
    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

    await request.SendWebRequest();

    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError($"Stability AI generation failed: {(int)request.responseCode} - {request.downloadHandler.text}");
        return null;
    }

    var resultJson = JsonUtility.FromJson<StabilityAIResponse>(request.downloadHandler.text);
    string base64Image = resultJson.artifacts[0].base64;
    return DecodeBase64Image(base64Image);
}

// Utility to escape quotes, backslashes etc.
private static string EscapeJson(string str)
{
    return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

        private static async Task<Texture2D> DownloadImageFromURL(string imageUrl)
        {
            using var www = UnityWebRequestTexture.GetTexture(imageUrl);
            await www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to download image: " + www.error);
                return null;
            }
            return DownloadHandlerTexture.GetContent(www);
        }

        private static Texture2D DecodeBase64Image(string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            return tex;
        }

        [Serializable]
        private class OpenAIImageResponse
        {
            public ImageData[] data;
        }

        [Serializable]
        private class ImageData
        {
            public string url;
        }

        [Serializable]
        private class StabilityRequest
        {
            public TextPrompt[] text_prompts;
            public float cfg_scale;
            public string clip_guidance_preset;
            public int height;
            public int width;
            public int samples;
            public int steps;
        }

        [Serializable]
        private class TextPrompt
        {
            public string text;
        }

        [Serializable]
        private class StabilityAIResponse
        {
            public StabilityImage[] artifacts;
        }

        [Serializable]
        private class StabilityImage
        {
            public string base64;
        }
    }
}
