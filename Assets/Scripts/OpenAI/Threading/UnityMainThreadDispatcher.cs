using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenAI.Threading
{
/// <summary>
/// Enables safe callbacks from background threads to Unity main thread
/// Prevents deadlocks when async operations need to interact with Unity APIs
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
private static readonly Queue<Action> _executionQueue = new Queue<Action>();
private static UnityMainThreadDispatcher _instance = null;
        private static volatile bool _applicationQuitting = false;
private static volatile bool _instanceInitialized = false;

public static UnityMainThreadDispatcher Instance
{
    get
    {
    // If application is quitting or instance failed to initialize, return null
    if (_applicationQuitting || !_instanceInitialized)
            return _instance;
            
        if (_instance == null)
        {
            // Only try to find/create instance from main thread
            // Use try-catch to handle background thread calls gracefully
            try
            {
                _instance = FindFirstObjectByType<UnityMainThreadDispatcher>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UnityMainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
                _instanceInitialized = true;
            }
            catch (UnityException)
            {
                // Called from background thread - this is expected during audio processing
                // Don't log warning every time, just return null
            return null;
            }
            }
                return _instance;
    }
}

void Awake()
{
// Ensure singleton pattern
if (_instance == null)
{
    _instance = this;
        _instanceInitialized = true;
                DontDestroyOnLoad(gameObject);
    }
    else if (_instance != this)
    {
        Destroy(gameObject);
    }
}

void OnApplicationQuit()
{
    _applicationQuitting = true;
        }

void Update()
{
    lock (_executionQueue)
    {
    while (_executionQueue.Count > 0)
    {
        _executionQueue.Dequeue().Invoke();
}
}
}

/// <summary>
/// Enqueue an action to be executed on the Unity main thread
/// </summary>
public void Enqueue(Action action)
    {
            if (_applicationQuitting) return;
            
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Static method for easy access from background threads
        /// Thread-safe: Will not crash if called from audio thread
        /// </summary>
        public static void EnqueueAction(Action action)
        {
            // Direct access to avoid Unity API calls from background threads
            if (_applicationQuitting || action == null) return;
            
            var instance = _instance;
            if (instance != null && _instanceInitialized)
            {
                instance.Enqueue(action);
            }
            // Silently ignore if instance not available (normal during audio thread execution)
        }
        
        /// <summary>
        /// Initialize the dispatcher from main thread (call this early in your app)
        /// </summary>
        public static void Initialize()
        {
            var _ = Instance; // Trigger initialization
        }
    }
}
