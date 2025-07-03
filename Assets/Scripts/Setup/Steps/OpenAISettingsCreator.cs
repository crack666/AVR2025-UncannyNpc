using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Setup.Steps
{
    /// <summary>
    /// Helper class for creating OpenAISettings asset automatically
    /// </summary>
    public static class OpenAISettingsCreator
    {
        public const string SETTINGS_PATH = "Assets/Resources/OpenAISettings.asset";
        public const string RESOURCES_FOLDER = "Assets/Resources";

        /// <summary>
        /// Creates OpenAISettings asset if it doesn't exist
        /// </summary>
        /// <param name="apiKey">Optional API key to set</param>
        /// <returns>The created or existing OpenAISettings</returns>
        public static ScriptableObject CreateIfNotExists(string apiKey = null)
        {
#if UNITY_EDITOR
            // Prüfe ob bereits existiert
            var existingSettings = Resources.Load<ScriptableObject>("OpenAISettings");
            if (existingSettings != null)
            {
                Debug.Log("[OpenAISettings] Existing settings found in Resources folder");
                return existingSettings;
            }

            Debug.Log("[OpenAISettings] Creating new OpenAISettings asset...");

            // Erstelle Resources Ordner falls nicht vorhanden
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
            {
                Debug.Log("[OpenAISettings] Creating Resources folder...");
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Finde OpenAISettings Type
            var openAISettingsType = System.Type.GetType("OpenAISettings") ?? 
                                     System.Type.GetType("Assets.Settings.OpenAISettings") ??
                                     GetOpenAISettingsType();

            if (openAISettingsType == null)
            {
                Debug.LogError("[OpenAISettings] OpenAISettings type not found! Ensure the script exists.");
                return null;
            }

            // Erstelle ScriptableObject Instanz
            var newSettings = ScriptableObject.CreateInstance(openAISettingsType);
            if (newSettings == null)
            {
                Debug.LogError("[OpenAISettings] Failed to create OpenAISettings instance");
                return null;
            }

            // Setze API Key falls angegeben
            if (!string.IsNullOrEmpty(apiKey))
            {
                SetApiKey(newSettings, apiKey);
            }

            // Speichere Asset
            AssetDatabase.CreateAsset(newSettings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[OpenAISettings] Successfully created: {SETTINGS_PATH}");
            return newSettings;
#else
            // Runtime: Versuche aus Resources zu laden
            return Resources.Load<ScriptableObject>("OpenAISettings");
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Findet OpenAISettings Type durch Assembly-Suche
        /// </summary>
        private static System.Type GetOpenAISettingsType()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "OpenAISettings" && type.IsSubclassOf(typeof(ScriptableObject)))
                    {
                        Debug.Log($"[OpenAISettings] Found type: {type.FullName}");
                        return type;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Setzt API Key per Reflection
        /// </summary>
        private static void SetApiKey(ScriptableObject settings, string apiKey)
        {
            var type = settings.GetType();
            var apiKeyField = type.GetField("apiKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            
            if (apiKeyField != null)
            {
                apiKeyField.SetValue(settings, apiKey);
                Debug.Log($"[OpenAISettings] API key set successfully");
            }
            else
            {
                Debug.LogWarning("[OpenAISettings] Could not find 'apiKey' field to set");
            }
        }
#endif
    }
}
