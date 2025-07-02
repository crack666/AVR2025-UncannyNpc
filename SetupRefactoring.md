# Refactoring Plan

The current setup scripts, especially `CreateUISystemStep.cs` and `SetupLipSyncSystemStep.cs`, are too large and complex. This refactoring aims to break them down into smaller, more manageable, and single-responsibility steps.

- **Phase 1: Refactor `CreateUISystemStep`**
    - [x] **Step 1.1: Create `SetupCanvasStep.cs`** - This will solely handle the creation and configuration of the main `Canvas` and `EventSystem`.
    - [x] **Step 1.2: Create `CreateUIPanelStep.cs`** - This step will be responsible for creating the main UI panel.
    - [x] **Step 1.3: Create `CreateUIButtonsStep.cs`** - This will create all the necessary buttons (`Connect`, `Disconnect`, etc.).
    - [x] **Step 1.4: Create `CreateUITextElementsStep.cs`** - This will handle the creation of all `TextMeshPro` text elements.
    - [x] **Step 1.5: Create `CreateUIInputFieldsStep.cs`** - This step will create the message `TMP_InputField`.
    - [x] **Step 1.6: Create `CreateUIControlsStep.cs`** - This will create the `Voice Dropdown`, `Volume Slider`, and `VAD Toggle`.
    - [x] **Step 1.7: Create `LinkUIManagerReferencesStep.cs`** - This final UI step will link all the created UI elements to the `NpcUiManager`.
    - [x] **Step 1.8: Refactor `CreateUISystemStep.cs`** - The original script will be updated to orchestrate these new, smaller UI steps.

- **Phase 2: Refactor `SetupLipSyncSystemStep` (Detailed)**
    - [x] **Step 2.1: Extract `ReadyPlayerMeLipSync` Component**
        - [x] **Step 2.1.1:** Create a new file `Assets/Scripts/Animation/ReadyPlayerMeLipSync.cs`.
        - [x] **Step 2.1.2:** Move the `ReadyPlayerMeLipSync` class and all its methods (from `Start()` to `TestAnimationCoroutine()`) from `SetupLipSyncSystemStep.cs` into the new file.
        - [x] **Step 2.1.3:** Ensure the new file has the correct namespace (`Animation`) and necessary `using` statements.
    - [x] **Step 2.2: Create `SetupFallbackLipSyncStep.cs`**
        - [x] **Step 2.2.1:** Create a new file `Assets/Scripts/Setup/Steps/SetupFallbackLipSyncStep.cs`.
        - [x] **Step 2.2.2:** Move the `SetupFallbackLipSync` and `ConfigureFallbackLipSync` methods from `SetupLipSyncSystemStep.cs` into this new class.
        - [x] **Step 2.2.3:** This class will be responsible for adding and configuring the `ReadyPlayerMeLipSync` component.
    - [x] **Step 2.3: Create `SetupULipSyncStep.cs`**
        - [x] **Step 2.3.1:** Create a new file `Assets/Scripts/Setup/Steps/SetupULipSyncStep.cs`.
        - [x] **Step 2.3.2:** Move all `uLipSync`-related setup methods (`SetupULipSyncSystem`, `SetupULipSyncBlendShape`, `ConfigureULipSyncProfile`, `SetupULipSyncComponent`, `LinkULipSyncComponents`, `ConfigureReadyPlayerMeBlendShapes`, `ConfigureULipSyncForOpenAI`) into this new class.
    - [x] **Step 2.4: Create `DetectLipSyncSystemStep.cs` (as planned before)**
        - [x] **Step 2.4.1:** Create the file `Assets/Scripts/Setup/Steps/DetectLipSyncSystemStep.cs`.
        - [x] **Step 2.4.2:** Move the `DetectLipSyncSystems` and `LogSystemDetection` methods, along with the `LipSyncSystemInfo` class, into it.
    - [x] **Step 2.5: Refactor `SetupLipSyncSystemStep.cs` (Orchestrator)**
        - [x] **Step 2.5.1:** The original `SetupLipSyncSystemStep.cs` will be heavily simplified. Its `Execute` method will now:
            1.  Call `DetectLipSyncSystemStep`.
            2.  Based on the result, call either `SetupULipSyncStep` or `SetupFallbackLipSyncStep`.
            3.  Move helper methods like `FindFacialRenderer`, `FindPlaybackAudioSource`, and `GetBlendShapeAlternatives` into a new static utility class or into the steps that use them.
            4.  Move validation methods (`ValidateCompleteSetup`, `ValidateBlendShapes`) into a new `ValidateLipSyncSetupStep.cs`.
    - [x] **Step 2.6: Identify and Mark Obsolete Code**
        - [x] **Step 2.6.1:** After the refactoring, I will analyze the new, smaller scripts for any unreferenced methods and mark them as obsolete.

- **Phase 3: Identify and Mark Obsolete Code**
    - [ ] **Step 3.1: Analyze Code References** - I will go through the newly refactored scripts and the remaining original ones to find any methods that are no longer referenced.
    - [ ] **Step 3.2: Mark Obsolete Methods** - I will add a `// TODO: Obsolete?` comment to any unreferenced public or internal methods so they can be reviewed and removed later.

---

## ✅ **Completed - Voice System Refactoring (July 2025)**

### **Phase 4: OpenAIVoice System Modularization**

- [x] **Step 4.1: Extract OpenAIVoice Enum**
    - [x] **Step 4.1.1:** Created `Assets/Scripts/OpenAI/OpenAIVoice.cs` 
    - [x] **Step 4.1.2:** Moved `OpenAIVoice` enum from inline usage to dedicated file
    - [x] **Step 4.1.3:** Added static extension class `OpenAIVoiceExtensions` with utility methods

- [x] **Step 4.2: Voice Description Enhancement**
    - [x] **Step 4.2.1:** Added gender indicators to voice descriptions
    - [x] **Step 4.2.2:** Implemented `GetDescription()` method with format: "Alloy (neutral): Balanced, warm voice"
    - [x] **Step 4.2.3:** Added `GetAllVoiceDescriptions()` for UI dropdown population

- [x] **Step 4.3: Serialization Improvements** 
    - [x] **Step 4.3.1:** Refactored `OpenAISettings` to use `int voiceIndex` instead of enum
    - [x] **Step 4.3.2:** Added automatic validation and fallback for invalid voice indices
    - [x] **Step 4.3.3:** Implemented `VoiceIndex` property with getter validation

- [x] **Step 4.4: Fix UI Integration**
    - [x] **Step 4.4.1:** Updated `NPCUIManager.OnVoiceDropdownChanged()` to use VoiceIndex property
    - [x] **Step 4.4.2:** Enhanced voice dropdown to show descriptive names with gender
    - [x] **Step 4.4.3:** Fixed runtime voice switching functionality

- [x] **Step 4.5: Update All Voice References**
    - [x] **Step 4.5.1:** Updated `RealtimeClient.GetVoiceNameFromSettings()` to use VoiceIndex
    - [x] **Step 4.5.2:** Fixed all compilation errors related to removed `settings.Voice` property
    - [x] **Step 4.5.3:** Ensured type safety across all voice-related operations

### **Benefits Achieved:**
- ✅ **Modular Architecture**: Voice system now in dedicated file with extension methods
- ✅ **Improved UX**: Descriptive voice names with gender indicators in UI
- ✅ **Robust Serialization**: No more enum serialization issues
- ✅ **Type Safety**: Compile-time validation for all voice operations
- ✅ **Runtime Reliability**: Automatic validation and fallback for invalid states
