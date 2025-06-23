using System;
using UnityEngine;

namespace OpenAI.RealtimeAPI
{
    /// <summary>
    /// Datenstruktur für Audio-Chunks in der Realtime API
    /// </summary>
    [Serializable]
    public class AudioChunk
    {
        public byte[] audioData;
        public int sampleRate;
        public int channels;
        public float timestamp;
        public int sequenceNumber;
        
        public AudioChunk(byte[] data, int rate = 24000, int channelCount = 1)
        {
            audioData = data;
            sampleRate = rate;
            channels = channelCount;
            timestamp = Time.time;
        }
        
        /// <summary>
        /// Konvertiert Audio-Daten zu Base64 für API-Übertragung
        /// </summary>
        public string ToBase64()
        {
            return Convert.ToBase64String(audioData);
        }
        
        /// <summary>
        /// Erstellt AudioChunk aus Base64-String
        /// </summary>
        public static AudioChunk FromBase64(string base64Data, int rate = 24000)
        {
            byte[] data = Convert.FromBase64String(base64Data);
            return new AudioChunk(data, rate);
        }
        
        /// <summary>
        /// Konvertiert float[] samples zu PCM16 byte[]
        /// </summary>
        public static byte[] FloatToPCM16(float[] samples)
        {
            byte[] pcmData = new byte[samples.Length * 2];
            
            for (int i = 0; i < samples.Length; i++)
            {
                // Clamp und konvertiere zu 16-bit signed integer
                short sample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                
                // Little-endian byte order
                pcmData[i * 2] = (byte)(sample & 0xFF);
                pcmData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }
            
            return pcmData;
        }
        
        /// <summary>
        /// Konvertiert PCM16 byte[] zu float[] samples
        /// </summary>
        public static float[] PCM16ToFloat(byte[] pcmData)
        {
            float[] samples = new float[pcmData.Length / 2];
            
            for (int i = 0; i < samples.Length; i++)
            {
                // Little-endian byte order zu 16-bit signed integer
                short sample = (short)((pcmData[i * 2 + 1] << 8) | pcmData[i * 2]);
                
                // Konvertiere zu float [-1, 1]
                samples[i] = sample / (float)short.MaxValue;
            }
            
            return samples;
        }
        
        /// <summary>
        /// Berechnet die Dauer des Audio-Chunks in Sekunden
        /// </summary>
        public float GetDurationSeconds()
        {
            if (audioData == null || audioData.Length == 0)
                return 0f;
                
            // PCM16 = 2 bytes per sample
            int sampleCount = audioData.Length / 2 / channels;
            return (float)sampleCount / sampleRate;
        }
          /// <summary>
        /// Prüft ob der Audio-Chunk gültig ist
        /// </summary>
        public bool IsValid()
        {
            return audioData != null && 
                   audioData.Length > 0 && 
                   audioData.Length % 2 == 0 && // PCM16 requires even byte count
                   sampleRate > 0 && 
                   channels > 0;
        }
    }
}
