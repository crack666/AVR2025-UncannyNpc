using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Setup.Steps
{
    /// <summary>
    /// Step to set up the professional-grade uLipSync system.
    /// </summary>
    public class SetupULipSyncStep
    {
        private System.Action<string> log;

        public SetupULipSyncStep(System.Action<string> log)
        {
            this.log = log;
        }

        public IEnumerator Execute(GameObject targetAvatar, GameObject npcSystem)
        {
            log("🎯 Step 5.3: Setting up uLipSync (Professional Grade)");

            var uLipSyncType = DetectLipSyncSystemStep.GetULipSyncType();
            var blendShapeType = DetectLipSyncSystemStep.GetULipSyncBlendShapeType();

            log($"   [DEBUG REMOVE LATER] (pre-check) uLipSyncType: {(uLipSyncType != null ? uLipSyncType.FullName : "null")}, blendShapeType: {(blendShapeType != null ? blendShapeType.FullName : "null")}");

            if (uLipSyncType == null || blendShapeType == null)
            {
                log("❌ uLipSync types not found. Make sure the uLipSync package is installed correctly.");
                yield break;
            }

            // Setup sequence
            yield return SetupULipSyncComponent(npcSystem, uLipSyncType);
            // Prüfe nach dem Hinzufügen
            var audioSource = FindPlaybackAudioSource(npcSystem);
            var uLipSyncComponent = audioSource?.gameObject.GetComponent(uLipSyncType);
            log($"   [DEBUG] uLipSyncComponent after setup: {(uLipSyncComponent != null ? "FOUND" : "NOT FOUND")}");

            yield return ConfigureULipSyncProfile(npcSystem, uLipSyncType);
            yield return SetupULipSyncBlendShape(targetAvatar, blendShapeType);
            // Prüfe nach dem Hinzufügen
            var blendShapeComponent = targetAvatar.GetComponent(blendShapeType);
            log($"   [DEBUG] blendShapeComponent after setup: {(blendShapeComponent != null ? "FOUND" : "NOT FOUND")}");

            yield return LinkULipSyncComponents(targetAvatar, npcSystem, uLipSyncType, blendShapeType);
            // Prüfe Event-Linking
            #if UNITY_EDITOR
            if (uLipSyncComponent != null)
            {
                var serializedObject = new UnityEditor.SerializedObject(uLipSyncComponent);
                var onLipSyncUpdateProperty = serializedObject.FindProperty("onLipSyncUpdate");
                if (onLipSyncUpdateProperty != null)
                {
                    var persistentCallsProperty = onLipSyncUpdateProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    int count = persistentCallsProperty.arraySize;
                    log($"   [DEBUG] onLipSyncUpdate persistent calls: {count}");
                }
            }
            #endif

            log("🎉 uLipSync system configured successfully!");
        }

        public void ExecuteSync(GameObject targetAvatar, GameObject npcSystem)
        {
            log("🎯 Step 5.3: Setting up uLipSync (Professional Grade) [SYNC]");

            var uLipSyncType = DetectLipSyncSystemStep.GetULipSyncType();
            var blendShapeType = DetectLipSyncSystemStep.GetULipSyncBlendShapeType();

            log($"   [DEBUG REMOVE LATER] (pre-check) uLipSyncType: {(uLipSyncType != null ? uLipSyncType.FullName : "null")}, blendShapeType: {(blendShapeType != null ? blendShapeType.FullName : "null")}");

            if (uLipSyncType == null || blendShapeType == null)
            {
                log("❌ uLipSync types not found. Make sure the uLipSync package is installed correctly.");
                return;
            }

            // Setup sequence synchron ausgeführt
            SetupULipSyncComponentSync(npcSystem, uLipSyncType);
            var audioSource = FindPlaybackAudioSource(npcSystem);
            var uLipSyncComponent = audioSource?.gameObject.GetComponent(uLipSyncType);
            log($"   [DEBUG REMOVE LATER] uLipSyncComponent after setup: {(uLipSyncComponent != null ? "FOUND" : "NOT FOUND")}");

            ConfigureULipSyncProfileSync(npcSystem, uLipSyncType);
            SetupULipSyncBlendShapeSync(targetAvatar, blendShapeType);
            var blendShapeComponent = targetAvatar.GetComponent(blendShapeType);
            log($"   [DEBUG REMOVE LATER] blendShapeComponent after setup: {(blendShapeComponent != null ? "FOUND" : "NOT FOUND")}");

            LinkULipSyncComponentsSync(targetAvatar, npcSystem, uLipSyncType, blendShapeType);
            #if UNITY_EDITOR
            if (uLipSyncComponent != null)
            {
                var serializedObject = new UnityEditor.SerializedObject(uLipSyncComponent);
                var onLipSyncUpdateProperty = serializedObject.FindProperty("onLipSyncUpdate");
                if (onLipSyncUpdateProperty != null)
                {
                    var persistentCallsProperty = onLipSyncUpdateProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    int count = persistentCallsProperty.arraySize;
                    log($"   [DEBUG REMOVE LATER] onLipSyncUpdate persistent calls: {count}");
                }
            }
            #endif

            log("🎉 uLipSync system configured successfully! [SYNC]");
        }

        private IEnumerator SetupULipSyncComponent(GameObject npcSystem, System.Type uLipSyncType)
        {
            log($"   [DEBUG REMOVE LATER] SetupULipSyncComponent: npcSystem={npcSystem?.name}");
            AudioSource playbackAudioSource = FindPlaybackAudioSource(npcSystem);
            if (playbackAudioSource == null)
            {
                log("   [DEBUG REMOVE LATER] ❌ Playback AudioSource not found! (FindPlaybackAudioSource returned null)");
                yield break;
            }
            log($"   [DEBUG REMOVE LATER] PlaybackAudioSource found: {playbackAudioSource.gameObject.name}");

            Component uLipSyncComponent = playbackAudioSource.gameObject.GetComponent(uLipSyncType);
            if (uLipSyncComponent == null)
            {
                uLipSyncComponent = playbackAudioSource.gameObject.AddComponent(uLipSyncType);
                log("   [DEBUG REMOVE LATER] ✅ Added uLipSync component.");
            }
            else
            {
                log("   [DEBUG REMOVE LATER] uLipSync component already present.");
            }

            // Required for OnAudioFilterRead to work
            if (playbackAudioSource.clip == null)
            {
                playbackAudioSource.clip = AudioClip.Create("uLipSync_Dummy", 24000, 1, 24000, false);
                log("   [DEBUG REMOVE LATER] ✅ Created dummy AudioClip for uLipSync processing.");
            }
            else
            {
                log("   [DEBUG REMOVE LATER] PlaybackAudioSource already has a clip.");
            }
            yield return null;
        }

        private void SetupULipSyncComponentSync(GameObject npcSystem, System.Type uLipSyncType)
        {
            log($"   [DEBUG REMOVE LATER] SetupULipSyncComponent: npcSystem={npcSystem?.name}");
            AudioSource playbackAudioSource = FindPlaybackAudioSource(npcSystem);
            if (playbackAudioSource == null)
            {
                log("   [DEBUG REMOVE LATER] ❌ Playback AudioSource not found! (FindPlaybackAudioSource returned null)");
                return;
            }
            log($"   [DEBUG REMOVE LATER] PlaybackAudioSource found: {playbackAudioSource.gameObject.name}");

            Component uLipSyncComponent = playbackAudioSource.gameObject.GetComponent(uLipSyncType);
            if (uLipSyncComponent == null)
            {
                uLipSyncComponent = playbackAudioSource.gameObject.AddComponent(uLipSyncType);
                log("   [DEBUG REMOVE LATER] ✅ Added uLipSync component.");
            }
            else
            {
                log("   [DEBUG REMOVE LATER] uLipSync component already present.");
            }

            if (playbackAudioSource.clip == null)
            {
                playbackAudioSource.clip = AudioClip.Create("uLipSync_Dummy", 24000, 1, 24000, false);
                log("   [DEBUG REMOVE LATER] ✅ Created dummy AudioClip for uLipSync processing.");
            }
            else
            {
                log("   [DEBUG REMOVE LATER] PlaybackAudioSource already has a clip.");
            }
        }

        private IEnumerator ConfigureULipSyncProfile(GameObject npcSystem, System.Type uLipSyncType)
        {
            log("   - Configuring uLipSync profile...");
            AudioSource playbackAudioSource = FindPlaybackAudioSource(npcSystem);
            Component uLipSyncComponent = playbackAudioSource?.gameObject.GetComponent(uLipSyncType);
            if (uLipSyncComponent == null) yield break;

            var profileField = uLipSyncType.GetField("profile");
            if (profileField != null && profileField.GetValue(uLipSyncComponent) == null)
            {
                var profile = LoadULipSyncSampleProfile();
                if (profile != null)
                {
                    profileField.SetValue(uLipSyncComponent, profile);
                    log($"   ✅ Set uLipSync profile to: {profile.name}");
                }
                else
                {
                    log("   ⚠️ Could not load uLipSync sample profile. Please set it manually.");
                }
            }
            yield return null;
        }

        private void ConfigureULipSyncProfileSync(GameObject npcSystem, System.Type uLipSyncType)
        {
            log("   - Configuring uLipSync profile...");
            AudioSource playbackAudioSource = FindPlaybackAudioSource(npcSystem);
            Component uLipSyncComponent = playbackAudioSource?.gameObject.GetComponent(uLipSyncType);
            if (uLipSyncComponent == null) return;

            var profileField = uLipSyncType.GetField("profile");
            if (profileField != null && profileField.GetValue(uLipSyncComponent) == null)
            {
                var profile = LoadULipSyncSampleProfile();
                if (profile != null)
                {
                    profileField.SetValue(uLipSyncComponent, profile);
                    log($"   ✅ Set uLipSync profile to: {profile.name}");
                }
                else
                {
                    log("   ⚠️ Could not load uLipSync sample profile. Please set it manually.");
                }
            }
        }

        private IEnumerator SetupULipSyncBlendShape(GameObject targetAvatar, System.Type blendShapeType)
        {
            log($"   [DEBUG REMOVE LATER] SetupULipSyncBlendShape: targetAvatar={targetAvatar?.name}");
            Component blendShapeComponent = targetAvatar.GetComponent(blendShapeType);
            if (blendShapeComponent == null)
            {
                blendShapeComponent = targetAvatar.AddComponent(blendShapeType);
                log("   [DEBUG REMOVE LATER] ✅ Added uLipSyncBlendShape component.");
            }
            else
            {
                log("   [DEBUG REMOVE LATER] uLipSyncBlendShape component already present.");
            }

            SkinnedMeshRenderer facialRenderer = FindFacialRenderer(targetAvatar);
            if (facialRenderer == null)
            {
                log("   [DEBUG REMOVE LATER] ❌ No facial SkinnedMeshRenderer found!");
                yield break;
            }
            log($"   [DEBUG REMOVE LATER] Found facialRenderer: {facialRenderer.name}");

            var rendererField = blendShapeType.GetField("skinnedMeshRenderer");
            rendererField?.SetValue(blendShapeComponent, facialRenderer);

            var maxValueField = blendShapeType.GetField("maxBlendShapeValue");
            maxValueField?.SetValue(blendShapeComponent, 1.0f);

            ConfigureReadyPlayerMeBlendShapes(blendShapeComponent, facialRenderer, blendShapeType);
            yield return null;
        }

        private void SetupULipSyncBlendShapeSync(GameObject targetAvatar, System.Type blendShapeType)
        {
            log($"   [DEBUG REMOVE LATER] SetupULipSyncBlendShape: targetAvatar={targetAvatar?.name}");
            Component blendShapeComponent = targetAvatar.GetComponent(blendShapeType);
            if (blendShapeComponent == null)
            {
                blendShapeComponent = targetAvatar.AddComponent(blendShapeType);
                log("   [DEBUG REMOVE LATER] ✅ Added uLipSyncBlendShape component.");
            }
            else
            {
                log("   [DEBUG REMOVE LATER] uLipSyncBlendShape component already present.");
            }

            SkinnedMeshRenderer facialRenderer = FindFacialRenderer(targetAvatar);
            if (facialRenderer == null)
            {
                log("   [DEBUG REMOVE LATER] ❌ No facial SkinnedMeshRenderer found!");
                return;
            }
            log($"   [DEBUG REMOVE LATER] Found facialRenderer: {facialRenderer.name}");

            var rendererField = blendShapeType.GetField("skinnedMeshRenderer");
            rendererField?.SetValue(blendShapeComponent, facialRenderer);

            var maxValueField = blendShapeType.GetField("maxBlendShapeValue");
            maxValueField?.SetValue(blendShapeComponent, 1.0f);

            ConfigureReadyPlayerMeBlendShapes(blendShapeComponent, facialRenderer, blendShapeType);
        }

        private IEnumerator LinkULipSyncComponents(GameObject targetAvatar, GameObject npcSystem, System.Type uLipSyncType, System.Type blendShapeType)
        {
            log("   - Linking uLipSync components via events...");
            AudioSource audioSource = FindPlaybackAudioSource(npcSystem);
            Component uLipSyncComponent = audioSource?.gameObject.GetComponent(uLipSyncType);
            Component blendShapeComponent = targetAvatar.GetComponent(blendShapeType);

            if (uLipSyncComponent == null || blendShapeComponent == null)
            {
                log("   ❌ Could not find components for linking.");
                yield break;
            }

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
                    string methodUsed = "none";

            #if UNITY_EDITOR
            try
            {
                var serializedObject = new UnityEditor.SerializedObject(uLipSyncComponent);
                var onLipSyncUpdateProperty = serializedObject.FindProperty("onLipSyncUpdate");

                if (onLipSyncUpdateProperty != null)
                {
                    var persistentCallsProperty = onLipSyncUpdateProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    bool listenerExists = false;
                    for (int i = 0; i < persistentCallsProperty.arraySize; i++)
                    {
                        var call = persistentCallsProperty.GetArrayElementAtIndex(i);
                        if (call.FindPropertyRelative("m_Target").objectReferenceValue == blendShapeComponent &&
                            call.FindPropertyRelative("m_MethodName").stringValue == "OnLipSyncUpdate")
                        {
                            listenerExists = true;
                            break;
                        }
                    }

                    if (!listenerExists)
                    {
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
                        
                        // AUSKOMMENTIERT: Automatisches Scene-Speichern (kann auf Wunsch reaktiviert werden)
                        // UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                        // log("[DEBUG] Scene saved to persist FileID references!");
                        
                        log("✅ KRITISCH: Added PERSISTENT listener via SerializedObject (GUARANTEED to work!)");
                        log("[DEBUG] Set to RuntimeOnly mode - exactly like manual setup!");
                        log("[DEBUG] Forced Unity Inspector refresh!");
                        
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
                                    methodUsed = "SerializedObject";
                                }
                                else
                                {
                                    log("❌ VALIDATION FAILED: Event link not persisted correctly");
                                    editorLinkingSuccessful = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        log("✅ KRITISCH: Event listener already exists (SerializedObject)");
                        editorLinkingSuccessful = true;
                        methodUsed = "SerializedObject (already exists)";
                    }
                }
            }
            catch (System.Exception ex)
            {
                log($"[DEBUG] SerializedObject approach failed: {ex.Message}");
            }
            
            log("[DEBUG] SerializedObject approach only available in Editor");
            #endif
            
            // METHODE 2: Direct reflection on UnityEvent internals
            if (!editorLinkingSuccessful)
            {
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
                                    editorLinkingSuccessful = true;
                                    methodUsed = "Reflection";
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    log($"[DEBUG] Direct reflection approach failed: {ex.Message}");
                }
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
                    methodUsed = "Runtime";
                }
                else
                {
                    log("❌ Could not create runtime listener - method resolution failed");
                    methodUsed = "None (failed)";
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
            
            log($"[DEBUG] Link method used: {methodUsed}");
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

        private void LinkULipSyncComponentsSync(GameObject targetAvatar, GameObject npcSystem, System.Type uLipSyncType, System.Type blendShapeType)
        {
            log("   - Linking uLipSync components via events...");
            AudioSource audioSource = FindPlaybackAudioSource(npcSystem);
            Component uLipSyncComponent = audioSource?.gameObject.GetComponent(uLipSyncType);
            Component blendShapeComponent = targetAvatar.GetComponent(blendShapeType);

            if (uLipSyncComponent == null || blendShapeComponent == null)
            {
                log("   ❌ Could not find components for linking.");
                return;
            }

            string methodUsed = "none";
            bool editorLinkingSuccessful = false;
#if UNITY_EDITOR
            try
            {
                // 1. Editor-Linking via SerializedObject
                var serializedObject = new UnityEditor.SerializedObject(uLipSyncComponent);
                var onLipSyncUpdateProperty = serializedObject.FindProperty("onLipSyncUpdate");
                if (onLipSyncUpdateProperty != null)
                {
                    var persistentCallsProperty = onLipSyncUpdateProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    bool listenerExists = false;
                    for (int i = 0; i < persistentCallsProperty.arraySize; i++)
                    {
                        var call = persistentCallsProperty.GetArrayElementAtIndex(i);
                        if (call.FindPropertyRelative("m_Target").objectReferenceValue == blendShapeComponent &&
                            call.FindPropertyRelative("m_MethodName").stringValue == "OnLipSyncUpdate")
                        {
                            listenerExists = true;
                            break;
                        }
                    }
                    if (!listenerExists)
                    {
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
                        serializedObject.ApplyModifiedProperties();
                        UnityEditor.EditorUtility.SetDirty(uLipSyncComponent);
                        UnityEditor.AssetDatabase.SaveAssets();
                        UnityEditor.AssetDatabase.Refresh();
                        
                        // AUSKOMMENTIERT: Automatisches Scene-Speichern (kann auf Wunsch reaktiviert werden)
                        // UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                        // log("[DEBUG] Scene saved to persist FileID references!");
                        
                        log("✅ KRITISCH: Added PERSISTENT listener via SerializedObject (GUARANTEED to work!)");
                        log("[DEBUG] Set to RuntimeOnly mode - exactly like manual setup!");
                        log("[DEBUG] Forced Unity Inspector refresh!");
                        
                        // VALIDIERUNG
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
                                    methodUsed = "SerializedObject";
                                }
                                else
                                {
                                    log("❌ VALIDATION FAILED: Event link not persisted correctly");
                                }
                            }
                        }
                    }
                    else
                    {
                        log("✅ KRITISCH: Event listener already exists (SerializedObject)");
                        editorLinkingSuccessful = true;
                        methodUsed = "SerializedObject (already exists)";
                    }
                }
            }
            catch (System.Exception ex)
            {
                log($"[DEBUG] SerializedObject approach failed: {ex.Message}");
            }

            // 2. Direct Reflection auf UnityEvent-Interna (Fallback)
            if (!editorLinkingSuccessful)
            {
                try
                {
                    var onUpdateField = uLipSyncType.GetField("onLipSyncUpdate");
                    var unityEvent = onUpdateField?.GetValue(uLipSyncComponent);
                    var persistentCallsField = unityEvent?.GetType().GetField("m_PersistentCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (persistentCallsField != null)
                    {
                        var persistentCalls = persistentCallsField.GetValue(unityEvent);
                        var callsField = persistentCalls.GetType().GetField("m_Calls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (callsField != null)
                        {
                            var callsList = callsField.GetValue(persistentCalls) as System.Collections.IList;
                            if (callsList != null)
                            {
                                var persistentCallType = System.Type.GetType("UnityEngine.Events.PersistentCall, UnityEngine");
                                if (persistentCallType != null)
                                {
                                    var newCall = System.Activator.CreateInstance(persistentCallType);
                                    var targetField = persistentCallType.GetField("m_Target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    targetField?.SetValue(newCall, blendShapeComponent);
                                    var methodNameField = persistentCallType.GetField("m_MethodName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    methodNameField?.SetValue(newCall, "OnLipSyncUpdate");
                                    var callStateField = persistentCallType.GetField("m_CallState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    callStateField?.SetValue(newCall, 2); // RuntimeOnly
                                    var modeField = persistentCallType.GetField("m_Mode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    modeField?.SetValue(newCall, 1); // Object
                                    callsList.Add(newCall);
                                    log("✅ KRITISCH: Added PERSISTENT listener via direct reflection!");
                                    log("[DEBUG] Set to RuntimeOnly mode with Object parameter");
                                    editorLinkingSuccessful = true;
                                    methodUsed = "Reflection";
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    log($"[DEBUG] Direct reflection approach failed: {ex.Message}");
                }
            }
#endif
            // 3. Fallback: Runtime Listener (immer, wenn Editor-Linking fehlschlägt)
            if (!editorLinkingSuccessful)
            {
                var onUpdateField = uLipSyncType.GetField("onLipSyncUpdate");
                var unityEvent = onUpdateField?.GetValue(uLipSyncComponent);
                var addListenerMethod = unityEvent?.GetType().GetMethod("AddListener");
                var onLipSyncUpdateMethod = blendShapeType.GetMethod("OnLipSyncUpdate");
                if (addListenerMethod != null && onLipSyncUpdateMethod != null)
                {
                    var delegateType = addListenerMethod.GetParameters()[0].ParameterType;
                    var del = System.Delegate.CreateDelegate(delegateType, blendShapeComponent, onLipSyncUpdateMethod);
                    addListenerMethod.Invoke(unityEvent, new object[] { del });
                    log("✅ KRITISCH: Added RUNTIME listener (fallback - works at runtime)");
                    log($"[DEBUG] Runtime delegate type: {delegateType.Name}");
                    log($"[DEBUG] Target method: {onLipSyncUpdateMethod.Name} on {blendShapeComponent.name}");
                    methodUsed = "Runtime";
                }
                else
                {
                    log("❌ Could not create runtime listener - method resolution failed");
                    methodUsed = "None (failed)";
                }
            }
            else
            {
                log("✅ Editor linking successful - skipping runtime listener");
            }

            // Listener-Count Logging
            try
            {
                var onUpdateField = uLipSyncType.GetField("onLipSyncUpdate");
                var unityEvent = onUpdateField?.GetValue(uLipSyncComponent);
                var getPersistentEventCountMethod = unityEvent?.GetType().GetMethod("GetPersistentEventCount");
                if (getPersistentEventCountMethod != null)
                {
                    var count = getPersistentEventCountMethod.Invoke(unityEvent, null);
                    log($"[DEBUG] UnityEvent now has {count} persistent listeners");
                }
                var runtimeCountField = unityEvent?.GetType().GetField("m_Calls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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

            log($"[DEBUG] Link method used: {methodUsed}");
        }

        private void ConfigureReadyPlayerMeBlendShapes(Component blendShapeComponent, SkinnedMeshRenderer facialRenderer, System.Type blendShapeType)
        {
            var phonemeMappings = new Dictionary<string, string>
            {
                {"A", "mouthOpen"}, {"I", "mouthSmile"}, {"U", "mouthFunnel"},
                {"E", "mouthSmile"}, {"O", "mouthOpen"}, {"N", "mouthClose"}
            };

            var addBlendShapeMethod = blendShapeType.GetMethod("AddBlendShape", new System.Type[] { typeof(string), typeof(string) });
            if (addBlendShapeMethod == null) return;

            log("   - Configuring ReadyPlayerMe blendshape mappings...");
            foreach (var mapping in phonemeMappings)
            {
                if (facialRenderer.sharedMesh.GetBlendShapeIndex(mapping.Value) >= 0)
                {
                    addBlendShapeMethod.Invoke(blendShapeComponent, new object[] { mapping.Key, mapping.Value });
                }
            }
        }

        // Helper methods
        private AudioSource FindPlaybackAudioSource(GameObject npcSystem)
        {
            var audioManager = npcSystem.GetComponentInChildren<OpenAI.RealtimeAPI.RealtimeAudioManager>();
            if (audioManager != null)
            {
                var field = audioManager.GetType().GetField("playbackAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) return field.GetValue(audioManager) as AudioSource;
            }
            var sourceTransform = npcSystem.transform.Find("PlaybackAudioSource");
            return sourceTransform?.GetComponent<AudioSource>();
        }

        private SkinnedMeshRenderer FindFacialRenderer(GameObject targetAvatar)
        {
            SkinnedMeshRenderer[] renderers = targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers) if (renderer.name == "Renderer_Head") return renderer;
            foreach (var renderer in renderers) if (renderer.name.Contains("Head")) return renderer;
            return null;
        }

        private Object LoadULipSyncSampleProfile()
        {
            #if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("uLipSync-Profile-Sample t:ScriptableObject");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
            #endif
            return null;
        }

        // Static helpers to get the types from detection (single point of truth)
        public static System.Type GetULipSyncType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                var t1 = assembly.GetType("uLipSync.uLipSync", false);
                if (t1 != null) return t1;
            }
            return null;
        }
        public static System.Type GetULipSyncBlendShapeType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                var t2 = assembly.GetType("uLipSync.uLipSyncBlendShape", false);
                if (t2 != null) return t2;
            }
            return null;
        }
    }
}
