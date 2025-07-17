using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        public event System.Action<GameObject> OnCustomAvatarLoaded;
        
        // Shared avatar position for consistency between quick setup and LoadAvatarsStep
        private static readonly Vector3 STANDARD_AVATAR_POSITION = new Vector3(2.85f, -0.8f, 1.5f); // Vor dem Canvas und auf richtiger Höhe
        private static readonly Vector3 STANDARD_AVATAR_ROTATION = new Vector3(0f, 220f, 0f);
        
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
            if (name == "CustomAvatar")
            {
                OnCustomAvatarLoaded?.Invoke(avatar);
                UnityEngine.Debug.Log("[AvatarManager] OnCustomAvatarLoaded event fired.");
            }
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
        /// Check if all required avatars are loaded (Robert, RPM_Male, RPM_Female)
        /// </summary>
        public bool AreAvatarsLoaded() 
        {
            return loadedAvatars.Count >= 3; // Robert, RPM_Male, RPM_Female
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
        
        /// <summary>
        /// Get all registered avatars as a dictionary
        /// </summary>
        public Dictionary<string, GameObject> GetAllAvatars()
        {
            return new Dictionary<string, GameObject>(loadedAvatars);
        }
        
        /// <summary>
        /// Get all ReadyPlayerMe avatars (RPM_Male, RPM_Female, CustomAvatar)
        /// </summary>
        public Dictionary<string, GameObject> GetReadyPlayerMeAvatars()
        {
            var rpmAvatars = new Dictionary<string, GameObject>();
            
            foreach (var kvp in loadedAvatars)
            {
                string avatarName = kvp.Key;
                GameObject avatar = kvp.Value;
                
                // Check if this is a ReadyPlayerMe avatar
                if (IsReadyPlayerMeAvatar(avatarName, avatar))
                {
                    rpmAvatars[avatarName] = avatar;
                }
            }
            
            return rpmAvatars;
        }
        
        /// <summary>
        /// Check if an avatar is a ReadyPlayerMe avatar
        /// </summary>
        private bool IsReadyPlayerMeAvatar(string avatarName, GameObject avatar)
        {
            // RPM avatars by name
            if (avatarName == "RPM_Male" || avatarName == "RPM_Female" || avatarName == "CustomAvatar")
            {
                return true;
            }
            
            // Check if avatar has ReadyPlayerMe components or naming patterns
            if (avatar != null)
            {
                // Check for ReadyPlayerMe-specific components or patterns
                if (avatar.name.Contains("ReadyPlayerMe") || 
                    avatar.name.Contains("RPM_") ||
                    avatar.GetComponentInChildren<SkinnedMeshRenderer>()?.name.Contains("Wolf3D") == true)
                {
                    return true;
                }
                
                // Check for blendshapes typical of ReadyPlayerMe avatars
                var skinnedMeshRenderer = avatar.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer?.sharedMesh != null && skinnedMeshRenderer.sharedMesh.blendShapeCount > 10)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
