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
    
    /// <summary>
    /// Zustand der Konversation und Audio-Pipeline
    /// </summary>
    [Serializable]
    public class ConversationState
    {
        public bool isConnected;
        public bool isRecording;
        public bool isPlaying;
        public bool isWaitingForResponse;
        
        public string currentResponseId;
        public string currentItemId;
        
        public float lastAudioInputTime;
        public float lastAudioOutputTime;
        
        public int totalAudioChunksSent;
        public int totalAudioChunksReceived;
        
        public float averageLatency;
        
        public void Reset()
        {
            isConnected = false;
            isRecording = false;
            isPlaying = false;
            isWaitingForResponse = false;
            
            currentResponseId = null;
            currentItemId = null;
            
            lastAudioInputTime = 0f;
            lastAudioOutputTime = 0f;
            
            totalAudioChunksSent = 0;
            totalAudioChunksReceived = 0;
            
            averageLatency = 0f;
        }
        
        public void UpdateLatency(float newLatency)
        {
            if (averageLatency == 0f)
            {
                averageLatency = newLatency;
            }
            else
            {
                // Exponential moving average
                averageLatency = averageLatency * 0.9f + newLatency * 0.1f;
            }
        }
    }
}
