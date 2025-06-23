using System;
using UnityEngine;

namespace OpenAI.RealtimeAPI
{
    /// <summary>
    /// Event-Typen für OpenAI Realtime API Kommunikation
    /// Basiert auf der offiziellen API-Dokumentation
    /// </summary>
    
    [Serializable]
    public class RealtimeEvent
    {
        public string type;
        public string event_id;
        
        public RealtimeEvent(string eventType)
        {
            type = eventType;
            event_id = System.Guid.NewGuid().ToString();
        }
    }
    
    [Serializable]
    public class SessionUpdateEvent : RealtimeEvent
    {
        public SessionConfig session;
        
        public SessionUpdateEvent() : base("session.update") { }
    }
    
    [Serializable]
    public class SessionConfig
    {
        public string modalities = "['text', 'audio']";
        public string instructions = "";
        public string voice = "alloy";
        public string input_audio_format = "pcm16";
        public string output_audio_format = "pcm16";
        public VadConfig input_audio_transcription;
        public bool turn_detection;
        public TurnDetectionConfig turn_detection_config;
        public ToolsConfig[] tools;
        public float temperature = 0.8f;
        public int max_response_output_tokens = 4096;
    }
    
    [Serializable]
    public class VadConfig
    {
        public string model = "whisper-1";
    }
    
    [Serializable]
    public class TurnDetectionConfig
    {
        public string type = "server_vad";
        public float threshold = 0.5f;
        public int prefix_padding_ms = 300;
        public int silence_duration_ms = 200;
    }
    
    [Serializable]
    public class ToolsConfig
    {
        public string type = "function";
        public string name;
        public string description;
        public object parameters;
    }
    
    [Serializable]
    public class InputAudioBufferAppendEvent : RealtimeEvent
    {
        public string audio;
        
        public InputAudioBufferAppendEvent() : base("input_audio_buffer.append") { }
    }
    
    [Serializable]
    public class InputAudioBufferCommitEvent : RealtimeEvent
    {
        public InputAudioBufferCommitEvent() : base("input_audio_buffer.commit") { }
    }
    
    [Serializable]
    public class ConversationItemCreateEvent : RealtimeEvent
    {
        public ConversationItem item;
        
        public ConversationItemCreateEvent() : base("conversation.item.create") { }
    }
    
    [Serializable]
    public class ConversationItem
    {
        public string id;
        public string type;
        public string status;
        public string role;
        public ContentPart[] content;
    }
    
    [Serializable]
    public class ContentPart
    {
        public string type;
        public string text;
        public string audio;
        public string transcript;
    }
    
    [Serializable]
    public class ResponseCreateEvent : RealtimeEvent
    {
        public ResponseConfig response;
        
        public ResponseCreateEvent() : base("response.create") { }
    }
    
    [Serializable]
    public class ResponseConfig
    {
        public string modalities = "['text', 'audio']";
        public string instructions;
        public string voice = "alloy";
        public string output_audio_format = "pcm16";
        public ToolsConfig[] tools;
        public float temperature = 0.8f;
        public int max_output_tokens = 4096;
    }
    
    // Server Events (empfangen)
    [Serializable]
    public class ErrorEvent : RealtimeEvent
    {
        public ErrorDetails error;
        
        public ErrorEvent() : base("error") { }
    }
    
    [Serializable]
    public class ErrorDetails
    {
        public string type;
        public string code;
        public string message;
        public string param;
        public string event_id;
    }
    
    [Serializable]
    public class SessionCreatedEvent : RealtimeEvent
    {
        public SessionConfig session;
        
        public SessionCreatedEvent() : base("session.created") { }
    }
    
    [Serializable]
    public class ResponseAudioDeltaEvent : RealtimeEvent
    {
        public string response_id;
        public string item_id;
        public int output_index;
        public int content_index;
        public string delta;
        
        public ResponseAudioDeltaEvent() : base("response.audio.delta") { }
    }
    
    [Serializable]
    public class ResponseAudioDoneEvent : RealtimeEvent
    {
        public string response_id;
        public string item_id;
        public int output_index;
        public int content_index;
        
        public ResponseAudioDoneEvent() : base("response.audio.done") { }
    }
    
    [Serializable]
    public class ResponseTextDeltaEvent : RealtimeEvent
    {
        public string response_id;
        public string item_id;
        public int output_index;
        public int content_index;
        public string delta;
        
        public ResponseTextDeltaEvent() : base("response.text.delta") { }
    }
}
