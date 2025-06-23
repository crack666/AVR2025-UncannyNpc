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
    [SerializeField] private string voice = "alloy";
    [SerializeField] private float temperature = 0.8f;
    
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
    public string Voice => voice;
    public float Temperature => temperature;
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
