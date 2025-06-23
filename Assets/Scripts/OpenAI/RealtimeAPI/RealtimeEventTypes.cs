using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenAI.RealtimeAPI
{
    // Event base types
    [Serializable]
    public class RealtimeEvent
    {
        public string type;
        public string event_id;
        public SessionData session;
        public ConversationItem item;
        public ResponseData response;
        public string delta;
        public ErrorData error;
        public string audio;
        public string transcript;
        
        [JsonIgnore]
        public DateTime timestamp = DateTime.UtcNow;
    }
    
    [Serializable]
    public class SessionData
    {
        public string id;
        public string @object;
        public string model;
        public string[] modalities;
        public string instructions;
        public string voice;
        public string input_audio_format;
        public string output_audio_format;
        public InputAudioTranscription input_audio_transcription;        public TurnDetection turn_detection;
        [JsonConverter(typeof(ToolChoiceConverter))]
        public ToolChoice tool_choice;
        public Tool[] tools;        public float temperature;
        [JsonConverter(typeof(MaxTokensConverter))]
        public int max_response_output_tokens;
    }
    
    [Serializable]
    public class InputAudioTranscription
    {
        public string model;
    }
    
    [Serializable]
    public class TurnDetection
    {
        public string type;
        public float threshold;
        public int prefix_padding_ms;
        public int silence_duration_ms;
    }
    
    [Serializable]
    public class ToolChoice
    {
        public string type;
        public string name;
    }
    
    [Serializable]
    public class Tool
    {
        public string type;
        public string name;
        public string description;
        public ToolParameters parameters;
    }
    
    [Serializable]
    public class ToolParameters
    {
        public string type;
        public Dictionary<string, object> properties;
        public string[] required;
    }
    
    [Serializable]
    public class ConversationItem
    {
        public string id;
        public string @object;
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
    public class ResponseData
    {
        public string id;
        public string @object;
        public string status;
        public string status_details;
        public OutputItem[] output;
        public Usage usage;
    }
    
    [Serializable]
    public class OutputItem
    {
        public string id;
        public string @object;
        public string type;
        public string status;
        public string role;
        public ContentPart[] content;
    }
    
    [Serializable]
    public class Usage
    {
        public int total_tokens;
        public int input_tokens;
        public int output_tokens;
        public InputTokenDetails input_token_details;
        public OutputTokenDetails output_token_details;
    }
    
    [Serializable]
    public class InputTokenDetails
    {
        public int cached_tokens;
        public int text_tokens;
        public int audio_tokens;
    }
    
    [Serializable]
    public class OutputTokenDetails
    {
        public int text_tokens;
        public int audio_tokens;
    }
    
    [Serializable]
    public class ErrorData
    {
        public string type;
        public string code;
        public string message;
        public string param;
        public string event_id;
    }
    
    // Audio-specific events
    [Serializable]
    public class AudioBufferEvent
    {
        public string type = "input_audio_buffer.append";
        public string audio; // Base64 encoded audio
    }
    
    [Serializable]
    public class AudioBufferCommitEvent
    {
        public string type = "input_audio_buffer.commit";
    }
    
    [Serializable]
    public class AudioBufferClearEvent
    {
        public string type = "input_audio_buffer.clear";
    }
    
    // Response events
    [Serializable]
    public class ResponseCreateEvent
    {
        public string type = "response.create";
        public ResponseConfig response;
    }
    
    [Serializable]
    public class ResponseConfig
    {
        public string[] modalities;
        public string instructions;
        public string voice;
        public string output_audio_format;        public Tool[] tools;
        [JsonConverter(typeof(ToolChoiceConverter))]
        public ToolChoice tool_choice;
        public float temperature;
        public int max_output_tokens;
    }
    
    [Serializable]
    public class ResponseCancelEvent
    {
        public string type = "response.cancel";
    }
    
    // Conversation events
    [Serializable]
    public class ConversationItemCreateEvent
    {
        public string type = "conversation.item.create";
        public string previous_item_id;
        public ConversationItem item;
    }
    
    [Serializable]
    public class ConversationItemTruncateEvent
    {
        public string type = "conversation.item.truncate";
        public string item_id;
        public int content_index;
        public int audio_end_ms;
    }
    
    [Serializable]
    public class ConversationItemDeleteEvent
    {
        public string type = "conversation.item.delete";
        public string item_id;
    }
    
    // Session events
    [Serializable]
    public class SessionUpdateEvent
    {
        public string type = "session.update";
        public SessionData session;
    }
    
    // Rate limiting
    [Serializable]
    public class RateLimit
    {
        public string name;
        public int limit;
        public int remaining;
        public float reset_seconds;
    }
    
    [Serializable]
    public class RateLimitsEvent
    {
        public string type = "rate_limits.updated";
        public RateLimit[] rate_limits;
    }
    
    // Event type constants
    public static class EventTypes
    {
        // Server events
        public const string ERROR = "error";
        public const string SESSION_CREATED = "session.created";
        public const string SESSION_UPDATED = "session.updated";
        public const string CONVERSATION_CREATED = "conversation.created";
        public const string CONVERSATION_ITEM_CREATED = "conversation.item.created";
        public const string CONVERSATION_ITEM_INPUT_AUDIO_TRANSCRIPTION_COMPLETED = "conversation.item.input_audio_transcription.completed";
        public const string CONVERSATION_ITEM_INPUT_AUDIO_TRANSCRIPTION_FAILED = "conversation.item.input_audio_transcription.failed";
        public const string CONVERSATION_ITEM_TRUNCATED = "conversation.item.truncated";
        public const string CONVERSATION_ITEM_DELETED = "conversation.item.deleted";
        public const string INPUT_AUDIO_BUFFER_COMMITTED = "input_audio_buffer.committed";
        public const string INPUT_AUDIO_BUFFER_CLEARED = "input_audio_buffer.cleared";
        public const string INPUT_AUDIO_BUFFER_SPEECH_STARTED = "input_audio_buffer.speech_started";
        public const string INPUT_AUDIO_BUFFER_SPEECH_STOPPED = "input_audio_buffer.speech_stopped";
        public const string RESPONSE_CREATED = "response.created";
        public const string RESPONSE_DONE = "response.done";
        public const string RESPONSE_OUTPUT_ITEM_ADDED = "response.output_item.added";
        public const string RESPONSE_OUTPUT_ITEM_DONE = "response.output_item.done";
        public const string RESPONSE_CONTENT_PART_ADDED = "response.content_part.added";
        public const string RESPONSE_CONTENT_PART_DONE = "response.content_part.done";
        public const string RESPONSE_TEXT_DELTA = "response.text.delta";
        public const string RESPONSE_TEXT_DONE = "response.text.done";
        public const string RESPONSE_AUDIO_TRANSCRIPT_DELTA = "response.audio_transcript.delta";
        public const string RESPONSE_AUDIO_TRANSCRIPT_DONE = "response.audio_transcript.done";
        public const string RESPONSE_AUDIO_DELTA = "response.audio.delta";
        public const string RESPONSE_AUDIO_DONE = "response.audio.done";
        public const string RESPONSE_FUNCTION_CALL_ARGUMENTS_DELTA = "response.function_call_arguments.delta";
        public const string RESPONSE_FUNCTION_CALL_ARGUMENTS_DONE = "response.function_call_arguments.done";
        public const string RATE_LIMITS_UPDATED = "rate_limits.updated";
        
        // Client events
        public const string SESSION_UPDATE = "session.update";
        public const string INPUT_AUDIO_BUFFER_APPEND = "input_audio_buffer.append";
        public const string INPUT_AUDIO_BUFFER_COMMIT = "input_audio_buffer.commit";
        public const string INPUT_AUDIO_BUFFER_CLEAR = "input_audio_buffer.clear";
        public const string CONVERSATION_ITEM_CREATE = "conversation.item.create";
        public const string CONVERSATION_ITEM_TRUNCATE = "conversation.item.truncate";
        public const string CONVERSATION_ITEM_DELETE = "conversation.item.delete";
        public const string RESPONSE_CREATE = "response.create";
        public const string RESPONSE_CANCEL = "response.cancel";
    }
    
    // Utility class for event creation
    public static class EventFactory
    {
        public static SessionUpdateEvent CreateSessionUpdate(SessionData session)
        {
            return new SessionUpdateEvent
            {
                type = EventTypes.SESSION_UPDATE,
                session = session
            };
        }
        
        public static AudioBufferEvent CreateAudioBuffer(byte[] audioData)
        {
            return new AudioBufferEvent
            {
                type = EventTypes.INPUT_AUDIO_BUFFER_APPEND,
                audio = Convert.ToBase64String(audioData)
            };
        }
        
        public static ConversationItemCreateEvent CreateTextMessage(string text, string role = "user")
        {
            return new ConversationItemCreateEvent
            {
                type = EventTypes.CONVERSATION_ITEM_CREATE,
                item = new ConversationItem
                {
                    type = "message",
                    role = role,
                    content = new[]
                    {
                        new ContentPart
                        {
                            type = "input_text",
                            text = text
                        }
                    }
                }
            };
        }
        
        public static ResponseCreateEvent CreateResponse(string[] modalities = null, string instructions = null)
        {
            return new ResponseCreateEvent
            {
                type = EventTypes.RESPONSE_CREATE,
                response = new ResponseConfig
                {
                    modalities = modalities ?? new[] { "text", "audio" },
                    instructions = instructions
                }
            };
        }
    }
    
    /// <summary>
    /// Custom JSON converter to handle tool_choice which can be either a string ("auto", "none") or an object
    /// </summary>
    public class ToolChoiceConverter : JsonConverter<ToolChoice>
    {
        public override void WriteJson(JsonWriter writer, ToolChoice value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            
            if (value.type == "auto" || value.type == "none" || value.type == "required")
            {
                writer.WriteValue(value.type);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteValue(value.type);
                if (!string.IsNullOrEmpty(value.name))
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(value.name);
                }
                writer.WriteEndObject();
            }
        }
        
        public override ToolChoice ReadJson(JsonReader reader, Type objectType, ToolChoice existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
                
            if (reader.TokenType == JsonToken.String)
            {
                // Handle string values like "auto", "none", "required"
                var stringValue = reader.Value.ToString();
                return new ToolChoice
                {
                    type = stringValue,
                    name = null
                };
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                // Handle object values like { "type": "function", "name": "functionName" }
                var jObject = JObject.Load(reader);
                return new ToolChoice
                {
                    type = jObject["type"]?.ToString(),
                    name = jObject["name"]?.ToString()
                };
            }
            
            throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
        }
    }
    
    /// <summary>
    /// Custom JSON converter to handle max_response_output_tokens which can be either an integer or "inf" string
    /// </summary>
    public class MaxTokensConverter : JsonConverter<int>
    {
        public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
        {
            if (value == int.MaxValue || value < 0)
            {
                writer.WriteValue("inf");
            }
            else
            {
                writer.WriteValue(value);
            }
        }
        
        public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return 0;
                
            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value.ToString();
                if (stringValue.Equals("inf", StringComparison.OrdinalIgnoreCase) || 
                    stringValue.Equals("infinity", StringComparison.OrdinalIgnoreCase))
                {
                    return int.MaxValue; // Use int.MaxValue to represent infinity
                }
                
                if (int.TryParse(stringValue, out int result))
                {
                    return result;
                }
                
                throw new JsonSerializationException($"Could not convert string '{stringValue}' to integer");
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToInt32(reader.Value);
            }
            
            throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
        }
    }
}
