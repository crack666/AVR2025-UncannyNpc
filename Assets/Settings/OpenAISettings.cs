using UnityEngine;

/// <summary>
/// ScriptableObject für OpenAI API-Konfiguration
/// Speichert API-Keys und Verbindungseinstellungen sicher
/// </summary>
[CreateAssetMenu(fileName = "OpenAISettings", menuName = "OpenAI/Settings")]
public class OpenAISettings : ScriptableObject
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey = "";
    [SerializeField] private string baseUrl = "wss://api.openai.com/v1/realtime";
    [SerializeField] private string model = "gpt-4o-realtime-preview-2024-12-17";
    
    [Header("Audio Settings")]
    [SerializeField] private int sampleRate = 24000;
    [SerializeField] private int audioChunkSizeMs = 100;
    [SerializeField] private float microphoneVolume = 1.0f;
    
    [Header("Voice Settings")]
    [SerializeField] private int voiceIndex = 0; // Index statt Enum für bessere Kompatibilität
    [SerializeField] private float temperature = 0.8f;
    
    [Header("Conversation Settings")]
    [SerializeField] private string systemPrompt = "You are a helpful AI assistant.";
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool logAudioData = false;
    
    // Public Properties
    public string ApiKey => apiKey;
    public string BaseUrl => baseUrl;
    public string Model => model;
    public int SampleRate => sampleRate;
    public int AudioChunkSizeMs => audioChunkSizeMs;
    public float MicrophoneVolume => microphoneVolume;
    public int VoiceIndex 
    { 
        get 
        {
            // Validiere Voice-Index und setze auf Default falls ungültig
            if (voiceIndex < 0 || voiceIndex >= 8) // 8 verfügbare Voices (0-7)
            {
                Debug.LogWarning($"[OpenAISettings] Invalid voice index detected: {voiceIndex}. Resetting to 0 (alloy).");
                voiceIndex = 0; // alloy
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            return voiceIndex;
        }
        set
        {
            if (value >= 0 && value < 8) // 8 verfügbare Voices (0-7)
            {
                voiceIndex = value;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            else
            {
                Debug.LogWarning($"[OpenAISettings] Invalid voice index {value}. Keeping current value {voiceIndex}.");
            }
        }
    }
    
    public float Temperature => temperature;
    public string SystemPrompt => systemPrompt;
    public bool EnableDebugLogging => enableDebugLogging;
    public bool LogAudioData => logAudioData;
    
    /// <summary>
    /// Validates the settings configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(apiKey) && 
               !string.IsNullOrEmpty(baseUrl) && 
               !string.IsNullOrEmpty(model) &&
               sampleRate > 0 &&
               audioChunkSizeMs > 0;
    }
    
    /// <summary>
    /// Returns the WebSocket URL with model parameter
    /// </summary>
    public string GetWebSocketUrl()
    {
        return $"{baseUrl}?model={model}";
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Clamp values to reasonable ranges
        sampleRate = Mathf.Clamp(sampleRate, 8000, 48000);
        audioChunkSizeMs = Mathf.Clamp(audioChunkSizeMs, 20, 1000);
        microphoneVolume = Mathf.Clamp01(microphoneVolume);
        temperature = Mathf.Clamp01(temperature);
    }
    #endif
}
