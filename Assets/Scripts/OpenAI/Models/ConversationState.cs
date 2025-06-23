using System;
using UnityEngine;

namespace OpenAI.RealtimeAPI
{
    /// <summary>
    /// Manages the state of an ongoing conversation
    /// </summary>
    [Serializable]
    public class ConversationState
    {
        public bool isConnected;
        public string sessionId;
        public int messageCount;
        public DateTime lastActivity;
        public string currentResponseId;
        
        public ConversationState()
        {
            isConnected = false;
            sessionId = "";
            messageCount = 0;
            lastActivity = DateTime.UtcNow;
            currentResponseId = "";
        }
        
        public void Reset()
        {
            isConnected = false;
            sessionId = "";
            messageCount = 0;
            lastActivity = DateTime.UtcNow;
            currentResponseId = "";
        }
        
        public void UpdateActivity()
        {
            lastActivity = DateTime.UtcNow;
        }
        
        public bool IsStale(float timeoutSeconds = 300f)
        {
            return (DateTime.UtcNow - lastActivity).TotalSeconds > timeoutSeconds;
        }
    }
}
