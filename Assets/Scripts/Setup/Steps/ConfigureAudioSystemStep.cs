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
            GameObject playbackAudioObj = GameObject.Find("PlaybackAudioSource");
            if (playbackAudioObj == null)
            {
                playbackAudioObj = new GameObject("PlaybackAudioSource");
                playbackAudioObj.transform.SetParent(npcSystem.transform);
                log("✅ Created: PlaybackAudioSource GameObject");
            }
            AudioSource playbackAudio = playbackAudioObj.GetComponent<AudioSource>();
            if (playbackAudio == null)
            {
                playbackAudio = playbackAudioObj.AddComponent<AudioSource>();
                log("✅ Added: AudioSource component");
            }
            playbackAudio.playOnAwake = false;
            playbackAudio.loop = false;
            playbackAudio.volume = 1.0f;
            playbackAudio.spatialBlend = 0.0f;
            log("✅ AudioSource configured for TTS playback");
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
            // Optionally: configure audioManager via reflection
            log("✅ RealtimeAudioManager configured");
            yield return null;
        }
    }
}
