using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuperHorizonStudios.CoDev
{
    public class CoDevWindow : EditorWindow
    {
        private TextField inputField;
        private Button sendButton;
        private ScrollView chatContent;

        private VisualElement chatContainer;
        private VisualElement sessionsContainer;
        private VisualElement settingsContainer;
        private VisualElement errorsContainer;
        private VisualElement imageContainer;
        private ScrollView generatedImagesContainer;
        private PopupField<string> imageModelDropdown;
        private PopupField<string> imageSizeDropdown;
        private string selectedImageModel;
        private string selectedImageSize;
        private TextField imagePromptField;
        private Button generateImageButton;
        private Image generatedImagePreview;
        


        private ScrollView errorList;
        private Button sendErrorToAIButton;

        private TextField sessionNameField;
        private Button saveSessionBtn;
        private Button loadSessionBtn;
        private Button newSessionBtn;
        private ScrollView sessionList;

        
        private List<ChatMessage> sessionMessages = new();
        private string activeSessionName = "";
        private string selectedModel = "Gemini";

        [MenuItem("Tools/Co-Dev")]
        public static void OpenWindow()
        {
            var window = GetWindow<CoDevWindow>("Co-Dev");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
{
    var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CoDev/Editor/UI/CoDevWindow.uxml");
    var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/CoDev/Editor/UI/CoDevStyles.uss");

    var root = visualTree.CloneTree();
    root.styleSheets.Add(styleSheet);
    rootVisualElement.Clear();
    rootVisualElement.Add(root);

    // Tab containers
    chatContainer = root.Q<VisualElement>("chatContainer");
    sessionsContainer = root.Q<VisualElement>("sessionsContainer");
    settingsContainer = root.Q<VisualElement>("settingsContainer");
    errorsContainer = root.Q<VisualElement>("errorsContainer");

    // Tab switching
    root.Q<Button>("chatTab").clicked += () => ShowTab(chatContainer);
    root.Q<Button>("sessionsTab").clicked += () => ShowTab(sessionsContainer);
    root.Q<Button>("settingsTab").clicked += () => ShowTab(settingsContainer);
    root.Q<Button>("errorsTab").clicked += () => ShowTab(errorsContainer);

    // Chat tab
    inputField = root.Q<TextField>("inputField");
    sendButton = root.Q<Button>("sendButton");
    chatContent = root.Q<ScrollView>("chatContent");
    sendButton.clicked += OnSendClicked;

    // Sessions tab
    sessionNameField = root.Q<TextField>("sessionNameField");
    saveSessionBtn = root.Q<Button>("saveSessionBtn");
    loadSessionBtn = root.Q<Button>("loadSessionBtn");
    newSessionBtn = root.Q<Button>("newSessionBtn");
    sessionList = root.Q<ScrollView>("sessionList");

    if (saveSessionBtn != null) saveSessionBtn.clicked += SaveCurrentSession;
    if (loadSessionBtn != null) loadSessionBtn.clicked += () => LoadSessionByName(sessionNameField.value);
    if (newSessionBtn != null) newSessionBtn.clicked += StartNewChat;

    // Errors tab
    errorList = root.Q<ScrollView>("errorList");
    sendErrorToAIButton = root.Q<Button>("sendErrorToAIButton");
    if (sendErrorToAIButton != null) sendErrorToAIButton.clicked += OnSendErrorsToAI;

    // image tab
    
    imageContainer = root.Q<VisualElement>("imageContainer");
    generatedImagesContainer = root.Q<ScrollView>("generatedImagesContainer");
    root.Q<Button>("imageTab").clicked += () => ShowTab(imageContainer);
    List<string> imageModels = new List<string> { "DALL·E-2", "DALL·E-3", "StableDiffusion" };
    selectedImageModel = EditorPrefs.GetString("CoDev_SelectedImageModel", "DALL·E-3");

    imageModelDropdown = new PopupField<string>("Image Model", imageModels, selectedImageModel);
    imageModelDropdown.RegisterValueChangedCallback(evt =>
    {
        selectedImageModel = evt.newValue;
        EditorPrefs.SetString("CoDev_SelectedImageModel", selectedImageModel);
    });

    List<string> imageSizes = new List<string> { "256x256", "512x512", "1024x1024" };
    selectedImageSize = EditorPrefs.GetString("CoDev_SelectedImageSize", "1024x1024");

    imageSizeDropdown = new PopupField<string>("Image Size", imageSizes, selectedImageSize);
    imageSizeDropdown.RegisterValueChangedCallback(evt =>
    {
        selectedImageSize = evt.newValue;
        EditorPrefs.SetString("CoDev_SelectedImageSize", selectedImageSize);
    });

    imagePromptField = root.Q<TextField>("imagePromptField");
    generateImageButton = root.Q<Button>("generateImageButton");
    generatedImagePreview = root.Q<Image>("generatedImagePreview");
    
    imageContainer.Add(imageModelDropdown);
    imageContainer.Add(imageSizeDropdown);

    if (generateImageButton != null)
        generateImageButton.clicked += OnGenerateImageClicked;

    // Model selection
    selectedModel = EditorPrefs.GetString("CoDev_SelectedModel", "Gemini");


    var settingsScrollView = settingsContainer.Q<ScrollView>("settingsScrollView");
    if (settingsScrollView != null)
    {
        InjectApiKeyUI(settingsScrollView);
    }
    else
    {
        InjectApiKeyUI(settingsContainer); // fallback
    }

    Application.logMessageReceived += HandleUnityError;

    ShowTab(chatContainer);
    LoadSessionsToUI();
}

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleUnityError;
        }

        private void ShowTab(VisualElement tabToShow)
{
    foreach (var container in new[] { chatContainer, sessionsContainer, settingsContainer, errorsContainer, imageContainer })
        container.style.display = container == tabToShow ? DisplayStyle.Flex : DisplayStyle.None;
}

        private void OnSendClicked()
        {
            string input = inputField.value.Trim();
            if (string.IsNullOrEmpty(input)) return;

            AddMessageToChat(input, isUser: true);
            inputField.value = "";

            switch (selectedModel)
            {
                case "Gemini":
                    string geminiKey = EditorPrefs.GetString("CoDev_API_Key", "");
                    if (string.IsNullOrEmpty(geminiKey))
                    {
                        AddMessageToChat("Gemini API Key not set. Go to the Settings tab to enter it.", false);
                        return;
                    }

                    GeminiClient.GetGeminiResponse(geminiKey, input, response =>
                    {
                        EditorApplication.delayCall += () => AddMessageToChat(response, isUser: false);
                    });
                    break;

                case "Ollama":
                    OllamaClient.GetOllamaResponse(input, "llama3", response =>
                    {
                        EditorApplication.delayCall += () => AddMessageToChat(response, isUser: false);
                    });
                    break;

                case "GPT":
                    string gptKey = EditorPrefs.GetString("CoDev_OpenAI_Key", "");
                    string gptModel = EditorPrefs.GetString("CoDev_GPT_Model", "gpt-3.5-turbo");

                    if (string.IsNullOrEmpty(gptKey))
                    {
                        AddMessageToChat("OpenAI API Key not set. Go to the Settings tab to enter it.", false);
                        return;
                    }

                    _ = GPTClient.SendMessage(gptKey, gptModel, input).ContinueWith(task =>
                    {
                        string response = task.Result;
                        EditorApplication.delayCall += () => AddMessageToChat(response, false);
                    });
                    break;

                case "Claude":
                    string claudeKey = EditorPrefs.GetString("CoDev_Claude_Key", "");
                    if (string.IsNullOrEmpty(claudeKey))
                    {
                        AddMessageToChat("Claude API Key not set. Go to the Settings tab to enter it.", false);
                        return;
                    }

                    ClaudeClient.GetClaudeResponse(claudeKey, input, response =>
                    {
                        EditorApplication.delayCall += () => AddMessageToChat(response, false);
                    });
                    break;

                case "DeepSeek":
                    string deepSeekKey = EditorPrefs.GetString("CoDev_DeepSeek_Key", "");
                    if (string.IsNullOrEmpty(deepSeekKey))
                    {
                        AddMessageToChat("DeepSeek API Key not set. Go to the Settings tab to enter it.", false);
                        return;
                    }

                    DeepSeekClient.GetDeepSeekResponse(deepSeekKey, input, response =>
                    {
                        EditorApplication.delayCall += () => AddMessageToChat(response, false);
                    });
                    break;

                default:
                    AddMessageToChat("Selected model not supported.", false);
                    break;
            }
        }


        private void AddMessageToChat(string message, bool isUser)
        {
            //string timestamp = DateTime.Now.ToString("HH:mm:ss");
            sessionMessages.Add(new ChatMessage { content = message, isUser = isUser });

            AppendRestoredMessage(new ChatMessage { content = message, isUser = isUser});

            chatContent.schedule.Execute(() =>
            {
                chatContent.scrollOffset = new Vector2(0, chatContent.contentContainer.layout.height);
            }).ExecuteLater(100);
        }

        private void AppendRestoredMessage(ChatMessage msg)
        {
            if (msg.content.Contains("```"))
            {
                string[] parts = msg.content.Split(new[] { "```" }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i % 2 == 0 && !string.IsNullOrWhiteSpace(parts[i]))
                        AppendTextMessage(parts[i].Trim(), msg.isUser, msg.timestamp);
                    else
                        AppendCodeBlock(parts[i].Trim(), msg.timestamp);
                }
            }
            else
            {
                AppendTextMessage(msg.content, msg.isUser, msg.timestamp);
            }
        }

        private void AppendTextMessage(string text, bool isUser, string timestamp)
        {
            var label = new Label($" {text}");
            label.AddToClassList(isUser ? "user-message" : "ai-message");
            chatContent.Add(label);
        }

        private void AppendCodeBlock(string code, string timestamp)
        {
            var codeContainer = new VisualElement();
            codeContainer.AddToClassList("code-block");

            var codeLabel = new Label(code);
            codeLabel.AddToClassList("code-text");

            var copyBtn = new Button(() => EditorGUIUtility.systemCopyBuffer = code) { text = "Copy" };
            copyBtn.AddToClassList("copy-button");

            codeContainer.Add(codeLabel);
            codeContainer.Add(copyBtn);

            chatContent.Add(codeContainer);
        }

        private void HandleUnityError(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;

            string errorMsg = $"[{DateTime.Now:HH:mm:ss}] {condition}\n{stackTrace}";
            AddMessageToChat(errorMsg, true);

            var errorLabel = new Label(errorMsg);
            errorLabel.AddToClassList("error-label");
            errorList.Add(errorLabel);
        }

private async void OnGenerateImageClicked()
{
    string prompt = imagePromptField?.value?.Trim();
    if (string.IsNullOrEmpty(prompt))
    {
        Debug.LogWarning("Image prompt is empty.");
        return;
    }

    Debug.Log($"[ImageGeneration] Prompt: {prompt}");
    Texture2D generatedImage = null;

    switch (selectedImageModel)
    {
        case "DALL·E-2":
        case "DALL·E-3":
            string openAIKey = EditorPrefs.GetString("CoDev_OpenAI_Key", "");
            if (string.IsNullOrEmpty(openAIKey))
            {
                Debug.LogWarning("OpenAI API key not set.");
                return;
            }

            string dalleModel = selectedImageModel == "DALL·E-2" ? "dall-e-2" : "dall-e-3";
            generatedImage = await ImageGenerationClient.GenerateImageWithOpenAI(openAIKey, prompt, selectedImageSize, dalleModel);
            break;

        case "StableDiffusion":
            string stabilityKey = EditorPrefs.GetString("CoDev_Stability_Key", "");
            if (string.IsNullOrEmpty(stabilityKey))
            {
                Debug.LogWarning("Stability API key not set.");
                return;
            }

            generatedImage = await ImageGenerationClient.GenerateImageWithStableDiffusion(stabilityKey, prompt);
            break;

        default:
            Debug.LogWarning("Unsupported image model.");
            return;
    }

    if (generatedImage != null)
    {
        if (generatedImagePreview != null)
            generatedImagePreview.image = generatedImage;
        else
            Debug.LogWarning("generatedImagePreview is null. Check UXML assignment.");

        AddImageResultToUI(generatedImage);
    }
}

private void AddImageResultToUI(Texture2D image)
{
    var imageEntry = new VisualElement();
    imageEntry.style.flexDirection = FlexDirection.Column;
    imageEntry.style.marginBottom = 10;

    var imageElement = new Image
    {
        image = image,
        scaleMode = ScaleMode.ScaleToFit
    };
    imageElement.style.width = 256;
    imageElement.style.height = 256;

    var saveButton = new Button(() =>
    {
        string fileName = $"generated_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        SaveTextureToDisk(image, fileName);
    })
    { text = "Save Image" };

    imageEntry.Add(imageElement);
    imageEntry.Add(saveButton);

    generatedImagesContainer.Add(imageEntry);
}

private void SaveTextureToDisk(Texture2D texture, string fileName)
{
    if (texture == null) return;

    byte[] pngData = texture.EncodeToPNG();
    if (pngData == null) return;

    string directory = Path.Combine(Application.dataPath, "CoDevGeneratedImages");
    if (!Directory.Exists(directory))
        Directory.CreateDirectory(directory);

    string fullPath = Path.Combine(directory, fileName);
    File.WriteAllBytes(fullPath, pngData);
    Debug.Log($"Image saved to: {fullPath}");

    AssetDatabase.Refresh();
}


        private void SaveCurrentSession()
        {
            string name = sessionNameField.value.Trim();
            if (string.IsNullOrEmpty(name)) return;

            SessionManager.SaveSession(name, sessionMessages);
            activeSessionName = name;
            LoadSessionsToUI();
        }

        private void LoadSessionByName(string sessionName)
        {
            SessionData data = SessionManager.LoadSession(sessionName);
            if (data == null || data.messages == null)
            {
                Debug.LogWarning($"Failed to load session '{sessionName}' or it's empty.");
                return;
            }

            sessionMessages = new List<ChatMessage>();
            chatContent.Clear();

            foreach (var msg in data.messages)
            {
                sessionMessages.Add(msg);
                AppendRestoredMessage(msg);
            }

            activeSessionName = sessionName;
            Debug.Log($"Loaded session: {sessionName}");
        }

        private void StartNewChat()
        {
            sessionMessages.Clear();
            chatContent.Clear();
            activeSessionName = "";
            Debug.Log("Started new chat session.");
        }

        private void LoadSessionsToUI()
        {
            sessionList.Clear();
            var names = SessionManager.GetSavedSessions();

            foreach (var name in names)
            {
                var card = new VisualElement();
                card.AddToClassList("session-card");

                var title = new Label(name);
                title.AddToClassList("session-title");

                var time = new Label("Saved: Unknown");
                time.AddToClassList("session-time");

                card.Add(title);
                card.Add(time);

                card.RegisterCallback<MouseUpEvent>(_ =>
                {
                    sessionNameField.value = name;
                    LoadSessionByName(name);
                });

                sessionList.Add(card);
            }
        }

        private void OnSendErrorsToAI()
        {
            string errors = string.Join("\n\n", errorList.Children().OfType<Label>().Select(label => label.text));
            AddMessageToChat(errors, isUser: true);

            if (selectedModel == "Gemini")
            {
                string apiKey = EditorPrefs.GetString("CoDev_API_Key", "");
                if (string.IsNullOrEmpty(apiKey))
                {
                    AddMessageToChat("Gemini API Key not set. Go to the Settings tab to enter it.", false);
                    return;
                }

                GeminiClient.GetGeminiResponse(apiKey, errors, response =>
                {
                    EditorApplication.delayCall += () => AddMessageToChat(response, false);
                });
            }
            else if (selectedModel == "Ollama")
            {
                OllamaClient.GetOllamaResponse(errors, "llama3", response =>
                {
                    EditorApplication.delayCall += () => AddMessageToChat(response, false);
                });
            }
            else if (selectedModel == "GPT")
            {
                string key = EditorPrefs.GetString("CoDev_OpenAI_Key", "");
                string model = EditorPrefs.GetString("CoDev_GPT_Model", "gpt-3.5-turbo");

                if (string.IsNullOrEmpty(key))
                {
                    AddMessageToChat("OpenAI API Key not set.", false);
                    return;
                }

                _ = GPTClient.SendMessage(key, model, errors).ContinueWith(task =>
                {
                    string response = task.Result;
                    EditorApplication.delayCall += () => AddMessageToChat(response, false);
                });
            }
            else if (selectedModel == "Claude")
            {
                string claudeKey = EditorPrefs.GetString("CoDev_Claude_Key", "");
                if (string.IsNullOrEmpty(claudeKey))
                {
                    AddMessageToChat("Claude API Key not set.", false);
                    return;
                }

                ClaudeClient.GetClaudeResponse(claudeKey, errors, response =>
                {
                    EditorApplication.delayCall += () => AddMessageToChat(response, false);
                });
            }
            else if (selectedModel == "DeepSeek")
            {
                string deepSeekKey = EditorPrefs.GetString("CoDev_DeepSeek_Key", "");
                if (string.IsNullOrEmpty(deepSeekKey))
                {
                    AddMessageToChat("DeepSeek API Key not set.", false);
                    return;
                }

                DeepSeekClient.GetDeepSeekResponse(deepSeekKey, errors, response =>
                {
                    EditorApplication.delayCall += () => AddMessageToChat(response, false);
                });
            }
        }


        private void InjectApiKeyUI(VisualElement container)
        {
            container.Clear();

            var apiKeySection = new VisualElement();
            apiKeySection.AddToClassList("settings-section");

            var title = new Label("API Key Setup");
            title.AddToClassList("settings-title");

            // Gemini
            var apiKeyField = new TextField("Gemini API Key:") { isPasswordField = true };
            apiKeyField.value = EditorPrefs.GetString("CoDev_API_Key", "");

            var geminiToggle = new Toggle("Show Gemini Key");
            geminiToggle.RegisterValueChangedCallback(evt =>
            {
                apiKeyField.isPasswordField = !evt.newValue;
                apiKeyField.MarkDirtyRepaint();
            });

            var saveGeminiBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(apiKeyField.value))
                {
                    EditorPrefs.SetString("CoDev_API_Key", apiKeyField.value);
                    Debug.Log("Gemini API Key saved.");
                }
            }) { text = "Save Gemini Key" };

            var deleteGeminiBtn = new Button(() =>
            {
                EditorPrefs.DeleteKey("CoDev_API_Key");
                apiKeyField.value = "";
                Debug.Log("Gemini API Key deleted.");
            }) { text = "Delete Gemini Key" };

            
    // OpenAI GPT
    var openAIField = new TextField("OpenAI API Key:") { isPasswordField = true };
    openAIField.value = EditorPrefs.GetString("CoDev_OpenAI_Key", "");

    var openAIToggle = new Toggle("Show GPT Key");
    openAIToggle.RegisterValueChangedCallback(evt =>
    {
        openAIField.isPasswordField = !evt.newValue;
        openAIField.MarkDirtyRepaint();
    });

    var openAISaveBtn = new Button(() =>
    {
        if (!string.IsNullOrEmpty(openAIField.value))
        {
            EditorPrefs.SetString("CoDev_OpenAI_Key", openAIField.value);
            Debug.Log("OpenAI API Key saved.");
        }
    }) { text = "Save GPT Key" };

    var deleteOpenAIBtn = new Button(() =>
    {
        EditorPrefs.DeleteKey("CoDev_OpenAI_Key");
        openAIField.value = "";
        Debug.Log("OpenAI API Key deleted.");
    }) { text = "Delete GPT Key" };

            //  GPT Model Dropdown
            var gptModels = new List<string> { "gpt-3.5-turbo", "gpt-4o" };
            string currentGPTModel = EditorPrefs.GetString("CoDev_GPT_Model", "gpt-3.5-turbo");
            if (!gptModels.Contains(currentGPTModel)) currentGPTModel = "gpt-3.5-turbo";

            var gptModelDropdown = new PopupField<string>("GPT Model", gptModels, currentGPTModel);
            gptModelDropdown.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetString("CoDev_GPT_Model", evt.newValue);
                Debug.Log($"GPT model set to {evt.newValue}");
            });


            // Ollama
            var ollamaUrlField = new TextField("Ollama URL:");
            ollamaUrlField.value = EditorPrefs.GetString("CoDev_Ollama_URL", "http://localhost:11434/api/chat");

            var ollamaSaveBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(ollamaUrlField.value))
                {
                    EditorPrefs.SetString("CoDev_Ollama_URL", ollamaUrlField.value);
                    Debug.Log("Ollama URL saved.");
                }
            }) { text = "Save Ollama URL" };

            var deleteOllamaBtn = new Button(() =>
            {
                EditorPrefs.DeleteKey("CoDev_Ollama_URL");
                ollamaUrlField.value = "";
                Debug.Log("Ollama URL deleted.");
            }) { text = "Delete Ollama URL" };

            // Claude
            var claudeField = new TextField("Claude API Key:") { isPasswordField = true };
            claudeField.value = EditorPrefs.GetString("CoDev_Claude_Key", "");

            var claudeToggle = new Toggle("Show Claude Key");
            claudeToggle.RegisterValueChangedCallback(evt =>
            {
                claudeField.isPasswordField = !evt.newValue;
                claudeField.MarkDirtyRepaint();
            });

            var claudeSaveBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(claudeField.value))
                {
                    EditorPrefs.SetString("CoDev_Claude_Key", claudeField.value);
                    Debug.Log("Claude API Key saved.");
                }
            }) { text = "Save Claude Key" };

            var deleteClaudeBtn = new Button(() =>
            {
                EditorPrefs.DeleteKey("CoDev_Claude_Key");
                claudeField.value = "";
                Debug.Log("Claude API Key deleted.");
            }) { text = "Delete Claude Key" };

            // DeepSeek
            var deepSeekField = new TextField("DeepSeek API Key:") { isPasswordField = true };
            deepSeekField.value = EditorPrefs.GetString("CoDev_DeepSeek_Key", "");

            var deepSeekToggle = new Toggle("Show DeepSeek Key");
            deepSeekToggle.RegisterValueChangedCallback(evt =>
            {
                deepSeekField.isPasswordField = !evt.newValue;
                deepSeekField.MarkDirtyRepaint();
            });

            var deepSeekSaveBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(deepSeekField.value))
                {
                    EditorPrefs.SetString("CoDev_DeepSeek_Key", deepSeekField.value);
                    Debug.Log("DeepSeek API Key saved.");
                }
            }) { text = "Save DeepSeek Key" };

            var deleteDeepSeekBtn = new Button(() =>
            {
                EditorPrefs.DeleteKey("CoDev_DeepSeek_Key");
                deepSeekField.value = "";
                Debug.Log("DeepSeek API Key deleted.");
            }) { text = "Delete DeepSeek Key" };

            // Stability AI
            var stabilityField = new TextField("Stability AI Key:") { isPasswordField = true };
            stabilityField.value = EditorPrefs.GetString("CoDev_Stability_Key", "");

            var stabilityToggle = new Toggle("Show Stability Key");
            stabilityToggle.RegisterValueChangedCallback(evt =>
            {
                stabilityField.isPasswordField = !evt.newValue;
                stabilityField.MarkDirtyRepaint();
            });

            var stabilitySaveBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(stabilityField.value))
                {
                    EditorPrefs.SetString("CoDev_Stability_Key", stabilityField.value);
                    Debug.Log("Stability AI Key saved.");
                }
            }) { text = "Save Stability Key" };

            var stabilityDeleteBtn = new Button(() =>
            {
                EditorPrefs.DeleteKey("CoDev_Stability_Key");
                stabilityField.value = "";
                Debug.Log("Stability AI Key deleted.");
            }) { text = "Delete Stability Key" };

            // Dropdown for model selection
            var availableModels = new List<string> { "Gemini", "Ollama", "GPT", "Claude", "DeepSeek" };
            if (!availableModels.Contains(selectedModel))
                selectedModel = "Gemini"; // fallback to a safe default

            var modelDropdown = new PopupField<string>("Model", availableModels, selectedModel);

            modelDropdown.RegisterValueChangedCallback(evt =>
            {
                selectedModel = evt.newValue;
                EditorPrefs.SetString("CoDev_SelectedModel", selectedModel);
            });

            // OpenAI Image (DALL·E 2 / 3)
            var openAIImageField = new TextField("OpenAI Image API Key:") { isPasswordField = true };
            openAIImageField.value = EditorPrefs.GetString("CoDev_OpenAI_Image_Key", "");

            var openAIImageToggle = new Toggle("Show DALL·E Key");
            openAIImageToggle.RegisterValueChangedCallback(evt =>
            {
                openAIImageField.isPasswordField = !evt.newValue;
                openAIImageField.MarkDirtyRepaint();
            });

            var openAIImageSaveBtn = new Button(() =>
            {
                if (!string.IsNullOrEmpty(openAIImageField.value))
                {
                    EditorPrefs.SetString("CoDev_OpenAI_Image_Key", openAIImageField.value);
                    Debug.Log("OpenAI Image API Key saved.");
                }
            }) { text = "Save DALL·E Key" };

            var openAIImageDeleteBtn = new Button(() =>
            {
                EditorPrefs.DeleteKey("CoDev_OpenAI_Image_Key");
                openAIImageField.value = "";
                Debug.Log("OpenAI Image API Key deleted.");
            }) { text = "Delete DALL·E Key" };

            // Add all fields to UI
            apiKeySection.Add(title);

            apiKeySection.Add(modelDropdown);
            container.Add(apiKeySection);

            apiKeySection.Add(apiKeyField);
            apiKeySection.Add(geminiToggle);
            apiKeySection.Add(saveGeminiBtn);
            apiKeySection.Add(deleteGeminiBtn);

            apiKeySection.Add(openAIField);
            apiKeySection.Add(gptModelDropdown);
            apiKeySection.Add(openAIToggle);
            apiKeySection.Add(openAISaveBtn);
            apiKeySection.Add(deleteOpenAIBtn);
            

            apiKeySection.Add(ollamaUrlField);
            apiKeySection.Add(ollamaSaveBtn);
            apiKeySection.Add(deleteOllamaBtn);

            apiKeySection.Add(claudeField);
            apiKeySection.Add(claudeToggle);
            apiKeySection.Add(claudeSaveBtn);
            apiKeySection.Add(deleteClaudeBtn);

            apiKeySection.Add(deepSeekField);
            apiKeySection.Add(deepSeekToggle);
            apiKeySection.Add(deepSeekSaveBtn);
            apiKeySection.Add(deleteDeepSeekBtn);

            apiKeySection.Add(stabilityField);
            apiKeySection.Add(stabilityToggle);
            apiKeySection.Add(stabilitySaveBtn);
            apiKeySection.Add(stabilityDeleteBtn);

            apiKeySection.Add(openAIImageField);
        apiKeySection.Add(openAIImageToggle);
        apiKeySection.Add(openAIImageSaveBtn);
        apiKeySection.Add(openAIImageDeleteBtn);

            

            var importantTextLabel = new Label("IMPORTANT NOTES ABOUT THE MODELS\n" +
                "Chat-GPT: Requires paid OpenAI API key.\n" +
                "Ollama: Requires local installation.\n" +
                "Gemini: Free from Google Cloud.\n" +
                "Claude: Use Anthropic-compatible endpoint.\n" +
                "DeepSeek: High-speed transformer model API.")
            {
                name = "importantTextLabel"
            };
            importantTextLabel.AddToClassList("important-text");
            apiKeySection.Add(importantTextLabel);
        }
    }

    [Serializable]
    public class ChatMessage
    {
        public string content;
        public bool isUser;
        public string timestamp;
    }

    [Serializable]
    public class SessionData
    {
        public List<ChatMessage> messages;
    }
}
