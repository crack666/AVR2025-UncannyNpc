# 🛠️ Unity OpenAI Realtime NPC - Technical Documentation

**Deep dive into the architecture, implementation, and key learnings from building gapless voice NPCs**

---

## 📋 Table of Contents

1. [Architecture Overview](#-architecture-overview)
2. [Core Components](#-core-components)
3. [Gapless Audio Implementation](#-gapless-audio-implementation)
4. [Key Learnings](#-key-learnings)
5. [File Structure](#-file-structure)
6. [Configuration](#-configuration)
7. [Extending the System](#-extending-the-system)
8. [Audio Pipeline & LipSync Integration](#-audio-pipeline-lipsync-integration)
9. [Automated Setup System: Orchestrator & StepScripts](#-automated-setup-system-orchestrator--stepscripts)

---

## 🏗️ Architecture Overview

### **Design Philosophy**

```
🎯 Goal: Zero-gap audio streaming like OpenAI's web console
🧩 Approach: Modular, event-driven architecture
🎮 Target: Production-ready Unity integration
```

### **Core Architecture Pattern**

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   NPCController │◄──►│ RealtimeClient   │◄──►│ OpenAI Realtime │
│   (Behavior)    │    │ (Communication)  │    │ API (Cloud)     │
└─────────┬───────┘    └─────────┬────────┘    └─────────────────┘
          │                      │
          ▼                      ▼
┌─────────────────┐    ┌──────────────────┐
│RealtimeAudio    │    │ UnityMain        │
│Manager          │    │ ThreadDispatcher │
│(Gapless Audio)  │    │ (Thread Safety)  │
└─────────────────┘    └──────────────────┘
```

---

## 🎛️ Core Components

### **1. NPCController** 
*The brain of the NPC*

```csharp
Location: Assets/Scripts/NPC/NPCController.cs
Purpose: Orchestrates conversation flow and NPC behavior
```

**Key Responsibilities:**
- 🎭 **State Management** - Idle, Listening, Speaking, Error states
- 🔄 **Conversation Flow** - Automatic turn-taking and recovery
- 🎬 **Animation Integration** - Triggers for ReadyPlayerMe avatars
- 🛡️ **Error Handling** - Graceful degradation and reconnection

**State Machine:**
```
Idle ──Connect──► Listening ──VoiceDetected──► Processing
 │                    ▲                            │
 └◄──Error──┐        │                            ▼
             │        └──◄AutoReturn◄──── Speaking
             │
        ErrorState
```

### **2. RealtimeClient**
*WebSocket communication with OpenAI*

```csharp
Location: Assets/Scripts/OpenAI/RealtimeAPI/RealtimeClient.cs
Purpose: Handles all API communication and event processing
```

**Key Features:**
- 🌐 **WebSocket Management** - Connection, reconnection, heartbeat
- 📡 **Event Processing** - Real-time audio and text events
- 🎵 **Session Management** - Voice models, instructions, tools
- 🔒 **Thread Safety** - All API calls are thread-safe

### **3. RealtimeAudioManager** 
*The magic behind gapless audio*

```csharp
Location: Assets/Scripts/OpenAI/RealtimeAPI/RealtimeAudioManager.cs
Purpose: Implements OpenAI reference-quality audio streaming
```

**Revolutionary Features:**
- 🎯 **Gapless Streaming** - Zero-millisecond gaps between audio chunks
- 🎙️ **Voice Activity Detection** - Smart conversation turn-taking
- 🔄 **Stream Processing** - Based on OpenAI's web console implementation
- 📊 **Real-time Monitoring** - Audio levels, buffer status, performance

---

## 🎵 Gapless Audio Implementation

### **The Problem We Solved**

**Before (Gaps and Clicks):**
```
Audio: [Chunk1] [Gap] [Chunk2] [Gap] [Chunk3]
Result: Choppy, robotic speech with audible interruptions
```

**After (Seamless):**
```
Audio: [Chunk1][Chunk2][Chunk3][Chunk4]...
Result: Natural, human-like speech flow
```

### **Core Innovation: OnAudioRead() Streaming**

**Inspired by OpenAI's web console AudioWorklet implementation:**

```csharp
// Unity's audio thread callback - called every ~23ms
private void OnAudioRead(float[] data) 
{
    // Fill entire audio buffer with multiple stream buffers
    int dataIndex = 0;
    
    while (dataIndex < data.Length && streamOutputBuffers.Count > 0) 
    {
        StreamBuffer buffer = streamOutputBuffers.Dequeue();
        
        // Copy as much as possible from this buffer
        int samplesToCopy = Mathf.Min(buffer.buffer.Length, data.Length - dataIndex);
        
        for (int i = 0; i < samplesToCopy; i++) 
        {
            data[dataIndex + i] = buffer.buffer[i];
        }
        
        dataIndex += samplesToCopy;
        
        // Handle partial buffer usage...
    }
    
    // Fill remaining with silence if needed
    while (dataIndex < data.Length) 
    {
        data[dataIndex++] = 0f;
    }
}
```

### **Stream Buffer Architecture**

```
OpenAI Audio Chunks (Int16 PCM)
         ↓
   Float32 Conversion
         ↓
    Stream Buffers (1024 samples)
         ↓
    Unity OnAudioRead() [1408 samples]
         ↓
    Seamless Audio Output
```

**Key Parameters:**
- **Stream Buffer Size:** 1024 samples (43ms at 24kHz)
- **Unity Audio Buffer:** ~1408 samples (varies by system)
- **Sample Rate:** 24kHz (OpenAI standard)
- **Format:** Float32 [-1.0, 1.0] (Unity standard)

---

## 🧠 Key Learnings

### **🎯 Critical Discoveries**

1. **Buffer Size Matters**
   ```
   ❌ 128 samples = Choppy audio (5.3ms chunks)
   ✅ 1024 samples = Smooth audio (42.7ms chunks)
   ```

2. **Thread Safety Is Essential**
   ```csharp
   ❌ Direct Unity API calls from background threads
   ✅ UnityMainThreadDispatcher.EnqueueAction(() => { ... })
   ```

3. **State Management Complexity**
   ```
   ❌ Simple boolean flags
   ✅ Proper state machine with delayed transitions
   ```

4. **Audio Timing Precision**
   ```csharp
   ❌ yield return new WaitForSeconds(0.1f);  // Causes gaps
   ✅ yield return null;                       // Frame-perfect
   ```

### **🛡️ Error Handling Patterns**

```csharp
// Granular error classification
if (error.Contains("buffer too small")) {
    // Ignore harmless warnings
    return;
} else if (error.Contains("already has an active response")) {
    // Handle race conditions gracefully
    audioManager.ResetAfterError();
    return;
} else if (error.Contains("voice change during session")) {
    // Restart session for voice changes
    RestartSessionForVoiceChange();
    return;
}
// Only set error state for critical issues
SetState(NPCState.Error);
```

---

## 📁 File Structure

### **Core Scripts Architecture**

```
Assets/Scripts/
├── 🤖 NPC/
│   ├── NPCController.cs              # Main NPC behavior
│   └── NPCState.cs                   # State definitions
│
├── 🌐 OpenAI/
│   ├── RealtimeAPI/
│   │   ├── RealtimeClient.cs         # WebSocket client
│   │   ├── RealtimeAudioManager.cs   # Gapless audio system
│   │   └── RealtimeEventTypes.cs     # API event definitions
│   │
│   ├── Models/
│   │   ├── AudioChunk.cs             # Audio data structures
│   │   ├── OpenAISettings.cs         # Configuration ScriptableObject
│   │   └── ConversationState.cs      # Session management
│   │
│   └── Threading/
│       └── UnityMainThreadDispatcher.cs # Thread safety utility
│
├── 🎛️ Managers/
│   ├── NPCUIManager.cs               # In-game configuration UI
│   └── GameManager.cs                # Scene management
│
└── 🛠️ Setup/
    ├── OpenAINPCSetupUtility.cs      # Automated setup system
    └── Steps/                        # Modular setup components
        ├── CreateUISystemStep.cs
        ├── ConfigureAudioSystemStep.cs
        └── LinkReferencesStep.cs
```

---

## ⚙️ Configuration

### **OpenAI Settings Configuration**

```csharp
[CreateAssetMenu(fileName = "OpenAISettings", menuName = "OpenAI/Settings")]
public class OpenAISettings : ScriptableObject 
{
    [Header("API Configuration")]
    public string apiKey;                    // Your OpenAI API key
    public string model = "gpt-4o-realtime-preview";
    public string voice = "alloy";           // alloy, echo, fable, onyx, nova, shimmer
    
    [Header("Audio Processing")]
    public int sampleRate = 24000;           // OpenAI standard
    public int audioChunkSizeMs = 200;       // Chunk size for processing
    public float microphoneVolume = 1.0f;    // Input gain
    
    [Header("Voice Activity Detection")]
    public bool enableVAD = true;
    public float vadThreshold = 0.02f;       // Sensitivity
    public float vadSilenceDuration = 1.0f;  // Pause before stopping
    
    [Header("System Instructions")]
    [TextArea(3, 10)]
    public string systemInstructions = "You are a helpful AI assistant...";
    
    [Header("Advanced")]
    public bool useGaplessStreaming = true;  // Enable/disable new audio system
    public int streamBufferSize = 1024;     // Audio buffer size
    public bool enableDebugLogging = true;  // Verbose logging
}
```

---

## 🔧 Extending the System

### **Adding New NPC Personalities**

1. **Create Settings Asset:**
```csharp
// Right-click → Create → OpenAI → Settings
// Configure personality-specific instructions
systemInstructions = "You are a medieval tavern keeper who loves telling stories...";
voice = "echo";  // Deeper voice for character
```

2. **Custom NPC Controller:**
```csharp
public class TavernKeeperNPC : NPCController 
{
    [Header("Tavern Keeper Specific")]
    public AudioClip[] backgroundTavernSounds;
    public string[] storyPrompts;
    
    protected override void OnNPCStartedSpeaking() 
    {
        base.OnNPCStartedSpeaking();
        // Play background tavern ambience
        PlayTavernAmbience();
    }
}
```

---

## 🔊 Audio Pipeline & LipSync Integration (2025 Update)

### Audio Pipeline Overview

Unsere Audio-Pipeline basiert auf einem asynchronen, thread-sicheren Streaming-Ansatz, der von OpenAI's WebConsole-Referenz inspiriert ist. Die wichtigsten Stationen:

1. **OpenAI RealtimeClient** empfängt Audio-Chunks (PCM16) via WebSocket.
2. **RealtimeAudioManager** konvertiert diese zu Float32 und speist sie in eine Queue von Stream-Buffern (1024 Samples pro Buffer).
3. **Unity OnAudioRead()** (läuft auf Unitys Audio-Thread) zieht kontinuierlich Daten aus der Queue und füllt den Audio-Output-Buffer. Dadurch entsteht gapless Playback ohne hörbare Unterbrechungen.

### LipSync Integration – Technische Details

- **Fallback-LipSync (ReadyPlayerMeLipSync):**
  - Wird automatisch auf dem Avatar aktiviert, wenn kein uLipSync gefunden wird.
  - Ruft im `Update()`-Loop pro Frame die Methode `AnalyzeAudio()` auf.
  - Diese Methode liest die aktuellen Audiodaten direkt aus dem Playback-AudioSource (`GetOutputData` und `GetSpectrumData`).
  - Die Lautstärke- und ggf. Formant-Analyse steuert die BlendShapes für Mundbewegungen.

- **uLipSync (wenn installiert):**
  - Nutzt Unitys `OnAudioFilterRead()`-Callback, der ebenfalls auf dem Audio-Thread läuft.
  - Analysiert das gleiche Audiosignal, das auch für das Playback verwendet wird.
  - Liefert Phonem-Events, die auf den Avatar gemappt werden.
  - **Wichtig:** Ist uLipSync installiert, wird die Fallback-Komponente (`ReadyPlayerMeLipSync`) nicht hinzugefügt und deren `Update()`/`AnalyzeAudio()` wird nicht ausgeführt. Es gibt keine Interferenz zwischen den Systemen – immer nur eine LipSync-Komponente ist aktiv.

### Threading & Performance

- **Audio Playback:**
  - Das eigentliche Audio-Playback (OnAudioRead) läuft auf Unitys dediziertem Audio-Thread.
  - Die Queue-Architektur sorgt dafür, dass keine Race-Conditions oder Buffer-Underruns auftreten.

- **LipSync (Fallback):**
  - Die Analyse (`AnalyzeAudio()`) läuft im normalen Unity-Update-Thread (Main Thread).
  - Sie liest nur die aktuellen Samples aus dem Playback-AudioSource (kein Eingriff in die Queue oder das Playback selbst).
  - Die Berechnung (RMS, ggf. FFT) ist sehr leichtgewichtig und hat keinen messbaren Einfluss auf die Audio-Performance.

- **uLipSync:**
  - Arbeitet direkt auf dem Audio-Thread, aber nur lesend/analysierend.
  - Die eigentliche Audioausgabe bleibt davon unbeeinflusst.

### Kann LipSync das Audio-Playback stören?

- **Nein.**
  - Die LipSync-Analyse liest nur die aktuellen Audiodaten, sie verändert oder verzögert das Playback nicht.
  - Die Queue-Architektur und die Trennung von Audio-Thread und Main Thread verhindern, dass BlendShape-Updates oder Analyse das Audio-Streaming beeinflussen.
  - Auch bei sehr vielen BlendShapes oder hoher Update-Rate bleibt das Playback gapless.

### Fazit

- Die LipSync-Integration ist vollständig thread-sicher und beeinflusst die Audio-Pipeline in keiner Weise negativ.
- Die Architektur ist so ausgelegt, dass Audio-Playback und Mundanimation unabhängig voneinander performant und stabil laufen.
- Auch bei komplexen Szenen oder vielen NPCs bleibt die Audioqualität erhalten.

---

*Letztes Review: 2025-06-29 – Systemarchitektur und Performance bestätigt.*

---

## 🛠️ Automatisiertes Setup-System: Orchestrator & StepScripts

### Überblick

Das Setup-System ist modular aufgebaut und besteht aus einem zentralen Orchestrator (SetupScript/Utility) und einer Reihe von StepScripts, die jeweils für einen klar abgegrenzten Teil der Einrichtung zuständig sind. Dieses Design ermöglicht eine flexible, erweiterbare und fehlertolerante Automatisierung der gesamten NPC- und Systemkonfiguration.

### Komponenten

- **Orchestrator (z.B. `OpenAINPCSetupUtility`)**
  - Steuert den gesamten Setup-Prozess als "State Machine" oder Pipeline.
  - Ruft die einzelnen StepScripts in definierter Reihenfolge auf.
  - Übernimmt Logging, Fehlerbehandlung und das Weiterreichen von Kontext (z.B. Avatar, Settings, UI-Objekte).
  - Kann aus dem Editor (MenuItem) oder per Code aufgerufen werden.

- **StepScripts (z.B. `FindOrValidateAssetsStep`, `CreateUISystemStep`, `SetupLipSyncSystemStep`, ...)**
  - Kapseln jeweils einen logischen Setup-Schritt (z.B. Avatar finden, UI erstellen, Audio konfigurieren, LipSync einrichten).
  - Haben klar definierte Ein- und Ausgaben (z.B. GameObject, Settings, Rückgabewerte).
  - Können unabhängig getestet, erweitert oder ausgetauscht werden.
  - Jeder Step ist für seine eigene Fehlerbehandlung und Logging verantwortlich.

### Ablauf (Beispiel)

1. **Asset Discovery:**
   - `FindOrValidateAssetsStep` sucht nach einem ReadyPlayerMe-Avatar und den OpenAISettings.
2. **UI-System:**
   - `CreateUISystemStep` erstellt Canvas, Panel und UI-Elemente gemäß Konfiguration.
3. **NPC-System:**
   - Erstellt das zentrale NPC-System-GameObject und fügt Kernkomponenten hinzu.
4. **Audio-System:**
   - Konfiguriert AudioSources, RealtimeAudioManager und deren Verbindungen.
5. **LipSync-System:**
   - `SetupLipSyncSystemStep` erkennt und konfiguriert das optimale LipSync-System (uLipSync oder Fallback).
6. **Referenz-Verknüpfung:**
   - `LinkReferencesStep` verbindet alle Komponenten und UI-Elemente miteinander.
7. **Validierung:**
   - Jeder Step prüft und loggt seinen Erfolg, der Orchestrator gibt eine Gesamtzusammenfassung aus.

### Vorteile
- **Modularität:** Jeder Step kann unabhängig angepasst oder erweitert werden.
- **Wartbarkeit:** Fehler und Verbesserungen sind gezielt pro StepScript möglich.
- **Transparenz:** Jeder Schritt loggt detailliert, was passiert ist und ob es Fehler gab.
- **Erweiterbarkeit:** Neue Features (z.B. weitere UI-Elemente, alternative LipSync-Systeme) können als neue Steps ergänzt werden.

### Beispiel-Code (Orchestrator)
```csharp
// Auszug aus OpenAINPCSetupUtility
public static void ExecuteFullSetup(...) {
    // Step 1: Asset Discovery
    var assetStep = new FindOrValidateAssetsStep(...);
    assetStep.Execute().MoveNext();
    // Step 2: UI-System
    var uiStep = new CreateUISystemStep();
    uiStep.Execute(...);
    // ...weitere Steps...
    // Step N: LipSync
    var lipSyncStep = new SetupLipSyncSystemStep(...);
    lipSyncStep.Execute(...);
    // ...
}
```

---

**This technical documentation reflects our journey from choppy audio to production-ready gapless streaming. Every optimization and pattern here was learned through real implementation challenges.** 🎯

*For setup instructions, see [SETUP.md](SETUP.md)*
*For general project info, see [README.md](README.md)*