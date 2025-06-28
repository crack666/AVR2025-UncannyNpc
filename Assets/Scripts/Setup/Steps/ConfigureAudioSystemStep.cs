using UnityEngine;
using System.Collections;

namespace Setup.Steps
{
    public class ConfigureAudioSystemStep
    {
        private System.Action<string> log;
        public ConfigureAudioSystemStep(System.Action<string> log) { this.log = log; }
        public IEnumerator Execute(GameObject npcSystem)
        {
            log("🔊 Step 3: Audio System Configuration");
            // --- MicrophoneAudioSource ---
            GameObject micAudioObj = npcSystem.transform.Find("MicrophoneAudioSource")?.gameObject;
            if (micAudioObj == null)
            {
                micAudioObj = new GameObject("MicrophoneAudioSource");
                micAudioObj.transform.SetParent(npcSystem.transform);
                micAudioObj.transform.localPosition = Vector3.zero;
                log("✅ Created: MicrophoneAudioSource GameObject");
            }
            AudioSource micAudio = micAudioObj.GetComponent<AudioSource>();
            if (micAudio == null)
            {
                micAudio = micAudioObj.AddComponent<AudioSource>();
                log("✅ Added: AudioSource component (Microphone)");
            }
            micAudio.loop = true;
            micAudio.mute = true;
            micAudio.volume = 0f;
            micAudio.playOnAwake = false;
            micAudio.spatialBlend = 0.0f;
            log("✅ AudioSource configured for microphone input");
            // --- PlaybackAudioSource ---
            GameObject playbackAudioObj = npcSystem.transform.Find("PlaybackAudioSource")?.gameObject;
            if (playbackAudioObj == null)
            {
                playbackAudioObj = new GameObject("PlaybackAudioSource");
                playbackAudioObj.transform.SetParent(npcSystem.transform);
                playbackAudioObj.transform.localPosition = Vector3.zero;
                log("✅ Created: PlaybackAudioSource GameObject");
            }
            AudioSource playbackAudio = playbackAudioObj.GetComponent<AudioSource>();
            if (playbackAudio == null)
            {
                playbackAudio = playbackAudioObj.AddComponent<AudioSource>();
                log("✅ Added: AudioSource component (Playback)");
            }
            playbackAudio.playOnAwake = false;
            playbackAudio.loop = false;
            playbackAudio.volume = 1.0f;
            playbackAudio.spatialBlend = 0.0f;
            log("✅ AudioSource configured for TTS playback");
            // --- RealtimeAudioManager ---
            MonoBehaviour audioManager = npcSystem.GetComponent("RealtimeAudioManager") as MonoBehaviour;
            if (audioManager == null)
            {
                System.Type audioManagerType = System.Type.GetType("OpenAI.RealtimeAPI.RealtimeAudioManager") ?? System.Type.GetType("RealtimeAudioManager");
                if (audioManagerType != null)
                {
                    audioManager = npcSystem.AddComponent(audioManagerType) as MonoBehaviour;
                    log("✅ Added: RealtimeAudioManager component");
                }
                else
                {
                    log("⚠️ RealtimeAudioManager type not found - will create placeholder");
                }
            }
            // --- Verknüpfe AudioSources im AudioManager per Reflection ---
            if (audioManager != null)
            {
                var playbackField = audioManager.GetType().GetField("playbackAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playbackField != null) playbackField.SetValue(audioManager, playbackAudio);
                var micField = audioManager.GetType().GetField("microphoneAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (micField != null) micField.SetValue(audioManager, micAudio);
                log("✅ AudioSources linked in RealtimeAudioManager");
            }
            log("✅ RealtimeAudioManager configured");
            yield return null;
        }
    }
}
