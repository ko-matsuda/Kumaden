using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuperHorizonStudios.CoDev
{
    public class ApiKeyHelpWindow : EditorWindow
    {
        [MenuItem("Tools/API Key Help")]
        public static void ShowWindow()
        {
            var window = GetWindow<ApiKeyHelpWindow>("API Key Help");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            var root = rootVisualElement;
            root.Clear();

            var titleLabel = new Label("API Key Generation Help");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 16;
            titleLabel.style.marginBottom = 10;
            root.Add(titleLabel);

            AddProviderLink(root, "OpenAI (Chat, DALL·E)", "https://platform.openai.com/account/api-keys");
            AddProviderLink(root, "Stability AI (SDXL)", "https://platform.stability.ai/account/keys");
            AddProviderLink(root, "DeepSeek (Chat)", "https://platform.deepseek.com/api-keys");
            AddProviderLink(root, "Google Gemini", "https://makersuite.google.com/app/apikey");
            AddProviderLink(root, "Anthropic (Claude)", "https://console.anthropic.com/settings/keys");

            var footer = new Label("Click any link to open in browser.");
            footer.style.marginTop = 15;
            footer.style.fontSize = 11;
            footer.style.color = Color.gray;
            root.Add(footer);
        }

        private void AddProviderLink(VisualElement root, string name, string url)
        {
            var btn = new Button(() => Application.OpenURL(url))
            {
                text = $"🔗 {name}",
                style =
                {
                    marginTop = 4,
                    marginBottom = 4,
                    fontSize = 13
                }
            };
            root.Add(btn);
        }
    }
}
