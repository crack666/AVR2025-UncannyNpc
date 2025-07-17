using UnityEngine;
using System.Collections.Generic;

namespace Setup
{
    /// <summary>
    /// Centralized avatar management system for consistent positioning and tracking
    /// </summary>
    public class AvatarManager
    {
        private static AvatarManager instance;
        public static AvatarManager Instance => instance ??= new AvatarManager();
        
        private Dictionary<string, GameObject> loadedAvatars = new Dictionary<string, GameObject>();
        public event System.Action<Dictionary<string, GameObject>> OnAvatarsLoaded;
        
        // Shared avatar position for consistency between quick setup and LoadAvatarsStep
        private static readonly Vector3 STANDARD_AVATAR_POSITION = new Vector3(1.9f, -1.9f, 1.13f);
        private static readonly Vector3 STANDARD_AVATAR_ROTATION = new Vector3(0f, 180f, 0f);
        
        /// <summary>
        /// Register an avatar with the manager and ensure consistent positioning
        /// </summary>
        public void RegisterAvatar(string name, GameObject avatar) 
        {
            if (avatar == null) return;
            
            loadedAvatars[name] = avatar;
            // Ensure consistent positioning
            avatar.transform.position = STANDARD_AVATAR_POSITION;
            avatar.transform.eulerAngles = STANDARD_AVATAR_ROTATION;
            avatar.transform.localScale = Vector3.one;
            
            UnityEngine.Debug.Log($"[AvatarManager] Registered avatar: {name} at standard position {STANDARD_AVATAR_POSITION}");
        }
        
        /// <summary>
        /// Get a registered avatar by name
        /// </summary>
        public GameObject GetAvatar(string name) 
        {
            loadedAvatars.TryGetValue(name, out GameObject avatar);
            return avatar;
        }
        
        /// <summary>
        /// Check if all required avatars are loaded (Robert, Leonard, RPM)
        /// </summary>
        public bool AreAvatarsLoaded() 
        {
            return loadedAvatars.Count >= 3; // Robert, Leonard, RPM
        }
        
        /// <summary>
        /// Notify subscribers that all avatars have been loaded
        /// </summary>
        public void NotifyAvatarsLoaded() 
        {
            OnAvatarsLoaded?.Invoke(loadedAvatars);
            UnityEngine.Debug.Log($"[AvatarManager] Notified {loadedAvatars.Count} avatars loaded");
        }
        
        /// <summary>
        /// Get the standard avatar position for consistency
        /// </summary>
        public static Vector3 GetStandardAvatarPosition() 
        {
            return STANDARD_AVATAR_POSITION;
        }
        
        /// <summary>
        /// Get the standard avatar rotation for consistency
        /// </summary>
        public static Vector3 GetStandardAvatarRotation() 
        {
            return STANDARD_AVATAR_ROTATION;
        }
        
        /// <summary>
        /// Clear all registered avatars (for testing/cleanup)
        /// </summary>
        public void ClearAvatars()
        {
            loadedAvatars.Clear();
            UnityEngine.Debug.Log("[AvatarManager] Cleared all registered avatars");
        }
        
        /// <summary>
        /// Get count of registered avatars
        /// </summary>
        public int GetAvatarCount()
        {
            return loadedAvatars.Count;
        }
        
        /// <summary>
        /// Get all registered avatar names
        /// </summary>
        public string[] GetAvatarNames()
        {
            var names = new string[loadedAvatars.Count];
            loadedAvatars.Keys.CopyTo(names, 0);
            return names;
        }
    }
}
