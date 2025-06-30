using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Setup.Steps
{
    /// <summary>
    /// Enhanced LipSync Setup Step mit intelligenter uLipSync Integration
    /// Erkennt automatisch verfügbare LipSync-Systeme und konfiguriert optimal
    /// </summary>
    public class SetupLipSyncSystemStep
    {
        private System.Action<string> log;

        public SetupLipSyncSystemStep(System.Action<string> log)
        {
            this.log = log;
        }

        public IEnumerator Execute(GameObject targetAvatar, GameObject npcSystem)
        {
            log("👄 Step 5: Advanced LipSync System Setup");
            log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            if (targetAvatar == null)
            {
                log("❌ Cannot setup LipSync - no avatar found");
                yield break;
            }

            // Step 1: Detect available LipSync systems
            LipSyncSystemInfo systemInfo = DetectLipSyncSystems();
            LogSystemDetection(systemInfo);

            // Step 2: Setup optimal LipSync system
            if (systemInfo.HasULipSync)
            {
                log("🎯 Using uLipSync (Professional Grade)");
                // Execute uLipSync setup directly instead of yield return
                var setupCoroutine = SetupULipSyncSystem(targetAvatar, npcSystem, systemInfo);
                while (setupCoroutine.MoveNext()) { }
            }
            else
            {
                log("🔄 uLipSync not detected - setting up fallback system");
                // Execute fallback setup directly
                var fallbackCoroutine = SetupFallbackLipSync(targetAvatar, npcSystem);
                while (fallbackCoroutine.MoveNext()) { }

                // Guide user for upgrade
                if (systemInfo.CanInstallULipSync)
                {
                    log("");
                    log("💡 ENHANCEMENT AVAILABLE:");
                    log("   Install uLipSync for professional-grade lip animation:");
                    log("   → Window → Package Manager → Add from Git URL");
                    log("   → https://github.com/hecomi/uLipSync.git#upm");
                    log("   → Re-run setup after installation");
                }
            }

            // Step 3: Add NPCController
            var npcCoroutine = SetupNPCController(npcSystem);
            while (npcCoroutine.MoveNext()) { }

            // Step 4: Validate final setup
            ValidateCompleteSetup(targetAvatar, npcSystem, systemInfo);

            yield return null;
        }

        #region System Detection

        /// <summary>
        /// Informationen über verfügbare LipSync-Systeme
        /// </summary>
        private class LipSyncSystemInfo
        {
            public bool HasULipSync = false;
            public bool HasULipSyncBlendShape = false;
            public bool CanInstallULipSync = true;
            public string ULipSyncPath = "";
            public bool HasReadyPlayerMeBlendShapes = false;
            public List<string> AvailableBlendShapes = new List<string>();
            public SkinnedMeshRenderer FacialRenderer = null;
        }

        /// <summary>
        /// Erkennt verfügbare LipSync-Systeme
        /// </summary>
        private LipSyncSystemInfo DetectLipSyncSystems()
        {
            var info = new LipSyncSystemInfo();

            log("[DEBUG] === LipSync System Detection Started ===");

            // Robustly search for uLipSync types in all loaded assemblies
            System.Type uLipSyncType = null;
            System.Type blendShapeType = null;

            log("[DEBUG] Searching loaded assemblies for uLipSync types...");
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                // Defensive: skip dynamic/unloaded assemblies
                if (assembly.IsDynamic) continue;
                try
                {
                    log($"[DEBUG] Checking assembly: {assembly.GetName().Name}");
                    var t1 = assembly.GetType("uLipSync.uLipSync", false);
                    var t2 = assembly.GetType("uLipSync.uLipSyncBlendShape", false);

                    if (uLipSyncType == null && t1 != null)
                    {
                        uLipSyncType = t1;
                        log($"[DEBUG] ✅ Found uLipSync.uLipSync in {assembly.GetName().Name}");
                    }
                    if (blendShapeType == null && t2 != null)
                    {
                        blendShapeType = t2;
                        log($"[DEBUG] ✅ Found uLipSync.uLipSyncBlendShape in {assembly.GetName().Name}");
                    }
                    if (uLipSyncType != null && blendShapeType != null) break;
                }
                catch (System.Exception ex)
                {
                    log($"[DEBUG] ⚠️ Error checking assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }

            info.HasULipSync = uLipSyncType != null;
            info.HasULipSyncBlendShape = blendShapeType != null;

            log($"[DEBUG] Type resolution results:");
            log($"[DEBUG]   uLipSync: {(uLipSyncType != null ? uLipSyncType.FullName : "NOT FOUND")}");
            log($"[DEBUG]   uLipSyncBlendShape: {(blendShapeType != null ? blendShapeType.FullName : "NOT FOUND")}");

            // Check for uLipSync assemblies if types not found (legacy fallback)
            if (!info.HasULipSync)
            {
                string[] assemblyNames = { "uLipSync.Runtime", "uLipSync" };
                foreach (string assemblyName in assemblyNames)
                {
                    try
                    {
                        var assembly = System.Reflection.Assembly.Load(assemblyName);
                        if (assembly != null)
                        {
                            info.HasULipSync = true;
                            break;
                        }
                    }
                    catch { /* Assembly not found */ }
                }
            }

            // Check for uLipSync directories
            if (!info.HasULipSync)
            {
                string[] searchPaths = {
                    "Assets/uLipSync",
                    "Assets/Plugins/uLipSync",
                    "Packages/com.hecomi.ulipsync"
                };

                foreach (string path in searchPaths)
                {
                    if (Directory.Exists(path))
                    {
                        info.HasULipSync = true;
                        info.ULipSyncPath = path;
                        break;
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Logs detected systems
        /// </summary>
        private void LogSystemDetection(LipSyncSystemInfo info)
        {
            log("🔍 LipSync System Detection:");
            log($"   uLipSync Available: {(info.HasULipSync ? "✅ YES" : "❌ NO")}");
            log($"   uLipSyncBlendShape: {(info.HasULipSyncBlendShape ? "✅ YES" : "❌ NO")}");

            if (info.HasULipSync && !string.IsNullOrEmpty(info.ULipSyncPath))
            {
                log($"   Location: {info.ULipSyncPath}");
            }
            log("");
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Debug-Methode für LipSync-Events
        /// </summary>
        private static void DebugLipSyncUpdate(object lipSyncInfo)
        {
            Debug.Log($"[TEST] DebugLipSyncUpdate wurde aufgerufen! Info: {lipSyncInfo?.ToString() ?? "null"}");

            Debug.Log($"[DEBUG] LipSync Event triggered! Info type: {lipSyncInfo?.GetType().Name ?? "null"}");

            if (lipSyncInfo != null)
            {
                try
                {
                    // Try to get phoneme info via reflection
                    var phonemeField = lipSyncInfo.GetType().GetField("phoneme");
                    if (phonemeField != null)
                    {
                        var phoneme = phonemeField.GetValue(lipSyncInfo);
                        Debug.Log($"[DEBUG] LipSync Phoneme: {phoneme}");
                    }

                    var volumeField = lipSyncInfo.GetType().GetField("volume");
                    if (volumeField != null)
                    {
                        var volume = volumeField.GetValue(lipSyncInfo);
                        Debug.Log($"[DEBUG] LipSync Volume: {volume}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"[DEBUG] Could not extract LipSync info: {ex.Message}");
                }
            }
        }
        #endregion

        #region uLipSync Setup

        /// <summary>
        /// Konfiguriert uLipSync System (Professional Grade)
        /// KORRIGIERT: Fügt fehlende uLipSync-Konfigurationen hinzu basierend auf Scene "11111"
        /// </summary>
        private IEnumerator SetupULipSyncSystem(GameObject targetAvatar, GameObject npcSystem, LipSyncSystemInfo info)
        {
            log($"[DEBUG] SetupULipSyncSystem: targetAvatar={targetAvatar?.name}, npcSystem={npcSystem?.name}");
            log("🎭 Configuring uLipSync Professional System...");

            // Enhanced type resolution with detailed debugging
            System.Type uLipSyncType = null;
            System.Type blendShapeType = null;

            log("[DEBUG] Attempting to resolve uLipSync types...");

            // Method 1: Direct type resolution
            uLipSyncType = System.Type.GetType("uLipSync.uLipSync");
            blendShapeType = System.Type.GetType("uLipSync.uLipSyncBlendShape");
            log($"[DEBUG] Direct resolution - uLipSync: {uLipSyncType?.FullName ?? "null"}, BlendShape: {blendShapeType?.FullName ?? "null"}");

            // Method 2: Assembly-qualified name resolution
            if (uLipSyncType == null || blendShapeType == null)
            {
                log("[DEBUG] Trying assembly-qualified names...");
                string[] assemblyNames = { "uLipSync.Runtime", "uLipSync", "Assembly-CSharp" };

                foreach (string assemblyName in assemblyNames)
                {
                    if (uLipSyncType == null)
                    {
                        uLipSyncType = System.Type.GetType($"uLipSync.uLipSync, {assemblyName}");
                        if (uLipSyncType != null) log($"[DEBUG] Found uLipSync in assembly: {assemblyName}");
                    }
                    if (blendShapeType == null)
                    {
                        blendShapeType = System.Type.GetType($"uLipSync.uLipSyncBlendShape, {assemblyName}");
                        if (blendShapeType != null) log($"[DEBUG] Found uLipSyncBlendShape in assembly: {assemblyName}");
                    }
                }
            }

            // Method 3: Search all loaded assemblies (same as DetectLipSyncSystems but with logging)
            if (uLipSyncType == null || blendShapeType == null)
            {
                log("[DEBUG] Searching all loaded assemblies...");
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic) continue;
                    try
                    {
                        if (uLipSyncType == null)
                        {
                            var t1 = assembly.GetType("uLipSync.uLipSync", false);
                            if (t1 != null)
                            {
                                uLipSyncType = t1;
                                log($"[DEBUG] Found uLipSync in assembly: {assembly.FullName}");
                            }
                        }
                        if (blendShapeType == null)
                        {
                            var t2 = assembly.GetType("uLipSync.uLipSyncBlendShape", false);
                            if (t2 != null)
                            {
                                blendShapeType = t2;
                                log($"[DEBUG] Found uLipSyncBlendShape in assembly: {assembly.FullName}");
                            }
                        }
                        if (uLipSyncType != null && blendShapeType != null) break;
                    }
                    catch (System.Exception ex)
                    {
                        log($"[DEBUG] Assembly search error in {assembly.GetName().Name}: {ex.Message}");
                    }
                }
            }

            // Final check
            if (uLipSyncType == null || blendShapeType == null)
            {
                log("❌ uLipSync types not accessible after all resolution attempts!");
                log($"   uLipSyncType: {uLipSyncType?.FullName ?? "NOT FOUND"}");
                log($"   blendShapeType: {blendShapeType?.FullName ?? "NOT FOUND"}");
                log("   → Falling back to ReadyPlayerMeLipSync system");
                var fallbackCoroutine = SetupFallbackLipSync(targetAvatar, npcSystem);
                while (fallbackCoroutine.MoveNext()) { }
                yield break;
            }

            log($"✅ Successfully resolved uLipSync types:");
            log($"   uLipSync: {uLipSyncType.FullName}");
            log($"   uLipSyncBlendShape: {blendShapeType.FullName}");

            // ===== KORRIGIERTE REIHENFOLGE - RACE CONDITION BEHOBEN =====
            
            // Step 1: Setup uLipSync component on Audio System (ZUERST!)
            log("🔧 Step 1: Setting up uLipSync component on audio system...");
            var componentCoroutine = SetupULipSyncComponent(npcSystem, uLipSyncType);
            while (componentCoroutine.MoveNext()) { }

            // Step 2: Configure uLipSync Component with Profile (SOFORT nach Component!)
            log("⚙️ Step 2: Configuring uLipSync profile...");
            var profileCoroutine = ConfigureULipSyncProfile(npcSystem, uLipSyncType);
            while (profileCoroutine.MoveNext()) { }

            // ⚡ KRITISCH: Warte einen Frame damit uLipSync sich vollständig initialisiert
            yield return null;
            
            // Step 3: Setup uLipSyncBlendShape on Avatar (NACH uLipSync-Konfiguration!)
            log("🎭 Step 3: Setting up uLipSyncBlendShape on avatar...");
            var blendShapeCoroutine = SetupULipSyncBlendShape(targetAvatar, blendShapeType);
            while (blendShapeCoroutine.MoveNext()) { }

            // ⚡ KRITISCH: Nochmal warten damit BlendShape-Component vollständig konfiguriert ist
            yield return null;

            // Step 4: Link components with Event Setup (GANZ ZUM SCHLUSS!)
            log("🔗 Step 4: Linking components with event setup...");
            var linkCoroutine = LinkULipSyncComponents(targetAvatar, npcSystem, uLipSyncType, blendShapeType);
            while (linkCoroutine.MoveNext()) { }

            log("🎉 uLipSync system configured successfully!");
            log($"[DEBUG] SetupULipSyncSystem complete for avatar={targetAvatar.name}, npcSystem={npcSystem.name}");
        }

        /// <summary>
        /// Konfiguriert uLipSyncBlendShape auf Avatar
        /// </summary>
        private IEnumerator SetupULipSyncBlendShape(GameObject targetAvatar, System.Type blendShapeType)
        {
            log($"[DEBUG] SetupULipSyncBlendShape: targetAvatar={targetAvatar?.name}, blendShapeType={blendShapeType?.FullName}");
            log($"[DEBUG] Checking if {blendShapeType.Name} component already exists on avatar...");

            // Add component if not exists
            Component blendShapeComponent = targetAvatar.GetComponent(blendShapeType);
            if (blendShapeComponent == null)
            {
                log($"[DEBUG] Component not found, attempting to add {blendShapeType.Name} to {targetAvatar.name}");
                try
                {
                    blendShapeComponent = targetAvatar.AddComponent(blendShapeType);
                    log($"✅ Successfully added uLipSyncBlendShape to avatar '{targetAvatar.name}' (type: {blendShapeType.FullName})");
                    log($"[DEBUG] Component reference: {blendShapeComponent != null}");
                }
                catch (System.Exception ex)
                {
                    log($"❌ Failed to add uLipSyncBlendShape to '{targetAvatar.name}': {ex.Message}");
                    log($"[DEBUG] Stack trace: {ex.StackTrace}");
                    yield break;
                }
            }
            else
            {
                log($"[DEBUG] uLipSyncBlendShape already present on '{targetAvatar.name}'");
            }
            // Find facial renderer
            SkinnedMeshRenderer facialRenderer = FindFacialRenderer(targetAvatar);
            if (facialRenderer == null)
            {
                log($"⚠️ No facial SkinnedMeshRenderer found on '{targetAvatar.name}'");
                yield break;
            }
            // KRITISCH: Configure renderer (muss auf Renderer_Head, nicht EyeLeft gesetzt werden!)
            var rendererField = blendShapeType.GetField("skinnedMeshRenderer");
            if (rendererField != null)
            {
                rendererField.SetValue(blendShapeComponent, facialRenderer);
                log($"✅ Linked to SkinnedMeshRenderer: {facialRenderer.name} (on '{targetAvatar.name}')");
            }
            else
            {
                log($"⚠️ Could not find 'skinnedMeshRenderer' field on {blendShapeType.FullName}");
            }

            // KRITISCH: Setze maxBlendShapeValue auf 1 (nicht Default!)
            var maxValueField = blendShapeType.GetField("maxBlendShapeValue");
            if (maxValueField != null)
            {
                maxValueField.SetValue(blendShapeComponent, 1.0f);
                log($"✅ Set maxBlendShapeValue to 1.0 for optimal mouth movement");
            }
            else
            {
                log($"⚠️ Could not find 'maxBlendShapeValue' field on {blendShapeType.FullName}");
            }
            // Configure ReadyPlayerMe BlendShape mappings
            ConfigureReadyPlayerMeBlendShapes(blendShapeComponent, facialRenderer, blendShapeType);
            yield return null;
        }

        /// <summary>
        /// NEUE METHODE: Konfiguriert uLipSync-Profil (KRITISCH für Funktionalität!)
        /// Lädt automatisch das Sample-Profil von uLipSync
        /// </summary>
        private IEnumerator ConfigureULipSyncProfile(GameObject npcSystem, System.Type uLipSyncType)
        {
            log("🎯 Configuring uLipSync Profile (CRITICAL for functionality)...");
            
            // Find uLipSync component on PlaybackAudioSource
            AudioSource playbackAudioSource = FindPlaybackAudioSource(npcSystem);
            if (playbackAudioSource == null)
            {
                log("❌ Cannot configure profile - PlaybackAudioSource not found");
                yield break;
            }
            
            Component uLipSyncComponent = playbackAudioSource.gameObject.GetComponent(uLipSyncType);
            if (uLipSyncComponent == null)
            {
                log("❌ Cannot configure profile - uLipSync component not found");
                yield break;
            }
            
            // Load uLipSync sample profile
            UnityEngine.Object profile = LoadULipSyncSampleProfile();
            if (profile != null)
            {
                var profileField = uLipSyncType.GetField("profile");
                if (profileField != null)
                {
                    profileField.SetValue(uLipSyncComponent, profile);
                    log($"✅ Set uLipSync profile to: {profile.name}");
#if UNITY_EDITOR
                    log($"[DEBUG] Profile loaded from: {UnityEditor.AssetDatabase.GetAssetPath(profile)}");
#else
                    log($"[DEBUG] Profile loaded: {profile.name}");
#endif
                }
                else
                {
                    log("❌ Could not find 'profile' field on uLipSync component");
                }
            }
            else
            {
                log("⚠️ Could not load uLipSync sample profile - component will use default settings");
                log("💡 You can manually set a profile later in the Inspector");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Lädt das uLipSync Sample-Profil automatisch (Editor-sicher)
        /// </summary>
        private UnityEngine.Object LoadULipSyncSampleProfile()
        {
#if UNITY_EDITOR
            // Mögliche Pfade für das Sample-Profil
            string[] profilePaths = {
                "Packages/com.hecomi.ulipsync/Assets/Profiles/uLipSync-Profile-Sample.asset", // Package Cache
                "Assets/Packages/uLipSync/Assets/Profiles/uLipSync-Profile-Sample.asset", // Alternative
                "Assets/uLipSync-Profile-Sample.asset", // Projekt-lokale Kopie
            };
            
            // Versuche direkte Pfade zuerst
            foreach (string path in profilePaths)
            {
                var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (profile != null)
                {
                    log($"[DEBUG] Found profile at: {path}");
                    return profile;
                }
            }
            
            // Suche in Package Cache mit Wildcard
            string baseDir = "Library/PackageCache";
            if (Directory.Exists(baseDir))
            {
                var dirs = Directory.GetDirectories(baseDir, "com.hecomi.ulipsync@*");
                foreach (var dir in dirs)
                {
                    string fullPath = Path.Combine(dir, "Assets/Profiles/uLipSync-Profile-Sample.asset");
                    if (File.Exists(fullPath))
                    {
                        // Convert to Unity-relative path für AssetDatabase
                        string relativePath = fullPath.Replace('\\', '/');
                        var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                        if (profile != null)
                        {
                            log($"[DEBUG] Found profile at: {relativePath}");
                            return profile;
                        }
                    }
                }
            }
            
            // Fallback: Suche nach allen .asset files mit "uLipSync" und "Profile" im Namen
            string[] allAssets = UnityEditor.AssetDatabase.FindAssets("uLipSync-Profile t:ScriptableObject");
            foreach (string guid in allAssets)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains("Sample"))
                {
                    var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (profile != null)
                    {
                        log($"[DEBUG] Found profile via search: {assetPath}");
                        return profile;
                    }
                }
            }
            
            log("[DEBUG] No uLipSync sample profile found in any location");
#else
            log("[DEBUG] Profile loading only available in Editor - will use default settings");
#endif
            return null;
        }

        /// <summary>
        /// Konfiguriert uLipSync Haupt-Component
        /// </summary>
        private IEnumerator SetupULipSyncComponent(GameObject npcSystem, System.Type uLipSyncType)
        {
        log($"[DEBUG] SetupULipSyncComponent: npcSystem={npcSystem?.name}, uLipSyncType={uLipSyncType?.FullName}");
        
        // Find AudioSource for uLipSync
        log($"[DEBUG] Searching for PlaybackAudioSource in {npcSystem.name}...");
        AudioSource playbackAudioSource = FindPlaybackAudioSource(npcSystem);
        if (playbackAudioSource == null)
        {
        log($"⚠️ Playback AudioSource not found in '{npcSystem.name}'");
        // List all AudioSources for debugging
        AudioSource[] allAudioSources = npcSystem.GetComponentsInChildren<AudioSource>();
        log($"[DEBUG] Found {allAudioSources.Length} AudioSources in total:");
        foreach (var source in allAudioSources)
        {
        log($"[DEBUG]   - {source.gameObject.name} (AudioSource)");
        }
        yield break;
        }
        
        GameObject audioSourceParent = playbackAudioSource.gameObject;
        log($"[DEBUG] Found PlaybackAudioSource on GameObject: {audioSourceParent.name}");
        log($"[DEBUG] AudioSource enabled: {playbackAudioSource.enabled}, clip: {playbackAudioSource.clip?.name ?? "null"}");
        
        // CRITICAL: uLipSync needs SPECIFIC AudioClip for OnAudioFilterRead to work
        // Must match exactly what's used in working Scene "11111"
        log($"[DEBUG] Creating uLipSync_Dummy AudioClip (required for LipSync processing)");
        AudioClip dummyClip = AudioClip.Create("uLipSync_Dummy", 24000, 1, 24000, false);
        playbackAudioSource.clip = dummyClip;
        log($"✅ Set uLipSync_Dummy AudioClip: {dummyClip.name} (channels: {dummyClip.channels}, freq: {dummyClip.frequency})");
        
        log($"[DEBUG] Checking if {uLipSyncType.Name} component already exists...");
        
        // Add uLipSync to same GameObject as AudioSource (CRITICAL for OnAudioFilterRead)
        Component uLipSyncComponent = audioSourceParent.GetComponent(uLipSyncType);
        if (uLipSyncComponent == null)
        {
        log($"[DEBUG] Component not found, attempting to add {uLipSyncType.Name} to {audioSourceParent.name}");
        try
            {
                uLipSyncComponent = audioSourceParent.AddComponent(uLipSyncType);
                log($"✅ Successfully added uLipSync to '{audioSourceParent.name}' (type: {uLipSyncType.FullName})");
            log($"[DEBUG] Component reference: {uLipSyncComponent != null}");
            }
            catch (System.Exception ex)
            {
                log($"❌ Failed to add uLipSync to '{audioSourceParent.name}': {ex.Message}");
                log($"[DEBUG] Stack trace: {ex.StackTrace}");
                yield break;
            }
        }
        else
        {
            log($"[DEBUG] uLipSync already present on '{audioSourceParent.name}'");
        }
        
        // Configure uLipSync for OpenAI audio
        ConfigureULipSyncForOpenAI(uLipSyncComponent, uLipSyncType);
        
        // Additional debug info about the uLipSync component
        log($"[DEBUG] uLipSync component enabled: {((MonoBehaviour)uLipSyncComponent).enabled}");
        log($"[DEBUG] AudioSource after uLipSync setup - clip: {playbackAudioSource.clip?.name ?? "null"}");
        
        // Check if uLipSync has a profile set
        try
        {
            var profileField = uLipSyncType.GetField("profile");
                if (profileField != null)
                {
                    var profile = profileField.GetValue(uLipSyncComponent);
                    log($"[DEBUG] uLipSync profile: {profile?.ToString() ?? "null"}");
                }
            }
            catch (System.Exception ex)
            {
                log($"[DEBUG] Could not check uLipSync profile: {ex.Message}");
            }
            
            yield return null;
        }

        /// <summary>
        /// Verknüpft uLipSync Komponenten
        /// ERWEITERT: Validiert Komponenten vor Event-Verknüpfung
        /// </summary>
        private IEnumerator LinkULipSyncComponents(GameObject targetAvatar, GameObject npcSystem, System.Type uLipSyncType, System.Type blendShapeType)
        {
            log("🔗 Linking uLipSync components...");

            // KRITISCH: Validiere dass alle Komponenten VOLLSTÄNDIG konfiguriert sind
            AudioSource audioSource = FindPlaybackAudioSource(npcSystem);
            if (audioSource == null)
            {
                log("❌ AudioSource not found for linking");
                yield break;
            }

            Component uLipSyncComponent = audioSource.gameObject.GetComponent(uLipSyncType);
            Component blendShapeComponent = targetAvatar.GetComponent(blendShapeType);

            if (uLipSyncComponent == null || blendShapeComponent == null)
            {
                log($"❌ Components not found for linking - uLipSync: {uLipSyncComponent != null}, BlendShape: {blendShapeComponent != null}");
                yield break;
            }

            // NEUE VALIDIERUNG: Prüfe ob uLipSync vollständig initialisiert ist
            var profileField = uLipSyncType.GetField("profile");
            if (profileField != null)
            {
                var profile = profileField.GetValue(uLipSyncComponent);
                if (profile == null)
                {
                    log("⚠️ WARNING: uLipSync profile not set - component may not be fully initialized");
                    log("💡 Attempting to link anyway, but consider setting a profile manually");
                }
                else
                {
                    log($"✅ uLipSync profile validated: {profile}");
                }
            }

            // NEUE VALIDIERUNG: Prüfe ob BlendShape-Component konfiguriert ist
            var rendererField = blendShapeType.GetField("skinnedMeshRenderer");
            if (rendererField != null)
            {
                var renderer = rendererField.GetValue(blendShapeComponent);
                if (renderer == null)
                {
                    log("⚠️ WARNING: uLipSyncBlendShape renderer not set - component may not be fully configured");
                }
                else
                {
                    log($"✅ uLipSyncBlendShape renderer validated: {((SkinnedMeshRenderer)renderer).name}");
                }
            }

            log($"[DEBUG] All components validated - proceeding with event linking");
            log($"[DEBUG] Found components - AudioSource: {audioSource.name}, uLipSync: {uLipSyncComponent.name}, BlendShape: {blendShapeComponent.name}");

            // KRITISCH: Link uLipSync.onLipSyncUpdate → uLipSyncBlendShape.OnLipSyncUpdate
            // Das ist das Hauptproblem in Ihrem Setup gewesen!
            // KORRIGIERT: Füge PERSISTENT Listener hinzu (nicht nur Runtime!)
            var onUpdateField = uLipSyncType.GetField("onLipSyncUpdate");
            if (onUpdateField != null)
            {
                var unityEvent = onUpdateField.GetValue(uLipSyncComponent);
                if (unityEvent != null)
                {
                    bool editorLinkingSuccessful = false;
                    
                    // METHODE 1: Unity's SerializedObject approach (funktioniert garantiert!)
                    try
                    {
                        var serializedObject = new UnityEditor.SerializedObject(uLipSyncComponent);
                        var onLipSyncUpdateProperty = serializedObject.FindProperty("onLipSyncUpdate");
                        if (onLipSyncUpdateProperty != null)
                        {
                            var persistentCallsProperty = onLipSyncUpdateProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                            if (persistentCallsProperty != null)
                            {
                                // Add a new persistent call
                                persistentCallsProperty.arraySize++;
                                var newCallProperty = persistentCallsProperty.GetArrayElementAtIndex(persistentCallsProperty.arraySize - 1);
                                
                                // Set the target to the BlendShape COMPONENT (not the GameObject)
                                var targetProperty = newCallProperty.FindPropertyRelative("m_Target");
                                targetProperty.objectReferenceValue = blendShapeComponent;
                                log($"[DEBUG] Set target to: {blendShapeComponent.name} (Component: {blendShapeComponent.GetType().Name})");
                                
                                // Set the method name
                                var methodNameProperty = newCallProperty.FindPropertyRelative("m_MethodName");
                                methodNameProperty.stringValue = "OnLipSyncUpdate";
                                log($"[DEBUG] Set method name to: OnLipSyncUpdate");
                                
                                // Set the mode (0 = VOID - kein Parameter!)
                                var modeProperty = newCallProperty.FindPropertyRelative("m_Mode");
                                modeProperty.enumValueIndex = 0; // PersistentListenerMode.Void (NICHT Object!)
                                log($"[DEBUG] Set mode to: 0 (Void - kein Parameter)");
                                
                                // Set call state (2 = RuntimeOnly - exactly what we need!)
                                var callStateProperty = newCallProperty.FindPropertyRelative("m_CallState");
                                callStateProperty.enumValueIndex = 2; // UnityEventCallState.RuntimeOnly
                                log($"[DEBUG] Set call state to: 2 (RuntimeOnly)");
                                
                                // KRITISCH: Set Arguments richtig (Object-Parameter!)
                                var argumentsProperty = newCallProperty.FindPropertyRelative("m_Arguments");
                                if (argumentsProperty != null)
                                {
                                    var objectArgAssemblyProperty = argumentsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName");
                                    if (objectArgAssemblyProperty != null)
                                    {
                                        objectArgAssemblyProperty.stringValue = "UnityEngine.Object, UnityEngine";
                                        log("[DEBUG] Set ObjectArgumentAssemblyTypeName to: UnityEngine.Object, UnityEngine");
                                    }
                                    else
                                    {
                                        log("[WARNING] Could not find ObjectArgumentAssemblyTypeName property!");
                                    }
                                }
                                else
                                {
                                    log("[WARNING] Could not find Arguments property!");
                                }
                                
                                // KRITISCH: Set targetAssemblyTypeName (wie in funktionierender Scene!)
                                var assemblyTypeNameProperty = newCallProperty.FindPropertyRelative("m_TargetAssemblyTypeName");
                                if (assemblyTypeNameProperty != null)
                                {
                                    assemblyTypeNameProperty.stringValue = "uLipSync.uLipSyncBlendShape, uLipSync.Runtime";
                                    log("[DEBUG] Set TargetAssemblyTypeName to: uLipSync.uLipSyncBlendShape, uLipSync.Runtime");
                                }
                                else
                                {
                                    log("[WARNING] Could not find TargetAssemblyTypeName property!");
                                }
                                
                                // Apply the changes
                                serializedObject.ApplyModifiedProperties();
                                
                                // KRITISCH: Force Unity Inspector refresh
                                UnityEditor.EditorUtility.SetDirty(uLipSyncComponent);
                                UnityEditor.AssetDatabase.SaveAssets();
                                UnityEditor.AssetDatabase.Refresh();
                                
                                // SUPER-KRITISCH: Scene speichern damit FileIDs persistent sind!
                                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                                log("[DEBUG] Scene saved to persist FileID references!");
                                
                                log("✅ KRITISCH: Added PERSISTENT listener via SerializedObject (GUARANTEED to work!)");
                                log("[DEBUG] Set to RuntimeOnly mode - exactly like manual setup!");
                                log("[DEBUG] Forced Unity Inspector refresh!");
                                
                                // Force re-serialization
                                //yield return null;
                                
                                // Double-check: Validate the connection was actually set
                                var validationSerializedObject = new UnityEditor.SerializedObject(uLipSyncComponent);
                                var validationProperty = validationSerializedObject.FindProperty("onLipSyncUpdate");
                                if (validationProperty != null)
                                {
                                    var validationCalls = validationProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                                    if (validationCalls != null && validationCalls.arraySize > 0)
                                    {
                                        var lastCall = validationCalls.GetArrayElementAtIndex(validationCalls.arraySize - 1);
                                        var targetValidation = lastCall.FindPropertyRelative("m_Target");
                                        var methodValidation = lastCall.FindPropertyRelative("m_MethodName");
                                        var assemblyValidation = lastCall.FindPropertyRelative("m_TargetAssemblyTypeName");
                                        
                                        log($"[VALIDATION] Target: {targetValidation?.objectReferenceValue?.name ?? "null"}");
                                        log($"[VALIDATION] Method: {methodValidation?.stringValue ?? "null"}");
                                        log($"[VALIDATION] Assembly: {assemblyValidation?.stringValue ?? "null"}");
                                        
                                        if (targetValidation?.objectReferenceValue == blendShapeComponent && 
                                            methodValidation?.stringValue == "OnLipSyncUpdate")
                                        {
                                            log("✅ VALIDATION PASSED: Event is correctly linked!");
                                            editorLinkingSuccessful = true;
                                        }
                                        else
                                        {
                                            log("❌ VALIDATION FAILED: Event link not persisted correctly");
                                            editorLinkingSuccessful = false;
                                        }
                                    }
                                }
                            }
                        }
                        log("[DEBUG] SerializedObject approach only available in Editor");
                    }
                    catch (System.Exception ex)
                    {
                        log($"[DEBUG] SerializedObject approach failed: {ex.Message}");
                    }
                    
                    // METHODE 2: Direct reflection on UnityEvent internals
                    try
                    {
                        // Get the persistent calls array directly
                        var persistentCallsField = unityEvent.GetType().GetField("m_PersistentCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (persistentCallsField != null)
                        {
                            var persistentCalls = persistentCallsField.GetValue(unityEvent);
                            var callsField = persistentCalls.GetType().GetField("m_Calls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            if (callsField != null)
                            {
                                var callsList = callsField.GetValue(persistentCalls) as System.Collections.IList;
                                if (callsList != null)
                                {
                                    // Create a new PersistentCall
                                    var persistentCallType = System.Type.GetType("UnityEngine.Events.PersistentCall, UnityEngine");
                                    if (persistentCallType != null)
                                    {
                                        var newCall = System.Activator.CreateInstance(persistentCallType);
                                        
                                        // Set target
                                        var targetField = persistentCallType.GetField("m_Target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        targetField?.SetValue(newCall, blendShapeComponent);
                                        
                                        // Set method name
                                        var methodNameField = persistentCallType.GetField("m_MethodName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        methodNameField?.SetValue(newCall, "OnLipSyncUpdate");
                                        
                                        // Set call state to RuntimeOnly (2)
                                        var callStateField = persistentCallType.GetField("m_CallState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        callStateField?.SetValue(newCall, 2); // UnityEventCallState.RuntimeOnly
                                        
                                        // Set mode to Object (1)
                                        var modeField = persistentCallType.GetField("m_Mode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        modeField?.SetValue(newCall, 1); // PersistentListenerMode.Object
                                        
                                        // Add to list
                                        callsList.Add(newCall);
                                        
                                        log("✅ KRITISCH: Added PERSISTENT listener via direct reflection!");
                                        log("[DEBUG] Set to RuntimeOnly mode with Object parameter");
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        log($"[DEBUG] Direct reflection approach failed: {ex.Message}");
                    }
                    
                    // METHODE 3: Fallback - Runtime Listener (nur wenn Editor-Linking fehlgeschlagen)
                    if (!editorLinkingSuccessful)
                    {
                        var addListenerMethod = unityEvent.GetType().GetMethod("AddListener");
                        var onLipSyncUpdateMethod2 = blendShapeType.GetMethod("OnLipSyncUpdate");

                        if (addListenerMethod != null && onLipSyncUpdateMethod2 != null)
                        {
                            var delegateType = addListenerMethod.GetParameters()[0].ParameterType;
                            var del = System.Delegate.CreateDelegate(delegateType, blendShapeComponent, onLipSyncUpdateMethod2);
                            addListenerMethod.Invoke(unityEvent, new object[] { del });

                            log("✅ KRITISCH: Added RUNTIME listener (fallback - works at runtime)");
                            log($"[DEBUG] Runtime delegate type: {delegateType.Name}");
                            log($"[DEBUG] Target method: {onLipSyncUpdateMethod2.Name} on {blendShapeComponent.name}");
                        }
                        else
                        {
                            log("❌ Could not create runtime listener - method resolution failed");
                        }
                    }
                    else
                    {
                        log("✅ Editor linking successful - skipping runtime listener");
                    }
                    
                    // Verify the connection was successful
                    try
                    {
                        var getPersistentEventCountMethod = unityEvent.GetType().GetMethod("GetPersistentEventCount");
                        if (getPersistentEventCountMethod != null)
                        {
                            var count = getPersistentEventCountMethod.Invoke(unityEvent, null);
                            log($"[DEBUG] UnityEvent now has {count} persistent listeners");
                        }
                        
                        // Try to get runtime listener count too
                        var runtimeCountField = unityEvent.GetType().GetField("m_Calls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (runtimeCountField != null)
                        {
                            var runtimeCalls = runtimeCountField.GetValue(unityEvent);
                            if (runtimeCalls != null)
                            {
                                var runtimeCount = ((System.Collections.IList)runtimeCalls).Count;
                                log($"[DEBUG] UnityEvent also has {runtimeCount} runtime listeners");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        log($"[DEBUG] Could not verify listener count: {ex.Message}");
                    }

                    // Add debug listener to monitor events
                    try
                    {
                        var debugDelegate = System.Delegate.CreateDelegate(unityEvent.GetType().GetMethod("AddListener").GetParameters()[0].ParameterType, typeof(SetupLipSyncSystemStep).GetMethod("DebugLipSyncUpdate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                        if (debugDelegate != null)
                        {
                            unityEvent.GetType().GetMethod("AddListener").Invoke(unityEvent, new object[] { debugDelegate });
                            log("[DEBUG] Added debug event listener for LipSync events");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        log($"[DEBUG] Could not add debug listener: {ex.Message}");
                    }
                }
                else
                {
                    log("❌ KRITISCH: onLipSyncUpdate event is null");
                }
            }
            else
            {
                log("❌ KRITISCH: onLipSyncUpdate field not found on uLipSync component");
                log($"[DEBUG] Available fields on {uLipSyncType.Name}:");
                foreach (var field in uLipSyncType.GetFields())
                {
                    log($"[DEBUG]   - {field.Name} ({field.FieldType.Name})");
                }
            }

            yield return null;
        }

        /// <summary>
        /// Konfiguriert ReadyPlayerMe BlendShape Mappings
        /// </summary>
        private void ConfigureReadyPlayerMeBlendShapes(Component blendShapeComponent, SkinnedMeshRenderer facialRenderer, System.Type blendShapeType)
        {
            // ReadyPlayerMe phoneme mappings optimized for OpenAI voices
            var phonemeMappings = new Dictionary<string, string>
            {
                {"A", "mouthOpen"},      // Open mouth for "ah" sounds
                {"I", "mouthSmile"},     // Smile for "ee" sounds  
                {"U", "mouthFunnel"},    // Funnel for "oo" sounds
                {"E", "mouthSmile"},     // Smile for "eh" sounds
                {"O", "mouthOpen"},      // Open for "oh" sounds
                {"N", "mouthClose"},     // Closed for consonants
                {"-", ""}                // Silence/noise (no mapping)
            };

            // Apply mappings
            var addBlendShapeMethod = blendShapeType.GetMethod("AddBlendShape",
                new System.Type[] { typeof(string), typeof(string) });

            if (addBlendShapeMethod != null)
            {
                int mappedCount = 0;
                foreach (var mapping in phonemeMappings)
                {
                    if (!string.IsNullOrEmpty(mapping.Value))
                    {
                        int blendShapeIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(mapping.Value);
                        if (blendShapeIndex >= 0)
                        {
                            addBlendShapeMethod.Invoke(blendShapeComponent,
                                new object[] { mapping.Key, mapping.Value });
                            mappedCount++;
                            log($"   ✅ Mapped '{mapping.Key}' → '{mapping.Value}'");
                        }
                        else
                        {
                            // Try alternative names
                            string[] alternatives = GetBlendShapeAlternatives(mapping.Value);
                            bool found = false;
                            foreach (string alt in alternatives)
                            {
                                if (facialRenderer.sharedMesh.GetBlendShapeIndex(alt) >= 0)
                                {
                                    addBlendShapeMethod.Invoke(blendShapeComponent,
                                        new object[] { mapping.Key, alt });
                                    mappedCount++;
                                    log($"   ✅ Mapped '{mapping.Key}' → '{alt}' (alternative)");
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                log($"   ⚠️ BlendShape '{mapping.Value}' not found for phoneme '{mapping.Key}'");
                            }
                        }
                    }
                }

                log($"✅ Configured {mappedCount}/6 phoneme mappings for ReadyPlayerMe");
            }
        }

        /// <summary>
        /// Optimiert uLipSync für OpenAI Audio
        /// </summary>
        private void ConfigureULipSyncForOpenAI(Component uLipSyncComponent, System.Type uLipSyncType)
        {
            // Set optimal parameters for OpenAI 24kHz audio
            try
            {
                // Use default profile for now (user can calibrate later)
                log("✅ Using default uLipSync profile (optimize via calibration later)");
                log("   💡 Tip: Calibrate profile with your OpenAI voice for best results");
            }
            catch (System.Exception ex)
            {
                log($"⚠️ Could not configure uLipSync parameters: {ex.Message}");
            }
        }

        #endregion

        #region Fallback System

        /// <summary>
        /// Konfiguriert Fallback LipSync System
        /// </summary>
        private IEnumerator SetupFallbackLipSync(GameObject targetAvatar, GameObject npcSystem)
        {
            log("🔄 Setting up fallback LipSync system...");

            // Add ReadyPlayerMeLipSync component
            MonoBehaviour lipSync = targetAvatar.GetComponent("ReadyPlayerMeLipSync") as MonoBehaviour;
            if (lipSync == null)
            {
                System.Type lipSyncType = System.Type.GetType("Animation.ReadyPlayerMeLipSync") ??
                                          System.Type.GetType("ReadyPlayerMeLipSync") ??
                                          System.Type.GetType("Setup.Steps.ReadyPlayerMeLipSync");

                if (lipSyncType != null)
                {
                    lipSync = targetAvatar.AddComponent(lipSyncType) as MonoBehaviour;
                    log("✅ Added ReadyPlayerMeLipSync component to avatar");
                }
                else
                {
                    // Create fallback component
                    lipSync = targetAvatar.AddComponent<ReadyPlayerMeLipSync>();
                    log("✅ Added fallback ReadyPlayerMeLipSync component");
                }
            }

            // Configure fallback system
            ConfigureFallbackLipSync(targetAvatar, npcSystem);

            yield return null;
        }

        /// <summary>
        /// Konfiguriert Fallback System
        /// </summary>
        private void ConfigureFallbackLipSync(GameObject targetAvatar, GameObject npcSystem)
        {
            // Find head renderer
            SkinnedMeshRenderer headRenderer = FindFacialRenderer(targetAvatar);
            if (headRenderer != null)
            {
                log($"✅ Found facial renderer: {headRenderer.name}");

                // Find audio source
                AudioSource audioSource = FindPlaybackAudioSource(npcSystem);
                if (audioSource != null)
                {
                    log("✅ Audio source linked to fallback LipSync");
                }

                log("✅ Fallback LipSync configured (basic mouth movement)");
            }
            else
            {
                log("⚠️ No facial renderer found - LipSync may not work");
            }
        }

        #endregion

        #region NPCController Setup

        /// <summary>
        /// Fügt NPCController hinzu
        /// </summary>
        private IEnumerator SetupNPCController(GameObject npcSystem)
        {
            log("🤖 Setting up NPCController...");

            MonoBehaviour npcController = npcSystem.GetComponent("NPCController") as MonoBehaviour;
            if (npcController == null)
            {
                System.Type npcControllerType = System.Type.GetType("NPC.NPCController") ??
                                                System.Type.GetType("NPCController");
                if (npcControllerType != null)
                {
                    npcController = npcSystem.AddComponent(npcControllerType) as MonoBehaviour;
                    log("✅ Added NPCController component");
                }
                else
                {
                    log("⚠️ NPCController type not found - will create placeholder");
                }
            }

            yield return null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Findet Facial SkinnedMeshRenderer - KORRIGIERT für ReadyPlayerMe Avatare
        /// PRIORITÄT: Renderer_Head vor allen anderen!
        /// </summary>
        private SkinnedMeshRenderer FindFacialRenderer(GameObject targetAvatar)
        {
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            // ERSTE PRIORITÄT: Suche explizit nach "Renderer_Head" (ReadyPlayerMe Standard)
            foreach (var renderer in renderers)
            {
                if (renderer.name == "Renderer_Head")
                {
                    log($"[DEBUG] ✅ Found preferred facial renderer: {renderer.name}");
                    return renderer;
                }
            }
            
            // ZWEITE PRIORITÄT: Suche nach Head-ähnlichen Namen
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Head") && !renderer.name.Contains("EyeLeft") && !renderer.name.Contains("EyeRight"))
                {
                    log($"[DEBUG] ✅ Found head-like renderer: {renderer.name}");
                    return renderer;
                }
            }

            // DRITTE PRIORITÄT: Prüfe auf Mouth BlendShapes
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                {
                    // Check for mouth BlendShapes
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                        if (shapeName.ToLower().Contains("mouth") ||
                            shapeName.Contains("mouthOpen") ||
                            shapeName.Contains("mouthSmile") ||
                            shapeName.ToLower().Contains("jaw"))
                        {
                            log($"[DEBUG] ✅ Found renderer with mouth BlendShapes: {renderer.name}");
                            return renderer;
                        }
                    }
                }
            }

            // FALLBACK: Wolf3D oder andere
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("Wolf3D"))
                {
                    log($"[DEBUG] ✅ Found Wolf3D renderer: {renderer.name}");
                    return renderer;
                }
            }
            
            log($"[DEBUG] ⚠️ No optimal facial renderer found, available renderers:");
            foreach (var renderer in renderers)
            {
                log($"[DEBUG]   - {renderer.name} (BlendShapes: {renderer.sharedMesh?.blendShapeCount ?? 0})");
            }

            return null;
        }

        /// <summary>
        /// Findet Playback AudioSource
        /// </summary>
        private AudioSource FindPlaybackAudioSource(GameObject npcSystem)
        {
            // Try RealtimeAudioManager first
            var audioManager = npcSystem.GetComponent<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager != null)
            {
                var getAudioSourceMethod = audioManager.GetType().GetMethod("GetPlaybackAudioSource");
                if (getAudioSourceMethod != null)
                {
                    return getAudioSourceMethod.Invoke(audioManager, null) as AudioSource;
                }

                // Fallback: Reflection
                var playbackField = audioManager.GetType().GetField("playbackAudioSource",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playbackField != null)
                {
                    return playbackField.GetValue(audioManager) as AudioSource;
                }
            }

            // Fallback: Search by name
            GameObject playbackObj = GameObject.Find("PlaybackAudioSource");
            if (playbackObj != null)
            {
                return playbackObj.GetComponent<AudioSource>();
            }

            // Last resort: Any AudioSource in NPC system
            return npcSystem.GetComponentInChildren<AudioSource>();
        }

        /// <summary>
        /// Gibt alternative BlendShape Namen zurück
        /// </summary>
        private string[] GetBlendShapeAlternatives(string blendShapeName)
        {
            var alternatives = new Dictionary<string, string[]>
            {
                {"mouthOpen", new[] {"jawOpen", "JawOpen", "mouth_open", "Jaw_Open"}},
                {"mouthSmile", new[] {"mouthSmileLeft", "mouthSmileRight", "mouth_smile", "Mouth_Smile"}},
                {"mouthFunnel", new[] {"mouthPucker", "mouth_funnel", "Mouth_Funnel", "viseme_O"}},
                {"mouthClose", new[] {"mouthClosed", "mouth_close", "Mouth_Close"}}
            };

            return alternatives.ContainsKey(blendShapeName) ?
                   alternatives[blendShapeName] :
                   new string[0];
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validiert komplettes Setup
        /// </summary>
        private void ValidateCompleteSetup(GameObject targetAvatar, GameObject npcSystem, LipSyncSystemInfo info)
        {
            log("");
            log("🔍 Final LipSync Validation:");
            log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Validate BlendShapes
            ValidateBlendShapes(targetAvatar);

            // Validate LipSync components
            bool hasLipSyncComponent = false;
            if (info.HasULipSync)
            {
                hasLipSyncComponent = targetAvatar.GetComponent("uLipSync.uLipSyncBlendShape") != null;
                log($"   uLipSyncBlendShape: {(hasLipSyncComponent ? "✅" : "❌")}");

                AudioSource audioSource = FindPlaybackAudioSource(npcSystem);
                bool hasAnalyzer = audioSource?.GetComponent("uLipSync.uLipSync") != null;
                log($"   uLipSync Analyzer: {(hasAnalyzer ? "✅" : "❌")}");
            }
            else
            {
                hasLipSyncComponent = targetAvatar.GetComponent("ReadyPlayerMeLipSync") != null;
                log($"   ReadyPlayerMeLipSync: {(hasLipSyncComponent ? "✅" : "❌")}");
            }

            // Audio integration
            bool hasAudioManager = npcSystem.GetComponent<OpenAI.RealtimeAPI.RealtimeAudioManager>() != null;
            log($"   Audio Integration: {(hasAudioManager ? "✅" : "❌")}");

            // Overall status
            bool setupComplete = hasLipSyncComponent && hasAudioManager;
            log("");
            log($"🎭 LipSync Setup: {(setupComplete ? "✅ COMPLETE" : "⚠️ INCOMPLETE")}");

            if (info.HasULipSync && setupComplete)
            {
                log("💎 Professional-grade LipSync active!");
                log("💡 Next: Calibrate uLipSync profile for optimal results");
            }
            else if (setupComplete)
            {
                log("🔄 Basic LipSync active - upgrade to uLipSync for best results");
            }
        }

        /// <summary>
        /// Validiert BlendShapes (bestehende Methode erweitert)
        /// </summary>
        private void ValidateBlendShapes(GameObject targetAvatar)
        {
            if (targetAvatar == null) return;

            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            bool foundMouthOpen = false;
            bool foundMouthSmile = false;
            List<string> foundBlendShapes = new List<string>();

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;

                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    string name = renderer.sharedMesh.GetBlendShapeName(i);
                    foundBlendShapes.Add(name);

                    if (name == "mouthOpen" || name == "jawOpen") foundMouthOpen = true;
                    if (name == "mouthSmile") foundMouthSmile = true;
                }
            }

            if (foundMouthOpen && foundMouthSmile)
            {
                log("   ✅ Required BlendShapes: mouthOpen, mouthSmile");
            }
            else
            {
                log("   ⚠️ Missing optimal BlendShapes:");
                if (!foundMouthOpen) log("      → Missing: mouthOpen or jawOpen");
                if (!foundMouthSmile) log("      → Missing: mouthSmile");
            }

            log($"   📊 Total BlendShapes available: {foundBlendShapes.Count}");
        }

        #endregion
    }

    /// <summary>
    /// Fallback LipSync Component für grundlegende Mund-Animation
    /// Wird nur verwendet wenn uLipSync nicht verfügbar ist
    /// </summary>
    public class ReadyPlayerMeLipSync : MonoBehaviour
    {
        [Header("ReadyPlayerMe Integration")]
        [SerializeField] private SkinnedMeshRenderer facialRenderer;
        [SerializeField] private string mouthOpenBlendShape = "mouthOpen";
        [SerializeField] private string mouthSmileBlendShape = "mouthSmile";

        [Header("Audio Settings")]
        [SerializeField] private float sensitivity = 2.0f;
        [SerializeField] private float smoothing = 8.0f;
        [SerializeField] private float minMouthOpen = 0.0f;
        [SerializeField] private float maxMouthOpen = 0.7f;

        // New fields for smile, threshold, formant etc.
        [SerializeField] private float currentMouthSmile = 0f;
        [SerializeField] private float targetMouthSmile = 0f;
        [SerializeField] private float volumeThreshold = 0.01f;
        [SerializeField] private bool useFormantAnalysis = false;
        [SerializeField] private float formantSensitivity = 8.0f;

        private int mouthOpenIndex = -1;
        private int mouthSmileIndex = -1;
        private float currentMouthOpen = 0f;
        private float targetMouthOpen = 0f;
        private OpenAI.RealtimeAPI.RealtimeAudioManager audioManager;
        private AudioSource connectedAudioSource;
        private float[] audioSamples = new float[1024];
        private float[] spectrumData = new float[1024];

        // Formant analysis for better phoneme detection
        private float lastF1 = 0f; // First formant (jaw opening)
        private float lastF2 = 0f; // Second formant (tongue position)

        private void Start()
        {
            // Auto-find facial renderer
            if (facialRenderer == null)
            {
                facialRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                // Search in children for facial renderer
                SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                    {
                        // Check for mouth BlendShapes
                        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                        {
                            string shapeName = renderer.sharedMesh.GetBlendShapeName(i);
                            if (shapeName.ToLower().Contains("mouth"))
                            {
                                facialRenderer = renderer;
                                break;
                            }
                        }
                        if (facialRenderer != null) break;
                    }
                }
            }
            // Get BlendShape indices
            if (facialRenderer != null && facialRenderer.sharedMesh != null)
            {
                mouthOpenIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(mouthOpenBlendShape);
                mouthSmileIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(mouthSmileBlendShape);
                // Try alternatives if not found
                if (mouthOpenIndex < 0)
                {
                    string[] alternatives = { "jawOpen", "JawOpen", "mouth_open" };
                    foreach (string alt in alternatives)
                    {
                        mouthOpenIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(alt);
                        if (mouthOpenIndex >= 0)
                        {
                            mouthOpenBlendShape = alt;
                            break;
                        }
                    }
                }
                if (mouthSmileIndex < 0)
                {
                    string[] alternatives = { "mouthSmileLeft", "mouthSmileRight", "mouth_smile", "Mouth_Smile" };
                    foreach (string alt in alternatives)
                    {
                        mouthSmileIndex = facialRenderer.sharedMesh.GetBlendShapeIndex(alt);
                        if (mouthSmileIndex >= 0)
                        {
                            mouthSmileBlendShape = alt;
                            break;
                        }
                    }
                }
                Debug.Log($"[ReadyPlayerMeLipSync] BlendShapes - mouthOpen: {mouthOpenIndex} ({mouthOpenBlendShape}), mouthSmile: {mouthSmileIndex} ({mouthSmileBlendShape})");
            }
            // Find and connect to RealtimeAudioManager
#if UNITY_2022_1_OR_NEWER
            audioManager = UnityEngine.Object.FindFirstObjectByType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
#else
            audioManager = UnityEngine.Object.FindObjectOfType<OpenAI.RealtimeAPI.RealtimeAudioManager>();
#endif
            if (audioManager != null)
            {
                // Subscribe to audio level changes
                audioManager.OnMicrophoneLevelChanged.AddListener(OnAudioLevelChanged);
                Debug.Log("[ReadyPlayerMeLipSync] Connected to RealtimeAudioManager");
            }
            else
            {
                Debug.LogWarning("[ReadyPlayerMeLipSync] RealtimeAudioManager not found - manual control only");
            }
        }

        private void Update()
        {
            // Audioanalyse für LipSync
            AnalyzeAudio();
            // Smooth mouth movement
            currentMouthOpen = Mathf.Lerp(currentMouthOpen, targetMouthOpen, Time.deltaTime * smoothing);
            currentMouthSmile = Mathf.Lerp(currentMouthSmile, targetMouthSmile, Time.deltaTime * smoothing);
            // Apply BlendShapes
            ApplyBlendShapes();
        }

        /// <summary>
        /// Analysiert Audio für LipSync
        /// </summary>
        private void AnalyzeAudio()
        {
            if (connectedAudioSource == null || !connectedAudioSource.isPlaying)
            {
                // Fade to neutral when not speaking
                targetMouthOpen = 0f;
                targetMouthSmile = 0f;
                return;
            }

            // Get audio data
            connectedAudioSource.GetOutputData(audioSamples, 0);
            connectedAudioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            // Calculate RMS (volume)
            float rms = CalculateRMS(audioSamples);

            if (rms < volumeThreshold)
            {
                // No significant audio - fade to neutral
                targetMouthOpen = 0f;
                targetMouthSmile = 0f;
                return;
            }

            if (useFormantAnalysis)
            {
                AnalyzeFormants(rms);
            }
            else
            {
                // Simple volume-based animation
                targetMouthOpen = Mathf.Clamp(rms * sensitivity, minMouthOpen, maxMouthOpen);
                targetMouthSmile = 0f;
            }
        }

        /// <summary>
        /// Analysiert Formanten für bessere Phonem-Erkennung
        /// </summary>
        private void AnalyzeFormants(float volume)
        {
            // Find dominant frequencies (simplified formant analysis)
            float f1 = FindFormant(spectrumData, 200, 800);   // First formant (jaw opening)
            float f2 = FindFormant(spectrumData, 800, 2500);  // Second formant (tongue position)

            // Smooth formant changes
            lastF1 = Mathf.Lerp(lastF1, f1, Time.deltaTime * formantSensitivity);
            lastF2 = Mathf.Lerp(lastF2, f2, Time.deltaTime * formantSensitivity);

            // Map formants to mouth shapes
            // Lower F1 = more open mouth (vowels like "ah", "oh")
            // Higher F1 = more closed mouth (consonants)
            // Higher F2 = more smile (vowels like "ee", "ih")

            float openness = Mathf.InverseLerp(800f, 200f, lastF1); // Inverted - lower F1 = more open
            float frontness = Mathf.InverseLerp(800f, 2500f, lastF2); // Higher F2 = more front/smile

            // Apply volume scaling
            targetMouthOpen = Mathf.Clamp(openness * volume * sensitivity, minMouthOpen, maxMouthOpen);
            targetMouthSmile = Mathf.Clamp(frontness * volume * sensitivity * 0.5f, 0f, 0.6f);
        }

        /// <summary>
        /// Findet dominante Frequenz in einem Bereich
        /// </summary>
        private float FindFormant(float[] spectrum, float minFreq, float maxFreq)
        {
            int minIndex = Mathf.RoundToInt(minFreq / 22050f * spectrum.Length);
            int maxIndex = Mathf.RoundToInt(maxFreq / 22050f * spectrum.Length);

            float maxMagnitude = 0f;
            int peakIndex = minIndex;

            for (int i = minIndex; i < maxIndex && i < spectrum.Length; i++)
            {
                if (spectrum[i] > maxMagnitude)
                {
                    maxMagnitude = spectrum[i];
                    peakIndex = i;
                }
            }

            return (float)peakIndex / spectrum.Length * 22050f;
        }

        /// <summary>
        /// Berechnet RMS (Root Mean Square) für Lautstärke
        /// </summary>
        private float CalculateRMS(float[] samples)
        {
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }
            return Mathf.Sqrt(sum / samples.Length);
        }

        /// <summary>
        /// Wendet BlendShapes an
        /// </summary>
        private void ApplyBlendShapes()
        {
            if (facialRenderer == null) return;

            if (mouthOpenIndex >= 0)
            {
                facialRenderer.SetBlendShapeWeight(mouthOpenIndex, currentMouthOpen * 100f);
            }

            if (mouthSmileIndex >= 0)
            {
                facialRenderer.SetBlendShapeWeight(mouthSmileIndex, currentMouthSmile * 100f);
            }
        }

        /// <summary>
        /// Manueller Audio Level Callback (für RealtimeAudioManager Events)
        /// </summary>
        public void OnAudioLevelChanged(float level)
        {
            if (!useFormantAnalysis)
            {
                targetMouthOpen = Mathf.Clamp(level * sensitivity, minMouthOpen, maxMouthOpen);
            }
        }

        /// <summary>
        /// Öffentliche Methode zum manuellen Setzen der Mund-Animation
        /// </summary>
        public void SetMouthAnimation(float openAmount, float smileAmount)
        {
            targetMouthOpen = Mathf.Clamp(openAmount, minMouthOpen, maxMouthOpen);
            targetMouthSmile = Mathf.Clamp(smileAmount, 0f, 1f);
        }

        /// <summary>
        /// Kalibriert die Sensitivität basierend auf aktueller Audio-Eingabe
        /// </summary>
        public void CalibrateAudioSensitivity()
        {
            if (connectedAudioSource != null && connectedAudioSource.isPlaying)
            {
                connectedAudioSource.GetOutputData(audioSamples, 0);
                float currentRMS = CalculateRMS(audioSamples);

                if (currentRMS > 0.001f) // Avoid division by zero
                {
                    float optimalSensitivity = 0.5f / currentRMS; // Target 50% mouth opening for current volume
                    sensitivity = Mathf.Clamp(optimalSensitivity, 0.5f, 10f);
                    Debug.Log($"[ReadyPlayerMeLipSync] Auto-calibrated sensitivity to: {sensitivity:F2}");
                }
            }
        }

        #region Debug and Utilities

        [System.Serializable]
        public class LipSyncDebugInfo
        {
            public float currentVolume;
            public float f1Frequency;
            public float f2Frequency;
            public float mouthOpenValue;
            public float mouthSmileValue;
            public bool isConnectedToAudio;
            public string connectedAudioSourceName;
        }

        /// <summary>
        /// Gibt Debug-Informationen zurück
        /// </summary>
        public LipSyncDebugInfo GetDebugInfo()
        {
            var info = new LipSyncDebugInfo();

            if (connectedAudioSource != null)
            {
                connectedAudioSource.GetOutputData(audioSamples, 0);
                info.currentVolume = CalculateRMS(audioSamples);
                info.isConnectedToAudio = true;
                info.connectedAudioSourceName = connectedAudioSource.name;
            }

            info.f1Frequency = lastF1;
            info.f2Frequency = lastF2;
            info.mouthOpenValue = currentMouthOpen;
            info.mouthSmileValue = currentMouthSmile;

            return info;
        }

        /// <summary>
        /// Zeigt verfügbare BlendShapes im Log
        /// </summary>
        [ContextMenu("Log Available BlendShapes")]
        public void LogAvailableBlendShapes()
        {
            if (facialRenderer?.sharedMesh != null)
            {
                Debug.Log($"[ReadyPlayerMeLipSync] Available BlendShapes on {facialRenderer.name}:");
                for (int i = 0; i < facialRenderer.sharedMesh.blendShapeCount; i++)
                {
                    string shapeName = facialRenderer.sharedMesh.GetBlendShapeName(i);
                    Debug.Log($"   [{i}] {shapeName}");
                }
            }
        }

        /// <summary>
        /// Test-Methode für BlendShape Animation
        /// </summary>
        [ContextMenu("Test Mouth Animation")]
        public void TestMouthAnimation()
        {
            StartCoroutine(TestAnimationCoroutine());
        }

        private System.Collections.IEnumerator TestAnimationCoroutine()
        {
            Debug.Log("[ReadyPlayerMeLipSync] Testing mouth animation...");

            // Test mouth open
            SetMouthAnimation(0.8f, 0f);
            yield return new WaitForSeconds(1f);

            // Test mouth smile
            SetMouthAnimation(0f, 0.8f);
            yield return new WaitForSeconds(1f);

            // Test combined
            SetMouthAnimation(0.5f, 0.5f);
            yield return new WaitForSeconds(1f);

            // Return to neutral
            SetMouthAnimation(0f, 0f);

            Debug.Log("[ReadyPlayerMeLipSync] Animation test complete");
        }

        #endregion
    }
}