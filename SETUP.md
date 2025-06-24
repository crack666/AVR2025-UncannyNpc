# OpenAI Realtime NPC System - Setup Anleitung

## Übersicht
Diese Anleitung erklärt die vollständige Einrichtung des OpenAI Realtime NPC Systems mit ReadyPlayerMe Avatar und Lip Sync Funktionalität.

## Systemanforderungen
- Unity 2021.3 oder höher
- ReadyPlayerMe Avatar (GLB/GLTF Format)
- OpenAI API Schlüssel mit Realtime API Zugang
- Mikrofon für Voice Input

---

## 1. Projekt Setup

### 1.1 Unity Projekt vorbereiten
1. Erstellen Sie ein neues Unity 3D Projekt
2. Importieren Sie alle Skripte aus dem `Assets/Scripts/` Ordner
3. Stellen Sie sicher, dass alle Abhängigkeiten installiert sind:
   - TextMeshPro
   - Unity Audio System
   - Unity Networking (für WebSocket)

### 1.2 Scene Setup
1. Öffnen Sie die `SafeTestScene.unity` oder erstellen Sie eine neue Scene
2. Fügen Sie eine Directional Light hinzu (Lighting → Directional Light)
3. Stellen Sie sicher, dass eine Main Camera vorhanden ist

---

## 2. OpenAI Konfiguration

### 2.1 OpenAI Settings erstellen
1. Erstellen Sie einen `Resources` Ordner in `Assets/`
2. Rechtsklick im Resources Ordner → Create → OpenAI → Settings
3. Nennen Sie die Datei "OpenAISettings"
4. Konfigurieren Sie die Einstellungen:
   ```
   API Key: [Ihr OpenAI API Schlüssel]
   Model: gpt-4o-realtime-preview-2024-10-01
   Voice Model: alloy
   Sample Rate: 24000
   Audio Chunk Size Ms: 100
   Microphone Volume: 1.0
   Enable Debug Logging: true
   ```

### 2.2 Realtime Settings erstellen
1. Rechtsklick im Project Window → Create → OpenAI → Realtime Settings
2. Nennen Sie die Datei "OpenAIRealtimeSettings"
3. Platzieren Sie sie in `Assets/Settings/`
4. Referenzieren Sie die OpenAISettings

---

## 3. ReadyPlayerMe Avatar Setup

### 3.1 Avatar importieren
1. Laden Sie Ihren ReadyPlayerMe Avatar herunter (GLB Format)
2. Ziehen Sie die GLB Datei in den `Assets/` Ordner
3. Unity wird automatisch den Avatar importieren

### 3.2 Avatar in Scene platzieren
1. Ziehen Sie das Avatar Prefab in die Scene
2. Positionieren Sie es bei (0, 0, 0)
3. Rotation: (0, 180, 0) - damit der Avatar zur Kamera schaut
4. Nennen Sie das GameObject "ReadyPlayerMe"

### 3.3 Avatar Komponenten überprüfen
- Stellen Sie sicher, dass der Avatar eine `Animator` Komponente hat
- Überprüfen Sie, dass `SkinnedMeshRenderer` mit BlendShapes vorhanden sind
- Notieren Sie sich die Namen der Kopf-Meshes (meist "Wolf3D_Head")

---

## 4. NPC System Setup

### 4.1 NPC System GameObject erstellen
1. Erstellen Sie ein leeres GameObject: "OpenAI NPC System"
2. Position: (0, 0, 0)

### 4.2 RealtimeClient Component hinzufügen
1. Wählen Sie das "OpenAI NPC System" GameObject
2. Add Component → Scripts → OpenAI → RealtimeAPI → RealtimeClient
3. Konfiguration:
   ```
   Settings: [Referenz zu OpenAIRealtimeSettings]
   Auto Connect: false
   Enable Debug Logging: true
   ```

### 4.3 RealtimeAudioManager Component hinzufügen
1. Add Component → Scripts → OpenAI → RealtimeAPI → RealtimeAudioManager
2. Konfiguration:
   ```
   Settings: [Referenz zu OpenAISettings]
   Realtime Client: [Referenz zum RealtimeClient Component]
   Use Default Microphone: true
   Enable VAD: true
   VAD Threshold: 0.02
   Use Smooth Playback: true
   Concatenation Delay: 0.2
   ```

### 4.4 NPCController Component hinzufügen
1. Add Component → Scripts → NPC → NPCController
2. Konfiguration:
   ```
   Realtime Client: [Referenz zum RealtimeClient]
   Audio Manager: [Referenz zum RealtimeAudioManager]
   NPC Name: "AI Assistant"
   Personality: "friendly and helpful"
   Enable Lip Sync: true
   ```

### 4.5 AudioSource für Playback erstellen
1. Erstellen Sie ein Child GameObject: "PlaybackAudioSource"
2. Add Component → Audio → Audio Source
3. Konfiguration:
   ```
   Play On Awake: false
   Loop: false
   Volume: 1.0
   Spatial Blend: 0 (2D Audio)
   ```
4. Referenzieren Sie diese AudioSource im RealtimeAudioManager

---

## 5. Lip Sync System Setup

### 5.1 ReadyPlayerMeLipSync Component hinzufügen
1. Wählen Sie das ReadyPlayerMe Avatar GameObject
2. Add Component → Scripts → Animation → ReadyPlayerMeLipSync
3. Konfiguration:
   ```
   Head Mesh Renderer: [Auto-erkannt oder manuell zuweisen]
   Audio Source: [Referenz zur PlaybackAudioSource]
   Enable Lip Sync: true
   Lip Sync Sensitivity: 3.0
   Smoothing Speed: 10.0
   Mouth Open Multiplier: 1.0
   Mouth Smile Base: 0.1
   Mouth Smile Variation: 0.1
   Speaking Rate: 3.0
   ```

### 5.2 NPCController mit LipSync verknüpfen
1. Wählen Sie das "OpenAI NPC System" GameObject
2. Im NPCController Component:
   ```
   Lip Sync Controller: [Referenz zum ReadyPlayerMeLipSync Component]
   ```

### 5.3 BlendShape Namen überprüfen
1. Wählen Sie das ReadyPlayerMeLipSync Component
2. Rechtsklick → Context Menu → "List BlendShapes"
3. Überprüfen Sie die Console für verfügbare BlendShapes
4. Falls nötig, passen Sie die Namen an:
   ```
   Mouth Open BlendShape Name: "mouthOpen" (oder der korrekte Name)
   Mouth Smile BlendShape Name: "mouthSmile" (oder der korrekte Name)
   ```

---

## 6. UI System Setup

### 6.1 Canvas erstellen
1. Rechtsklick in Hierarchy → UI → Canvas
2. Canvas Scaler → UI Scale Mode: "Scale With Screen Size"
3. Reference Resolution: 1920x1080

### 6.2 UI Panel erstellen
1. Rechtsklick auf Canvas → UI → Panel
2. Nennen Sie es "NPC UI Panel"
3. Anchors: Stretch (links unten verankert)
4. Größe anpassen für gewünschte UI Fläche

### 6.3 UI Buttons erstellen
Erstellen Sie folgende Buttons (UI → Button - TextMeshPro):
- "Connect Button" - Text: "Connect"
- "Disconnect Button" - Text: "Disconnect"
- "Start Conversation Button" - Text: "Start Listening"
- "Stop Conversation Button" - Text: "Stop Listening"
- "Send Message Button" - Text: "Send"

### 6.4 UI Text Felder erstellen
Erstellen Sie folgende Text Felder (UI → Text - TextMeshPro):
- "Status Display" - Text: "Status: Disconnected"
- "Conversation Display" - Text: "OpenAI Realtime NPC Chat..."

### 6.5 Input Field erstellen
1. UI → Input Field - TextMeshPro
2. Nennen Sie es "Message Input Field"
3. Placeholder Text: "Type your message here..."

### 6.6 NPCUIManager Component hinzufügen
1. Wählen Sie das "NPC UI Panel" GameObject
2. Add Component → Scripts → Managers → NPCUIManager
3. Verknüpfen Sie alle UI Elemente:
   ```
   Connect Button: [Referenz]
   Disconnect Button: [Referenz]
   Start Conversation Button: [Referenz]
   Stop Conversation Button: [Referenz]
   Message Input Field: [Referenz]
   Send Message Button: [Referenz]
   Conversation Display: [Referenz]
   Status Display: [Referenz]
   NPC Controller: [Referenz zum NPCController]
   ```

---

## 7. Audio System Konfiguration

### 7.1 Mikrofon Setup
1. Stellen Sie sicher, dass ein Mikrofon angeschlossen ist
2. In Unity: Edit → Project Settings → Audio
3. Überprüfen Sie, dass das System DSP Buffer Size auf "Good latency" steht

### 7.2 Audio Import Settings
1. Für alle Audio Dateien im Projekt:
   - Audio Format: PCM
   - Sample Rate: 24000 Hz
   - Compression Format: PCM

### 7.3 Playback AudioSource Konfiguration
Die bereits erstellte PlaybackAudioSource sollte haben:
```
Play On Awake: false
Loop: false
Volume: 1.0
Pitch: 1.0
Spatial Blend: 0.0 (2D)
Reverb Zone Mix: 1.0
```

---

## 8. System Testing

### 8.1 Basis Tests
1. Starten Sie Play Mode
2. Klicken Sie "Connect" - Status sollte "Connected" anzeigen
3. Klicken Sie "Start Listening" - Mikrofon sollte aktiviert werden
4. Sprechen Sie - VAD sollte reagieren
5. Warten Sie auf Response - Audio sollte abgespielt werden

### 8.2 Lip Sync Tests
1. Im ReadyPlayerMeLipSync Component:
   - Context Menu → "List BlendShapes" (überprüft verfügbare Shapes)
   - Context Menu → "Test Mouth Open" (testet Animation)
2. Im NPCController:
   - Context Menu → "Test Lip Sync" (3-Sekunden Test)

### 8.3 Audio Tests
1. Im NPCUIManager:
   - Context Menu → "Show Audio Queue Status"
   - Context Menu → "Stop All Audio Playback"
   - Context Menu → "Toggle Smooth Audio Playback"

---

## 9. Erweiterte Konfiguration

### 9.1 Performance Optimierung
```
RealtimeAudioManager:
- Sample Rate: 24000 (Balance zwischen Qualität und Performance)
- Audio Chunk Size: 100ms (responsive aber nicht zu häufig)
- VAD Threshold: 0.02 (empfindlich genug für normale Sprache)

ReadyPlayerMeLipSync:
- Smoothing Speed: 10.0 (schnell genug für natürliche Bewegung)
- Lip Sync Sensitivity: 3.0 (reagiert gut auf Audio)
```

### 9.2 Qualitäts-Einstellungen
```
Hohe Qualität:
- Sample Rate: 24000
- Concatenation Delay: 0.1
- Smoothing Speed: 15.0

Performance-optimiert:
- Sample Rate: 16000
- Concatenation Delay: 0.3
- Smoothing Speed: 8.0
```

---

## 10. Troubleshooting

### 10.1 Häufige Probleme

**Problem: Keine Verbindung zu OpenAI**
- Lösung: Überprüfen Sie API Key in OpenAISettings
- Überprüfen Sie Internetverbindung
- Prüfen Sie Console auf Fehlermeldungen

**Problem: Kein Audio**
- Lösung: Überprüfen Sie Mikrofon-Berechtigung
- Testen Sie Mikrofon in anderen Anwendungen
- Überprüfen Sie VAD Threshold (evtl. zu hoch)

**Problem: Kein Lip Sync**
- Lösung: Verwenden Sie "List BlendShapes" Context Menu
- Überprüfen Sie BlendShape Namen
- Stellen Sie sicher, dass AudioSource korrekt referenziert ist

**Problem: Ruckeliges Audio**
- Lösung: Aktivieren Sie "Use Smooth Playback"
- Adjustieren Sie "Concatenation Delay"
- Überprüfen Sie Framerate

### 10.2 Debug Optionen
```
Logging aktivieren:
- OpenAI Settings → Enable Debug Logging: true
- Console zeigt detaillierte Informationen

Audio Debug:
- Context Menu → "Show Audio Queue Status"
- Zeigt aktuelle Warteschlange und Playback Status

BlendShape Debug:
- Context Menu → "List BlendShapes"
- Zeigt alle verfügbaren BlendShapes
```

---

## 11. Finaler Test

### 11.1 Vollständiger Workflow Test
1. ✅ Starten Sie Play Mode
2. ✅ Klicken Sie "Connect" → Status: "Connected"
3. ✅ Klicken Sie "Start Listening" → Mikrofon aktiv
4. ✅ Sprechen Sie eine Frage → VAD erkennt Sprache
5. ✅ Warten Sie auf Antwort → Audio wird abgespielt
6. ✅ Beobachten Sie Lip Sync → Mund bewegt sich synchron
7. ✅ Überprüfen Sie Conversation Display → Text wird angezeigt

### 11.2 System ist einsatzbereit wenn:
- ✅ OpenAI Verbindung funktioniert
- ✅ Mikrofon Input wird erkannt
- ✅ Audio Response wird abgespielt
- ✅ Lip Sync animiert korrekt
- ✅ UI zeigt Status und Conversation
- ✅ Alle Debug Tests funktionieren

---

## 12. Weitere Schritte

Nach erfolgreichem Setup können Sie:
- Erweiterte Animationen hinzufügen
- Zusätzliche BlendShapes für Emotionen nutzen
- Gesture-System implementieren
- VR/AR Integration
- Multiplayer-Funktionalität

**🎉 Herzlichen Glückwunsch! Ihr OpenAI Realtime NPC System ist einsatzbereit!**
