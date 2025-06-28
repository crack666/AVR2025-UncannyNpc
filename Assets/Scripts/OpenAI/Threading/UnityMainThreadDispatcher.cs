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

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UnityMainThreadDispatcher>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UnityMainThreadDispatcher");
                        _instance = go.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
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
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Static method for easy access from background threads
        /// </summary>
        public static void EnqueueAction(Action action)
        {
            Instance.Enqueue(action);
        }
    }
}
