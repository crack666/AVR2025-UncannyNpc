using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using OpenAI.RealtimeAPI;

/// <summary>
/// Setup-Skript für die OpenAI Realtime AudioSources.
/// Fügt dauerhaft eine PlaybackAudioSource und eine MicrophoneAudioSource als Childs hinzu und verknüpft sie im RealtimeAudioManager.
/// </summary>
[ExecuteInEditMode]
public class RealtimeAudioManagerSetup : MonoBehaviour
{
    public RealtimeAudioManager audioManager;

    private void Reset()
    {
        if (audioManager == null)
            audioManager = GetComponent<RealtimeAudioManager>();
        SetupAudioSources();
    }

    [ContextMenu("Setup AudioSources (Playback & Microphone)")]
    public void SetupAudioSources()
    {
        if (audioManager == null)
            audioManager = GetComponent<RealtimeAudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("[RealtimeAudioManagerSetup] Kein RealtimeAudioManager gefunden!");
            return;
        }
        // PlaybackAudioSource
        var playback = transform.Find("PlaybackAudioSource");
        if (playback == null)
        {
            var go = new GameObject("PlaybackAudioSource");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            var src = go.AddComponent<AudioSource>();
            src.loop = false;
            src.volume = 1f;
            audioManager.GetType().GetField("playbackAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(audioManager, src);
            Debug.Log("[RealtimeAudioManagerSetup] PlaybackAudioSource erstellt und verknüpft.");
        }
        else
        {
            var src = playback.GetComponent<AudioSource>();
            if (src == null) src = playback.gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.volume = 1f;
            audioManager.GetType().GetField("playbackAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(audioManager, src);
            Debug.Log("[RealtimeAudioManagerSetup] PlaybackAudioSource gefunden und verknüpft.");
        }
        // MicrophoneAudioSource
        var mic = transform.Find("MicrophoneAudioSource");
        if (mic == null)
        {
            var go = new GameObject("MicrophoneAudioSource");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.mute = true;
            src.volume = 0f;
            audioManager.GetType().GetField("microphoneAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(audioManager, src);
            Debug.Log("[RealtimeAudioManagerSetup] MicrophoneAudioSource erstellt und verknüpft.");
        }
        else
        {
            var src = mic.GetComponent<AudioSource>();
            if (src == null) src = mic.gameObject.AddComponent<AudioSource>();
            src.loop = true;
            src.mute = true;
            src.volume = 0f;
            audioManager.GetType().GetField("microphoneAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(audioManager, src);
            Debug.Log("[RealtimeAudioManagerSetup] MicrophoneAudioSource gefunden und verknüpft.");
        }
        #if UNITY_EDITOR
        EditorUtility.SetDirty(audioManager);
        EditorUtility.SetDirty(gameObject);
        #endif
    }
}
