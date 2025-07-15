/// <summary>
/// Extension methods für OpenAIVoice enum
/// </summary>
public static class OpenAIVoiceExtensions
{
    /// <summary>
    /// Konvertiert das Enum zu dem String, den die OpenAI API erwartet
    /// </summary>
    public static string ToApiString(this OpenAIVoice voice)
    {
        return voice switch
        {
            OpenAIVoice.alloy => "alloy",
            OpenAIVoice.ash => "ash",
            OpenAIVoice.ballad => "ballad",
            OpenAIVoice.coral => "coral",
            OpenAIVoice.echo => "echo",
            OpenAIVoice.sage => "sage",
            OpenAIVoice.shimmer => "shimmer",
            OpenAIVoice.verse => "verse",
            _ => "alloy"
        };
    }
    
    /// <summary>
    /// Konvertiert das Enum zu einem Integer-Index (entspricht dem Enum-Wert)
    /// </summary>
    public static int ToIndex(this OpenAIVoice voice)
    {
        return (int)voice;
    }
    
    /// <summary>
    /// Konvertiert einen Integer-Index zurück zu OpenAIVoice
    /// </summary>
    public static OpenAIVoice FromIndex(int index)
    {
        if (index >= 0 && index < GetVoiceCount())
        {
            return (OpenAIVoice)index;
        }
        
        UnityEngine.Debug.LogWarning($"[OpenAIVoiceExtensions] Invalid voice index {index}, using default alloy");
        return OpenAIVoice.alloy;
    }
    
    /// <summary>
    /// Gibt eine benutzerfreundliche Beschreibung der Stimme zurück
    /// </summary>
    public static string GetDescription(this OpenAIVoice voice)
    {
        return voice switch
        {
            OpenAIVoice.alloy => "Alloy (neutral): Balanced, warm voice",
            OpenAIVoice.ash => "Ash (neutral): Clear, articulate voice",
            OpenAIVoice.ballad => "Ballad (male): Deep, resonant voice",
            OpenAIVoice.coral => "Coral (female): Bright, friendly voice",
            OpenAIVoice.echo => "Echo (male): Confident, authoritative voice",
            OpenAIVoice.sage => "Sage (female): Calm, thoughtful voice",
            OpenAIVoice.shimmer => "Shimmer (female): Energetic, expressive voice",
            OpenAIVoice.verse => "Verse (female): Pleasant, conversational voice",
            _ => "Unknown voice"
        };
    }
    
    /// <summary>
    /// Gibt alle verfügbaren Voice-Beschreibungen für UI-Dropdowns zurück
    /// </summary>
    public static string[] GetAllVoiceDescriptions()
    {
        return new string[]
        {
            OpenAIVoice.alloy.GetDescription(),
            OpenAIVoice.ash.GetDescription(),
            OpenAIVoice.ballad.GetDescription(),
            OpenAIVoice.coral.GetDescription(),
            OpenAIVoice.echo.GetDescription(),
            OpenAIVoice.sage.GetDescription(),
            OpenAIVoice.shimmer.GetDescription(),
            OpenAIVoice.verse.GetDescription()
        };
    }
    
    /// <summary>
    /// Prüft, ob eine Voice gültig ist
    /// </summary>
    public static bool IsValid(OpenAIVoice voice)
    {
        return System.Enum.IsDefined(typeof(OpenAIVoice), voice);
    }
    
    /// <summary>
    /// Gibt die Standard-Voice zurück
    /// </summary>
    public static OpenAIVoice GetDefault()
    {
        return OpenAIVoice.alloy;
    }
    
    /// <summary>
    /// Gibt die Anzahl der verfügbaren Voices zurück
    /// </summary>
    public static int GetVoiceCount()
    {
        return System.Enum.GetValues(typeof(OpenAIVoice)).Length;
    }
}
