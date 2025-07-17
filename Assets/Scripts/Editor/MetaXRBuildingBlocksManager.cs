using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Manager class for Meta XR Building Blocks automation
/// Handles programmatic installation of Meta XR Building Blocks like Camera Rig, Controller Tracking, etc.
/// Uses the same approach as OVRComprehensiveRigWizard
/// </summary>
public class MetaXRBuildingBlocksManager
{
    // Template definitions similar to OVRComprehensiveRigWizard
    private static readonly object OVRCameraRigTemplate = CreateTemplate("OVRCameraRig", "126d619cf4daa52469682f85c1378b4a");
    private static readonly object OVRInteractionComprehensiveTemplate = CreateTemplate("OVRInteractionComprehensive", "0a7d2469f24041c4284c66706f84c45e");
    
    private static object CreateTemplate(string name, string guid)
    {
        try
        {
            // Try to create Template object via reflection
            var templateType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.Template, Oculus.Interaction.Editor");
            if (templateType != null)
            {
                return System.Activator.CreateInstance(templateType, name, guid);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Failed to create template {name}: {ex.Message}");
        }
        return null;
    }
    /// <summary>
    /// Opens the Meta XR Building Blocks window
    /// </summary>
    public void OpenBuildingBlocksWindow()
    {
        try
        {
            // Access Meta XR Building Blocks Window via reflection
            var buildingBlocksWindowType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.BuildingBlocksWindow, Meta.XR.BuildingBlocks.Editor");
            
            if (buildingBlocksWindowType != null)
            {
                // Try to find ShowWindow method with specific parameter signature
                var showWindowMethod = buildingBlocksWindowType.GetMethod("ShowWindow", 
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                if (showWindowMethod != null)
                {
                    var parameters = showWindowMethod.GetParameters();
                    Debug.Log($"[OpenAI NPC Setup] ShowWindow method found with {parameters.Length} parameters.");
                    
                    if (parameters.Length == 4)
                    {
                        // Call with 4 parameters (Origins.Self, null, false, null)
                        var originsType = System.Type.GetType("Meta.XR.Editor.UserInterface.Origins, Meta.XR.Editor.UserInterface");
                        if (originsType != null)
                        {
                            var selfOrigin = System.Enum.Parse(originsType, "Self");
                            showWindowMethod.Invoke(null, new object[] { selfOrigin, null, false, null });
                            Debug.Log("[OpenAI NPC Setup] Meta XR Building Blocks window opened successfully.");
                        }
                        else
                        {
                            Debug.LogWarning("[OpenAI NPC Setup] Could not find Origins type, trying alternative approach.");
                            // Try with enum value 0 (usually Self)
                            showWindowMethod.Invoke(null, new object[] { 0, null, false, null });
                        }
                    }
                    else if (parameters.Length == 0)
                    {
                        // Call without parameters
                        showWindowMethod.Invoke(null, new object[] { });
                        Debug.Log("[OpenAI NPC Setup] Meta XR Building Blocks window opened (parameterless method).");
                    }
                    else
                    {
                        Debug.LogWarning($"[OpenAI NPC Setup] ShowWindow method has unexpected parameter count: {parameters.Length}");
                        // Try to call with null parameters
                        object[] args = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].ParameterType.IsValueType)
                            {
                                args[i] = System.Activator.CreateInstance(parameters[i].ParameterType);
                            }
                            else
                            {
                                args[i] = null;
                            }
                        }
                        showWindowMethod.Invoke(null, args);
                        Debug.Log("[OpenAI NPC Setup] Meta XR Building Blocks window opened (dynamic parameters).");
                    }
                }
                else
                {
                    Debug.LogError("[OpenAI NPC Setup] Could not find ShowWindow method in Meta XR Building Blocks.");
                }
            }
            else
            {
                Debug.LogError("[OpenAI NPC Setup] Meta XR Building Blocks not available. Please ensure Meta XR SDK is installed.");
                EditorUtility.DisplayDialog("Meta XR SDK Required", 
                    "Meta XR Building Blocks requires the Meta XR SDK to be installed.\n\nPlease install the Meta XR SDK from the Package Manager:\nWindow > Package Manager > Add package by name > com.meta.xr.sdk.all", 
                    "OK");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OpenAI NPC Setup] Error opening Meta XR Building Blocks: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets up essential Meta XR Building Blocks for VR interaction using Templates approach
    /// </summary>
    /// <param name="setupEnabled">Whether to actually install the blocks</param>
    public void SetupEssentialBuildingBlocks(bool setupEnabled = true)
    {
        if (!setupEnabled)
        {
            Debug.Log("[MetaXR BB Manager] Meta XR Building Blocks setup skipped (disabled by user).");
            return;
        }

        Debug.Log("[MetaXR BB Manager] Setting up Meta XR Building Blocks using Templates approach...");

        try
        {
            // 1. Create OVRCameraRig if not exists
            var existingCameraRig = FindExistingCameraRig();
            if (existingCameraRig == null)
            {
                Debug.Log("[MetaXR BB Manager] Creating OVRCameraRig...");
                CreateCameraRig();
            }
            else
            {
                Debug.Log("[MetaXR BB Manager] OVRCameraRig already exists.");
            }

            // 2. Create Interaction Rig
            Debug.Log("[MetaXR BB Manager] Creating Interaction Rig...");
            CreateInteractionRig();
            
            Debug.Log("[MetaXR BB Manager] ✅ Meta XR Building Blocks setup completed using Templates.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error setting up Meta XR Building Blocks: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates OVRCameraRig using Templates approach
    /// </summary>
    private void CreateCameraRig()
    {
        try
        {
            var templatesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.Templates, Oculus.Interaction.Editor");
            if (templatesType == null)
            {
                Debug.LogError("[MetaXR BB Manager] Could not find Templates class.");
                return;
            }

            var createFromTemplateMethod = templatesType.GetMethod("CreateFromTemplate", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (createFromTemplateMethod == null)
            {
                Debug.LogError("[MetaXR BB Manager] Could not find CreateFromTemplate method.");
                return;
            }

            Debug.Log("[MetaXR BB Manager] Creating OVRCameraRig from template...");
            
            // Create OVRCameraRig: Templates.CreateFromTemplate(null, OVRCameraRigTemplate, asPrefab: true)
            var cameraRigObject = createFromTemplateMethod.Invoke(null, new object[] { null, OVRCameraRigTemplate, true });
            
            if (cameraRigObject != null)
            {
                Debug.Log("[MetaXR BB Manager] ✅ OVRCameraRig created successfully.");
                
                // Configure OVRManager similar to OVRComprehensiveRigWizard
                var cameraRigGameObject = cameraRigObject as GameObject;
                if (cameraRigGameObject != null && cameraRigGameObject.TryGetComponent(out object ovrManager))
                {
                    ConfigureOVRManager(ovrManager);
                }
            }
            else
            {
                Debug.LogError("[MetaXR BB Manager] Failed to create OVRCameraRig from template.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error creating OVRCameraRig: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates Interaction Rig using Templates approach
    /// </summary>
    private void CreateInteractionRig()
    {
        try
        {
            var cameraRig = FindExistingCameraRig();
            if (cameraRig == null)
            {
                Debug.LogError("[MetaXR BB Manager] No OVRCameraRig found for interaction rig creation.");
                return;
            }

            var templatesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.Templates, Oculus.Interaction.Editor");
            if (templatesType == null)
            {
                Debug.LogError("[MetaXR BB Manager] Could not find Templates class.");
                return;
            }

            var createFromTemplateMethod = templatesType.GetMethod("CreateFromTemplate", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (createFromTemplateMethod == null)
            {
                Debug.LogError("[MetaXR BB Manager] Could not find CreateFromTemplate method.");
                return;
            }

            Debug.Log("[MetaXR BB Manager] Creating Interaction Rig from template...");
            
            // Get Transform from cameraRig
            Transform cameraRigTransform = null;
            if (cameraRig is GameObject cameraRigGameObject)
            {
                cameraRigTransform = cameraRigGameObject.transform;
            }
            else if (cameraRig is Component cameraRigComponent)
            {
                cameraRigTransform = cameraRigComponent.transform;
            }
            
            if (cameraRigTransform == null)
            {
                Debug.LogError("[MetaXR BB Manager] Could not get Transform from OVRCameraRig.");
                return;
            }
            
            // Create Interaction Rig: Templates.CreateFromTemplate(cameraRigTransform, OVRInteractionComprehensiveTemplate, asPrefab: true)
            var interactionRigObject = createFromTemplateMethod.Invoke(null, new object[] { cameraRigTransform, OVRInteractionComprehensiveTemplate, true });
            
            if (interactionRigObject != null)
            {
                Debug.Log("[MetaXR BB Manager] ✅ Interaction Rig created successfully.");
                
                // Disable duplicate visuals similar to OVRComprehensiveRigWizard
                DisableDuplicateVisuals(cameraRig);
                
                // Select the created object
                if (interactionRigObject is UnityEngine.Object unityObject)
                {
                    EditorGUIUtility.PingObject(unityObject);
                    Selection.activeObject = unityObject;
                }
            }
            else
            {
                Debug.LogError("[MetaXR BB Manager] Failed to create Interaction Rig from template.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error creating Interaction Rig: {ex.Message}");
        }
    }

    /// <summary>
    /// Find existing OVRCameraRig in scene
    /// </summary>
    private object FindExistingCameraRig()
    {
        try
        {
            var ovrCameraRigType = System.Type.GetType("OVRCameraRig, Assembly-CSharp");
            if (ovrCameraRigType == null)
            {
                // Try alternative assembly
                ovrCameraRigType = System.Type.GetType("OVRCameraRig, Oculus.VR");
            }
            
            if (ovrCameraRigType != null)
            {
                return Object.FindFirstObjectByType(ovrCameraRigType);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error finding OVRCameraRig: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Configure OVRManager similar to OVRComprehensiveRigWizard
    /// </summary>
    private void ConfigureOVRManager(object ovrManager)
    {
        try
        {
            var ovrManagerType = ovrManager.GetType();
            
            // Set tracking origin to FloorLevel
            var trackingOriginTypeField = ovrManagerType.GetField("_trackingOriginType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (trackingOriginTypeField != null)
            {
                var trackingOriginType = trackingOriginTypeField.FieldType;
                var floorLevelValue = System.Enum.Parse(trackingOriginType, "FloorLevel");
                trackingOriginTypeField.SetValue(ovrManager, floorLevelValue);
                Debug.Log("[MetaXR BB Manager] OVRManager tracking origin set to FloorLevel.");
            }
            
            // Set controller driven hand poses
            var controllerDrivenHandPosesProperty = ovrManagerType.GetProperty("controllerDrivenHandPosesType");
            if (controllerDrivenHandPosesProperty != null)
            {
                var enumType = controllerDrivenHandPosesProperty.PropertyType;
                var conformingValue = System.Enum.Parse(enumType, "ConformingToController");
                controllerDrivenHandPosesProperty.SetValue(ovrManager, conformingValue);
                Debug.Log("[MetaXR BB Manager] OVRManager controller driven hand poses configured.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error configuring OVRManager: {ex.Message}");
        }
    }

    /// <summary>
    /// Disable duplicate visuals similar to OVRComprehensiveRigWizard
    /// </summary>
    private void DisableDuplicateVisuals(object cameraRig)
    {
        try
        {
            var cameraRigGameObject = cameraRig as GameObject;
            if (cameraRigGameObject == null)
            {
                var cameraRigComponent = cameraRig as Component;
                if (cameraRigComponent != null)
                {
                    cameraRigGameObject = cameraRigComponent.gameObject;
                }
            }
            
            if (cameraRigGameObject != null)
            {
                var ovrHandType = System.Type.GetType("OVRHand, Assembly-CSharp") ?? System.Type.GetType("OVRHand, Oculus.VR");
                if (ovrHandType != null)
                {
                    var ovrHands = cameraRigGameObject.GetComponentsInChildren(ovrHandType);
                    foreach (var hand in ovrHands)
                    {
                        DisableHandVisualComponents(hand);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error disabling duplicate visuals: {ex.Message}");
        }
    }

    /// <summary>
    /// Disable hand visual components to avoid duplicates
    /// </summary>
    private void DisableHandVisualComponents(object hand)
    {
        try
        {
            var handGameObject = (hand as Component)?.gameObject;
            if (handGameObject == null) return;

            // Disable OVRSkeletonRenderer
            var skeletonRenderer = handGameObject.GetComponent<Component>();
            if (skeletonRenderer != null && skeletonRenderer.GetType().Name == "OVRSkeletonRenderer")
            {
                var enabledProperty = skeletonRenderer.GetType().GetProperty("enabled");
                enabledProperty?.SetValue(skeletonRenderer, false);
            }

            // Disable OVRMesh
            var ovrMesh = handGameObject.GetComponent<Component>();
            if (ovrMesh != null && ovrMesh.GetType().Name == "OVRMesh")
            {
                var enabledProperty = ovrMesh.GetType().GetProperty("enabled");
                enabledProperty?.SetValue(ovrMesh, false);
            }

            // Disable OVRMeshRenderer
            var meshRenderer = handGameObject.GetComponent<Component>();
            if (meshRenderer != null && meshRenderer.GetType().Name == "OVRMeshRenderer")
            {
                var enabledProperty = meshRenderer.GetType().GetProperty("enabled");
                enabledProperty?.SetValue(meshRenderer, false);
            }

            // Disable SkinnedMeshRenderer
            var skinnedMeshRenderer = handGameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.enabled = false;
            }

            Debug.Log("[MetaXR BB Manager] Duplicate hand visual components disabled.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error disabling hand visual components: {ex.Message}");
        }
    }

    /// <summary>
    /// Installs a specific building block by its ID from BlockDataIds
    /// </summary>
    /// <param name="blockDataIdsTypeName">The type name containing block IDs</param>
    /// <param name="blockIdField">The field name of the specific block ID</param>
    /// <param name="blockDisplayName">Display name for logging</param>
    public void InstallBuildingBlockById(string blockDataIdsTypeName, string blockIdField, string blockDisplayName)
    {
        try
        {
            // Get the block ID
            var blockDataIdsType = System.Type.GetType(blockDataIdsTypeName + ", Meta.XR.BuildingBlocks.Editor");
            if (blockDataIdsType == null)
            {
                Debug.LogError($"[OpenAI NPC Setup] Could not find {blockDataIdsTypeName} type.");
                return;
            }

            var blockIdFieldInfo = blockDataIdsType.GetField(blockIdField, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (blockIdFieldInfo == null)
            {
                Debug.LogError($"[OpenAI NPC Setup] Could not find {blockIdField} field in {blockDataIdsTypeName}.");
                return;
            }

            string blockId = blockIdFieldInfo.GetValue(null) as string;
            if (string.IsNullOrEmpty(blockId))
            {
                Debug.LogError($"[OpenAI NPC Setup] Block ID for {blockDisplayName} is null or empty.");
                return;
            }

            Debug.Log($"[OpenAI NPC Setup] Found Block ID for {blockDisplayName}: {blockId}");

            // Get Utils class and find block data
            var utilsType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            if (utilsType == null)
            {
                Debug.LogError("[OpenAI NPC Setup] Could not find Meta.XR.BuildingBlocks.Editor.Utils type.");
                return;
            }

            var getBlockDataMethod = utilsType.GetMethod("GetBlockData", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new System.Type[] { typeof(string) },
                null);
            
            if (getBlockDataMethod == null)
            {
                Debug.LogError("[OpenAI NPC Setup] Could not find GetBlockData method in Utils.");
                return;
            }

            var blockData = getBlockDataMethod.Invoke(null, new object[] { blockId });
            if (blockData == null)
            {
                Debug.LogError($"[OpenAI NPC Setup] Could not find block data for {blockDisplayName} (ID: {blockId}).");
                return;
            }

            Debug.Log($"[OpenAI NPC Setup] Found block data for {blockDisplayName}: {blockData.GetType().Name}");

            // Check if block is already installed
            var blockDataType = blockData.GetType();
            var getCacheMethod = blockDataType.GetMethod("GetCache", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (getCacheMethod != null)
            {
                var cache = getCacheMethod.Invoke(blockData, null);
                var isInteractableProperty = cache.GetType().GetProperty("IsInteractable");
                if (isInteractableProperty != null)
                {
                    bool isInteractable = (bool)isInteractableProperty.GetValue(cache);
                    if (!isInteractable)
                    {
                        Debug.Log($"[OpenAI NPC Setup] {blockDisplayName} is already installed or not available.");
                        return;
                    }
                }
            }

            // Debug: List ALL methods to understand what's available
            var allMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Debug.Log($"[OpenAI NPC Setup] Available instance methods for {blockDisplayName}:");
            foreach (var method in allMethods)
            {
                var paramTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                Debug.Log($"  - {method.Name}({paramTypes})");
            }
            
            // Also check for static methods
            var staticMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (staticMethods.Length > 0)
            {
                Debug.Log($"[OpenAI NPC Setup] Available static methods for {blockDisplayName}:");
                foreach (var method in staticMethods)
                {
                    var paramTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    Debug.Log($"  - static {method.Name}({paramTypes})");
                }
            }

            // Try multiple installation approaches
            System.Reflection.MethodInfo installMethod = null;
            
            // Approach 1: Look for AddToProject with specific signatures
            var addToProjectMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(m => m.Name == "AddToProject").ToArray();
            
            Debug.Log($"[OpenAI NPC Setup] Found {addToProjectMethods.Length} AddToProject methods for {blockDisplayName}");
            
            foreach (var method in addToProjectMethods)
            {
                var paramTypes = method.GetParameters();
                var paramString = string.Join(", ", paramTypes.Select(p => p.ParameterType.Name));
                Debug.Log($"  - AddToProject({paramString})");
                
                // Try the simplest signature first
                if (paramTypes.Length == 1 && paramTypes[0].ParameterType == typeof(GameObject))
                {
                    installMethod = method;
                    Debug.Log($"[OpenAI NPC Setup] Using AddToProject(GameObject) for {blockDisplayName}");
                    break;
                }
            }
            
            // Approach 2: If no GameObject-only method, try GameObject + Action
            if (installMethod == null)
            {
                foreach (var method in addToProjectMethods)
                {
                    var paramTypes = method.GetParameters();
                    if (paramTypes.Length == 2 && 
                        paramTypes[0].ParameterType == typeof(GameObject) && 
                        paramTypes[1].ParameterType == typeof(System.Action))
                    {
                        installMethod = method;
                        Debug.Log($"[OpenAI NPC Setup] Using AddToProject(GameObject, Action) for {blockDisplayName}");
                        break;
                    }
                }
            }
            
            // Approach 3: Try any AddToProject method
            if (installMethod == null && addToProjectMethods.Length > 0)
            {
                installMethod = addToProjectMethods[0];
                Debug.Log($"[OpenAI NPC Setup] Using first available AddToProject method for {blockDisplayName}");
            }
            
            // Approach 4: Look for Install methods
            if (installMethod == null)
            {
                var installMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(m => m.Name.Contains("Install")).ToArray();
                
                if (installMethods.Length > 0)
                {
                    installMethod = installMethods[0];
                    Debug.Log($"[OpenAI NPC Setup] Using install method: {installMethod.Name} for {blockDisplayName}");
                }
            }

            // Execute the installation
            if (installMethod != null)
            {
                Debug.Log($"[MetaXR BB Manager] Installing {blockDisplayName}...");
                
                var parameters = installMethod.GetParameters();
                object[] args = new object[parameters.Length];
                
                // Fill parameters with appropriate defaults
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    if (paramType == typeof(GameObject))
                    {
                        args[i] = null; // Parent GameObject
                    }
                    else if (paramType == typeof(System.Action))
                    {
                        args[i] = (System.Action)null; // Callback
                    }
                    else if (paramType.IsValueType)
                    {
                        args[i] = System.Activator.CreateInstance(paramType);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                
                installMethod.Invoke(blockData, args);
                Debug.Log($"[MetaXR BB Manager] ✅ {blockDisplayName} installed successfully.");
            }
            else
            {
                // Use Utils.GetInstallationRoutine - the correct Meta XR SDK pattern!
                Debug.Log($"[MetaXR BB Manager] Using Utils.GetInstallationRoutine for {blockDisplayName}");
                
                var getInstallationRoutineMethod = utilsType.GetMethod("GetInstallationRoutine", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new System.Type[] { typeof(string) },
                    null);
                
                if (getInstallationRoutineMethod != null)
                {
                    try
                    {
                        Debug.Log($"[MetaXR BB Manager] Getting installation routine for block ID: {blockId}");
                        var installationRoutine = getInstallationRoutineMethod.Invoke(null, new object[] { blockId });
                        
                        if (installationRoutine != null)
                        {
                            Debug.Log($"[MetaXR BB Manager] Installation routine found: {installationRoutine.GetType().Name}");
                            
                            // Try to execute the installation routine manually
                            var routineType = installationRoutine.GetType();
                            var executeMethod = routineType.GetMethod("Execute", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            var runMethod = routineType.GetMethod("Run", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            var startMethod = routineType.GetMethod("Start", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            
                            bool executed = false;
                            
                            // Try Execute method
                            if (executeMethod != null)
                            {
                                try
                                {
                                    executeMethod.Invoke(installationRoutine, null);
                                    Debug.Log($"[MetaXR BB Manager] ✅ {blockDisplayName} executed via Execute() method.");
                                    executed = true;
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[MetaXR BB Manager] Execute() failed: {ex.Message}");
                                }
                            }
                            
                            // Try Run method
                            if (!executed && runMethod != null)
                            {
                                try
                                {
                                    runMethod.Invoke(installationRoutine, null);
                                    Debug.Log($"[MetaXR BB Manager] ✅ {blockDisplayName} executed via Run() method.");
                                    executed = true;
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[MetaXR BB Manager] Run() failed: {ex.Message}");
                                }
                            }
                            
                            // Try Start method
                            if (!executed && startMethod != null)
                            {
                                try
                                {
                                    startMethod.Invoke(installationRoutine, null);
                                    Debug.Log($"[MetaXR BB Manager] ✅ {blockDisplayName} executed via Start() method.");
                                    executed = true;
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"[MetaXR BB Manager] Start() failed: {ex.Message}");
                                }
                            }
                            
                            if (!executed)
                            {
                                Debug.LogWarning($"[MetaXR BB Manager] Could not execute installation routine for {blockDisplayName}. Available methods:");
                                var routineMethods = routineType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                foreach (var method in routineMethods)
                                {
                                    var paramTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                                    Debug.Log($"  - {method.Name}({paramTypes})");
                                }
                            }
                            
                            return;
                        }
                        else
                        {
                            Debug.LogError($"[MetaXR BB Manager] Installation routine returned null for {blockDisplayName}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[MetaXR BB Manager] Failed to get installation routine for {blockDisplayName}: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[MetaXR BB Manager] GetInstallationRoutine method not found in Utils class.");
                }
                
                Debug.LogError($"[MetaXR BB Manager] Could not install {blockDisplayName} - installation routine failed.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error installing {blockDisplayName}: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Installs a building block by searching for it by name
    /// </summary>
    /// <param name="blockName">Name to search for</param>
    /// <param name="blockDisplayName">Display name for logging</param>
    private void InstallBuildingBlockByName(string blockName, string blockDisplayName)
    {
        try
        {
            Debug.Log($"[OpenAI NPC Setup] Installing {blockDisplayName} by name: {blockName}");

            // Get Utils class to search for blocks by name
            var utilsType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            if (utilsType == null)
            {
                Debug.LogError("[OpenAI NPC Setup] Could not find Meta.XR.BuildingBlocks.Editor.Utils type.");
                return;
            }

            // Try to get all available blocks
            var getAllBlocksMethod = utilsType.GetMethod("GetAllBlockData", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (getAllBlocksMethod == null)
            {
                Debug.LogError("[OpenAI NPC Setup] Could not find GetAllBlockData method in Utils.");
                
                // Alternative: Try to access blocks via the Window directly
                InstallBuildingBlockDirectly(blockName, blockDisplayName);
                return;
            }

            var allBlocks = getAllBlocksMethod.Invoke(null, null);
            if (allBlocks == null)
            {
                Debug.LogError("[OpenAI NPC Setup] GetAllBlockData returned null.");
                return;
            }

            Debug.Log($"[OpenAI NPC Setup] Found blocks collection: {allBlocks.GetType().Name}");

            // Search for our block by name
            object targetBlock = null;
            
            // Handle different collection types
            if (allBlocks is System.Collections.IEnumerable enumerable)
            {
                foreach (var block in enumerable)
                {
                    if (block == null) continue;
                    
                    // Get block name/title
                    var blockType = block.GetType();
                    var nameProperty = blockType.GetProperty("BlockName") ?? 
                                      blockType.GetProperty("Name") ?? 
                                      blockType.GetProperty("Title");
                    
                    if (nameProperty != null)
                    {
                        var name = nameProperty.GetValue(block)?.ToString();
                        Debug.Log($"[OpenAI NPC Setup] Found block: {name}");
                        
                        if (name != null && name.Contains(blockName))
                        {
                            targetBlock = block;
                            Debug.Log($"[OpenAI NPC Setup] Matched block: {name}");
                            break;
                        }
                    }
                }
            }

            if (targetBlock == null)
            {
                Debug.LogError($"[OpenAI NPC Setup] Could not find block with name containing: {blockName}");
                return;
            }

            // Install the found block using the existing InstallBuildingBlock logic
            InstallFoundBuildingBlock(targetBlock, blockDisplayName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OpenAI NPC Setup] Error installing {blockDisplayName} by name: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Attempts direct installation via the Building Blocks window
    /// </summary>
    /// <param name="blockName">Name of the block to install</param>
    /// <param name="blockDisplayName">Display name for logging</param>
    public static void InstallBuildingBlockDirectly(string blockName, string blockDisplayName)
    {
        try
            {
                Debug.Log($"[OpenAI NPC Setup] Trying direct installation for {blockDisplayName}");

                // Access Meta XR Building Blocks Window directly and trigger installation
                var buildingBlocksWindowType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.BuildingBlocksWindow, Meta.XR.BuildingBlocks.Editor");
                
                if (buildingBlocksWindowType == null)
                {
                    Debug.LogError("[OpenAI NPC Setup] Could not find BuildingBlocksWindow type.");
                    return;
                }

                // Try to find an Install method that takes a block name
                var installMethods = buildingBlocksWindowType.GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                foreach (var method in installMethods)
                {
                    if (method.Name.Contains("Install") || method.Name.Contains("Add"))
                    {
                        var paramTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                        Debug.Log($"[OpenAI NPC Setup] Found potential install method: {method.Name}({paramTypes})");
                    }
                }

                Debug.Log($"[OpenAI NPC Setup] Could not directly install {blockDisplayName}. Manual installation required.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OpenAI NPC Setup] Error in direct installation: {ex.Message}");
            }
    }

    private void InstallFoundBuildingBlock(object blockData, string blockDisplayName)
    {
        try
        {
            if (blockData == null)
            {
                Debug.LogError($"[OpenAI NPC Setup] Block data is null for {blockDisplayName}.");
                return;
            }

            Debug.Log($"[OpenAI NPC Setup] Installing found block for {blockDisplayName}: {blockData.GetType().Name}");

            // Check if block is already installed
            var blockDataType = blockData.GetType();
            var getCacheMethod = blockDataType.GetMethod("GetCache", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (getCacheMethod != null)
            {
                var cache = getCacheMethod.Invoke(blockData, null);
                var isInteractableProperty = cache?.GetType().GetProperty("IsInteractable");
                if (isInteractableProperty != null)
                {
                    bool isInteractable = (bool)isInteractableProperty.GetValue(cache);
                    if (!isInteractable)
                    {
                        Debug.Log($"[OpenAI NPC Setup] {blockDisplayName} is already installed or not available.");
                        return;
                    }
                }
            }

            // Debug: List all methods to understand the correct signature
            var allMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Debug.Log($"[OpenAI NPC Setup] Available methods for {blockDisplayName}:");
            foreach (var method in allMethods)
            {
                if (method.Name.Contains("Add") || method.Name.Contains("Install"))
                {
                    var paramTypes = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    Debug.Log($"  - {method.Name}({paramTypes})");
                }
            }

            // Try multiple installation approaches
            System.Reflection.MethodInfo installMethod = null;
            
            // Approach 1: Look for AddToProject with specific signatures
            var addToProjectMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(m => m.Name == "AddToProject").ToArray();
            
            Debug.Log($"[OpenAI NPC Setup] Found {addToProjectMethods.Length} AddToProject methods for {blockDisplayName}");
            
            foreach (var method in addToProjectMethods)
            {
                var paramTypes = method.GetParameters();
                var paramString = string.Join(", ", paramTypes.Select(p => p.ParameterType.Name));
                Debug.Log($"  - AddToProject({paramString})");
                
                // Try the simplest signature first
                if (paramTypes.Length == 1 && paramTypes[0].ParameterType == typeof(GameObject))
                {
                    installMethod = method;
                    Debug.Log($"[OpenAI NPC Setup] Using AddToProject(GameObject) for {blockDisplayName}");
                    break;
                }
            }
            
            // Approach 2: If no GameObject-only method, try GameObject + Action
            if (installMethod == null)
            {
                foreach (var method in addToProjectMethods)
                {
                    var paramTypes = method.GetParameters();
                    if (paramTypes.Length == 2 && 
                        paramTypes[0].ParameterType == typeof(GameObject) && 
                        paramTypes[1].ParameterType == typeof(System.Action))
                    {
                        installMethod = method;
                        Debug.Log($"[OpenAI NPC Setup] Using AddToProject(GameObject, Action) for {blockDisplayName}");
                        break;
                    }
                }
            }
            
            // Approach 3: Try any AddToProject method
            if (installMethod == null && addToProjectMethods.Length > 0)
            {
                installMethod = addToProjectMethods[0];
                Debug.Log($"[OpenAI NPC Setup] Using first available AddToProject method for {blockDisplayName}");
            }
            
            // Approach 4: Look for Install methods
            if (installMethod == null)
            {
                var installMethods = blockDataType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(m => m.Name.Contains("Install")).ToArray();
                
                if (installMethods.Length > 0)
                {
                    installMethod = installMethods[0];
                    Debug.Log($"[OpenAI NPC Setup] Using install method: {installMethod.Name} for {blockDisplayName}");
                }
            }

            // Execute the installation
            if (installMethod != null)
            {
                Debug.Log($"[OpenAI NPC Setup] Installing {blockDisplayName}...");
                
                var parameters = installMethod.GetParameters();
                object[] args = new object[parameters.Length];
                
                // Fill parameters with appropriate defaults
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    if (paramType == typeof(GameObject))
                    {
                        args[i] = null; // Parent GameObject
                    }
                    else if (paramType == typeof(System.Action))
                    {
                        args[i] = (System.Action)null; // Callback
                    }
                    else if (paramType.IsValueType)
                    {
                        args[i] = System.Activator.CreateInstance(paramType);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                
                installMethod.Invoke(blockData, args);
                Debug.Log($"[OpenAI NPC Setup] ✅ {blockDisplayName} installed successfully.");
            }
            else
            {
                Debug.LogError($"[OpenAI NPC Setup] Could not find any suitable installation method for {blockDisplayName}.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[OpenAI NPC Setup] Error installing found block {blockDisplayName}: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Gets available building block names for debugging
    /// </summary>
    /// <returns>Array of available block names, or empty array if none found</returns>
    public static string[] GetAvailableBuildingBlockNames()
    {
        try
        {
            var utilsType = System.Type.GetType("Meta.XR.BuildingBlocks.Editor.Utils, Meta.XR.BuildingBlocks.Editor");
            if (utilsType == null)
            {
                Debug.LogWarning("[MetaXR BB Manager] Could not find Meta.XR.BuildingBlocks.Editor.Utils type.");
                return new string[0];
            }

            var getAllBlocksMethod = utilsType.GetMethod("GetAllBlockData", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (getAllBlocksMethod == null)
            {
                Debug.LogWarning("[MetaXR BB Manager] Could not find GetAllBlockData method.");
                return new string[0];
            }

            var allBlocks = getAllBlocksMethod.Invoke(null, null);
            if (allBlocks == null)
            {
                return new string[0];
            }

            var blockNames = new System.Collections.Generic.List<string>();
            
            if (allBlocks is System.Collections.IEnumerable enumerable)
            {
                foreach (var block in enumerable)
                {
                    if (block == null) continue;
                    
                    var blockType = block.GetType();
                    var nameProperty = blockType.GetProperty("BlockName") ?? 
                                      blockType.GetProperty("Name") ?? 
                                      blockType.GetProperty("Title");
                    
                    if (nameProperty != null)
                    {
                        var name = nameProperty.GetValue(block)?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            blockNames.Add(name);
                        }
                    }
                }
            }

            return blockNames.ToArray();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetaXR BB Manager] Error getting available building blocks: {ex.Message}");
            return new string[0];
        }
    }

    /// <summary>
    /// Checks if Meta XR SDK is available
    /// </summary>
    /// <returns>True if Meta XR SDK is detected</returns>
    public static bool IsMetaXRSDKAvailable()
    {
        var templatesType = System.Type.GetType("Oculus.Interaction.Editor.QuickActions.Templates, Oculus.Interaction.Editor");
        var ovrCameraRigType = System.Type.GetType("OVRCameraRig, Assembly-CSharp") ?? System.Type.GetType("OVRCameraRig, Oculus.VR");
        
        return templatesType != null && ovrCameraRigType != null;
    }
}
