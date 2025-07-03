/// <summary>
/// OpenAI Realtime API Voice Configuration
/// Contains all supported voice types for the OpenAI Realtime API
/// </summary>
public enum OpenAIVoice
{
    /// <summary>Balanced, warm voice</summary>
    alloy,
    
    /// <summary>Expressive, dynamic voice</summary>
    ash,
    
    /// <summary>Pleasant, conversational voice</summary>
    ballad,
    
    /// <summary>Energetic, upbeat voice</summary>
    coral,
    
    /// <summary>Deep, resonant voice</summary>
    echo,
    
    /// <summary>Wise, thoughtful voice</summary>
    sage,
    
    /// <summary>Soft, gentle voice</summary>
    shimmer,
    
    /// <summary>Poetic, expressive voice</summary>
    verse
}

/// <summary>
/// Static utility class for OpenAI Voice operations
/// </summary>
public static class OpenAIVoiceExtensions
{
    /// <summary>
    /// Get a human-readable description of the voice with gender indication
    /// </summary>
    public static string GetDescription(this OpenAIVoice voice)
    {
        return voice switch
        {
            OpenAIVoice.alloy => "Alloy (neutral): Balanced, warm voice",
            OpenAIVoice.ash => "Ash (male): Expressive, dynamic voice", 
            OpenAIVoice.ballad => "Ballad (female): Pleasant, conversational voice",
            OpenAIVoice.coral => "Coral (female): Energetic, upbeat voice",
            OpenAIVoice.echo => "Echo (male): Deep, resonant voice",
            OpenAIVoice.sage => "Sage (female): Wise, thoughtful voice",
            OpenAIVoice.shimmer => "Shimmer (female): Soft, gentle voice",
            OpenAIVoice.verse => "Verse (male): Confident, clear voice",
            _ => "Unknown voice"
        };
    }
    
    /// <summary>
    /// Get the default voice (alloy)
    /// </summary>
    public static OpenAIVoice GetDefault() => OpenAIVoice.alloy;
    
    /// <summary>
    /// Validate if a voice value is supported by the API
    /// </summary>
    public static bool IsValid(OpenAIVoice voice)
    {
        return System.Enum.IsDefined(typeof(OpenAIVoice), voice);
    }
    
    /// <summary>
    /// Get all available voice names as string array
    /// </summary>
    public static string[] GetAllVoiceNames()
    {
        return System.Enum.GetNames(typeof(OpenAIVoice));
    }
    
    /// <summary>
    /// Get all voice descriptions for UI display
    /// </summary>
    public static string[] GetAllVoiceDescriptions()
    {
        var allVoices = System.Enum.GetValues(typeof(OpenAIVoice));
        var descriptions = new string[allVoices.Length];
        
        for (int i = 0; i < allVoices.Length; i++)
        {
            var voice = (OpenAIVoice)allVoices.GetValue(i);
            descriptions[i] = voice.GetDescription();
        }
        
        return descriptions;
    }
    
    /// <summary>
    /// Try to parse a voice name string to OpenAIVoice enum
    /// </summary>
    public static bool TryParse(string voiceName, out OpenAIVoice voice)
    {
        return System.Enum.TryParse(voiceName, true, out voice) && IsValid(voice);
    }
}
