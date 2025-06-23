# Unity NPC mit OpenAI Realtime API - Implementierungsplan

## 🎯 Projektziel
Verbindung eines ReadyPlayerMe NPCs in Unity mit der OpenAI Realtime API für natürliche Sprachinteraktionen in VR.

## 📋 Aktuelle Situation
- ✅ Unity-Projekt mit ReadyPlayerMe Setup vorhanden
- ✅ OpenAI Realtime Console (WebRTC-Implementierung) als Referenz verfügbar
- 🎯 Ziel: C# Unity-Integration für Echtzeit-Audio-Chat mit LLM

## 🏗️ Technische Architektur

### Option 1: WebSocket-basierte Lösung (Empfohlen)
**Vorteile für Unity:**
- Bessere C# WebSocket-Bibliotheken verfügbar
- Einfachere Integration in Unity
- Bewährte Lösung für Gamedev
- Weniger Browser-spezifische Abhängigkeiten

### Option 2: WebRTC-basierte Lösung
**Herausforderungen:**
- Begrenzte native C# WebRTC-Unterstützung
- Komplexere Audio-Pipeline
- Mehr externe Dependencies

## 📦 Benötigte Unity-Packages

### Core Dependencies
```
- Newtonsoft.Json (für JSON-Serialisierung)
- WebSocketSharp-netstandard oder Native WebSockets
- Unity Microphone API (built-in)
- Unity AudioSource (built-in)
```

### Audio Processing
```
- NAudio (optional, für erweiterte Audio-Verarbeitung)
- Unity WebRequest für HTTP-Calls
```

## 🗂️ Projektstruktur (Unity Assets)

```
Assets/
├── Scripts/
│   ├── OpenAI/
│   │   ├── RealtimeAPI/
│   │   │   ├── RealtimeClient.cs          # Haupt-WebSocket-Client
│   │   │   ├── RealtimeEventTypes.cs      # Event-Definitionen
│   │   │   ├── RealtimeAudioManager.cs    # Audio Ein-/Ausgabe
│   │   │   └── RealtimeMessageHandler.cs  # Message Processing
│   │   └── Models/
│   │       ├── SessionConfig.cs           # API-Konfiguration
│   │       ├── AudioChunk.cs              # Audio-Datenstrukturen
│   │       └── ConversationState.cs       # Gesprächszustand
│   ├── NPC/
│   │   ├── NPCController.cs               # NPC-Hauptsteuerung
│   │   ├── NPCAnimationController.cs      # Lippensync & Gesten
│   │   ├── NPCVoiceManager.cs             # Sprach-Integration
│   │   └── NPCStateManager.cs             # Gesprächszustände
│   └── Managers/
│       ├── GameManager.cs                 # Globale Spielsteuerung
│       ├── UIManager.cs                   # UI-Steuerung
│       └── AudioManager.cs                # Audio-System
├── Prefabs/
│   ├── NPCPrefabs/                        # ReadyPlayerMe NPCs
│   └── UIPrefabs/                         # Interface-Elemente
└── Settings/
    └── OpenAISettings.cs                  # API-Konfiguration
```

## 🔧 Implementierungsschritte

### Phase 1: Grundlegendes Setup (Week 1)
1. **Unity WebSocket Integration**
   - WebSocketSharp-netstandard Package installieren
   - Basis WebSocket-Connection zu OpenAI testen
   - API-Key Management implementieren

2. **Audio Pipeline Setup**
   - Unity Microphone Input konfigurieren
   - Audio-Encoding (PCM16 -> Base64) implementieren
   - AudioSource für Playback einrichten

3. **Event System Design**
   - Event-basierte Architektur für API-Messages
   - UnityEvent-Integration für UI-Updates

### Phase 2: Core API Integration (Week 2)
1. **RealtimeClient.cs Implementierung**
   ```csharp
   public class RealtimeClient : MonoBehaviour 
   {
       private WebSocket ws;
       private string apiKey;
       private bool isConnected;
       
       public UnityEvent<string> OnMessageReceived;
       public UnityEvent<AudioClip> OnAudioReceived;
       
       public async Task ConnectAsync()
       public void SendAudioChunk(byte[] audioData)
       public void SendTextMessage(string message)
   }
   ```

2. **Audio Processing**
   - Microphone-Input in Real-time verarbeiten
   - Audio-Chunks in Base64 konvertieren
   - Empfangene Audio-Daten zu AudioClip konvertieren

3. **Message Handling**
   - JSON-Event-Parsing
   - Async/Await-Pattern für Unity
   - Error-Handling & Reconnection-Logic

### Phase 3: NPC Integration (Week 3)
1. **NPCController.cs**
   ```csharp
   public class NPCController : MonoBehaviour
   {
       [SerializeField] private RealtimeClient realtimeClient;
       [SerializeField] private Animator npcAnimator;
       [SerializeField] private AudioSource audioSource;
       
       private void OnEnable()
       {
           realtimeClient.OnAudioReceived.AddListener(PlayNPCResponse);
           realtimeClient.OnMessageReceived.AddListener(HandleNPCMessage);
       }
   }
   ```

2. **Animation Integration**
   - Lippensync mit empfangenen Audio-Daten
   - Idle-Animationen während Gesprächen
   - Gesten-System basierend auf Kontext

3. **Voice Activity Detection**
   - Microphone-Level-Monitoring
   - Push-to-Talk vs. kontinuierliche Erkennung
   - Unity-Input-System Integration

### Phase 4: Advanced Features (Week 4)
1. **Conversation Management**
   - Multi-Turn-Dialoge verwalten
   - Kontext zwischen Gesprächen speichern
   - NPC-Persönlichkeits-System

2. **UI Integration**
   - Conversation-Log-Display
   - Audio-Level-Visualization
   - Connection-Status-Indicators

3. **Performance Optimization**
   - Audio-Buffer-Management
   - Memory-Pool für Audio-Chunks
   - Threading für Audio-Processing

## 🎚️ Audio-Pipeline Details

### Input-Processing
```csharp
// Microphone -> PCM16 -> Base64 -> WebSocket
private IEnumerator ProcessMicrophoneInput()
{
    while (isRecording)
    {
        float[] samples = new float[sampleRate / 10]; // 100ms chunks
        microphone.GetData(samples, 0);
        
        byte[] pcmData = ConvertToPCM16(samples);
        string base64Audio = Convert.ToBase64String(pcmData);
        
        SendAudioEvent(base64Audio);
        yield return new WaitForSeconds(0.1f);
    }
}
```

### Output-Processing
```csharp
// WebSocket -> Base64 -> PCM16 -> AudioClip -> AudioSource
private void ProcessIncomingAudio(string base64Audio)
{
    byte[] audioData = Convert.FromBase64String(base64Audio);
    AudioClip clip = CreateAudioClipFromPCM(audioData);
    
    npcAudioSource.clip = clip;
    npcAudioSource.Play();
    
    // Trigger lip sync animation
    StartLipSync(clip.length);
}
```

## 🔒 Sicherheit & Konfiguration

### API-Key Management
```csharp
[CreateAssetMenu(fileName = "OpenAISettings", menuName = "OpenAI/Settings")]
public class OpenAISettings : ScriptableObject
{
    [SerializeField] private string apiKey;
    [SerializeField] private string baseUrl = "wss://api.openai.com/v1/realtime";
    [SerializeField] private string model = "gpt-4o-realtime-preview-2024-12-17";
    
    public string GetApiKey() => apiKey;
}
```

## 🧪 Testing & Debugging

### Debug-Features
- WebSocket-Message-Logger
- Audio-Waveform-Visualizer
- Latenz-Monitoring
- Error-Reporting-System

### Performance-Metriken
- Audio-Latenz (Input -> Output)
- WebSocket-Response-Times
- Memory-Usage-Tracking
- Frame-Rate-Impact-Analysis

## 🎯 Erwartete Herausforderungen

### Technische Herausforderungen
1. **Audio-Latenz minimieren** - Optimierung der gesamten Pipeline
2. **WebSocket-Stabilität** - Reconnection-Logic implementieren
3. **Unity-Threading** - Async-Operations in Unity-Context
4. **Memory-Management** - Audio-Buffers effizient verwalten

### Lösungsansätze
1. **Audio-Chunks klein halten** (50-100ms)
2. **Connection-Pooling** für bessere Stabilität
3. **Unity-Jobs-System** für Audio-Processing
4. **Object-Pooling** für Audio-Clips

## 📈 Erfolgskriterien

### MVP (Minimum Viable Product)
- ✅ Stabile WebSocket-Verbindung zu OpenAI
- ✅ Audio-Input vom Mikrofon
- ✅ Audio-Output über NPC
- ✅ Basis-Lippensync

### Full Feature Set
- ✅ Natürliche Gesprächsführung
- ✅ Emotion-basierte Animationen
- ✅ Voice Activity Detection
- ✅ Multi-NPC-Support
- ✅ Conversation-History
- ✅ Performance-optimiert für VR

## 🔄 Nächste Schritte

1. **Sofort:** WebSocketSharp Package installieren
2. **Tag 1-2:** Basis WebSocket-Connection implementieren
3. **Tag 3-5:** Audio-Pipeline aufbauen
4. **Tag 6-7:** NPC-Integration testen
5. **Week 2:** Refinement und Performance-Optimierung

---

## 📚 Referenzen

### OpenAI Realtime API
- **WebSocket Endpoint:** `wss://api.openai.com/v1/realtime`
- **Model:** `gpt-4o-realtime-preview-2024-12-17`
- **Audio Format:** PCM16, 24kHz, mono
- **Event-basierte Kommunikation**

### Unity-spezifische Überlegungen
- **Microphone.Start()** für Audio-Input
- **AudioClip.Create()** für dynamische Audio-Clips
- **Coroutines** für asynchrone Audio-Verarbeitung
- **UnityEvents** für lose gekoppelte Kommunikation

### Performance-Optimierungen
- **Audio-Buffer-Größe:** 100ms chunks
- **Thread-Safety:** Main-Thread für Unity-API-Calls
- **Memory-Management:** Object-Pooling für häufige Allocations
- **Error-Handling:** Graceful degradation bei Verbindungsproblemen
