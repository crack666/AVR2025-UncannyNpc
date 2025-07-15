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
    [Tooltip("Select the voice for AI responses. Each voice has distinct personality characteristics.")]
    [SerializeField] private OpenAIVoice voice = OpenAIVoice.alloy;
    [SerializeField] private float temperature = 0.8f;
    
    [Header("Transcription Settings")]
    [Tooltip("Available models:\n• whisper-1: Faster, good quality (recommended)\n• whisper-large-v3: Highest quality, slower processing")]
    [SerializeField] private string transcriptionModel = "whisper-1";
    
    [Header("Voice Activity Detection (VAD)")]
    [Tooltip("VAD Type: 'server_vad' (recommended) or 'none' to disable turn detection")]
    [SerializeField] private string vadType = "server_vad";
    
    [Tooltip("Threshold: Higher values = less sensitive (0.0-1.0)")]
    [SerializeField] [Range(0.0f, 1.0f)] private float vadThreshold = 0.5f;
    
    [Tooltip("Prefix Padding: Audio included before voice activity starts (ms)")]
    [SerializeField] [Range(0, 1000)] private int vadPrefixPaddingMs = 300;
    
    [Tooltip("Silence Duration: How long to wait before considering speech ended (ms)")]
    [SerializeField] [Range(100, 2000)] private int vadSilenceDurationMs = 200;
    
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
    
    // Voice settings
    public OpenAIVoice Voice => voice;
    public string VoiceName => voice.ToApiString(); // Für OpenAI API
    
    public float Temperature => temperature;
    public string SystemPrompt => systemPrompt;
    public bool EnableDebugLogging => enableDebugLogging;
    public bool LogAudioData => logAudioData;
    
    // Transcription settings
    public string TranscriptionModel => transcriptionModel;
    
    // VAD settings
    public string VadType => vadType;
    public float VadThreshold => vadThreshold;
    public int VadPrefixPaddingMs => vadPrefixPaddingMs;
    public int VadSilenceDurationMs => vadSilenceDurationMs;
    
    /// <summary>
    /// Validates the settings configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(apiKey) && 
               !string.IsNullOrEmpty(baseUrl) && 
               !string.IsNullOrEmpty(model) &&
               !string.IsNullOrEmpty(transcriptionModel) &&
               !string.IsNullOrEmpty(vadType) &&
               sampleRate > 0 &&
               audioChunkSizeMs > 0 &&
               vadThreshold >= 0.0f && vadThreshold <= 1.0f &&
               vadPrefixPaddingMs >= 0 &&
               vadSilenceDurationMs >= 100;
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
        
        // Validate VAD settings
        vadThreshold = Mathf.Clamp01(vadThreshold);
        vadPrefixPaddingMs = Mathf.Clamp(vadPrefixPaddingMs, 0, 1000);
        vadSilenceDurationMs = Mathf.Clamp(vadSilenceDurationMs, 100, 2000);
        
        // Validate transcription model
        if (!string.IsNullOrEmpty(transcriptionModel))
        {
            string[] validModels = { "whisper-1", "whisper-large-v3" };
            if (!System.Array.Exists(validModels, model => model == transcriptionModel))
            {
                transcriptionModel = "whisper-1"; // Reset to default
            }
        }
        
        // Validate VAD type
        if (!string.IsNullOrEmpty(vadType))
        {
            string[] validVadTypes = { "server_vad", "none" };
            if (!System.Array.Exists(validVadTypes, type => type == vadType))
            {
                vadType = "server_vad"; // Reset to default
            }
        }
    }
    #endif
}
