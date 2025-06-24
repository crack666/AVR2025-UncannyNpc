# ReadyPlayerMe Lip Sync Setup Instructions

## Übersicht
Das neue `ReadyPlayerMeLipSync` System ermöglicht realistische Mundanimationen für ReadyPlayerMe Avatare während der Audio-Wiedergabe über das OpenAI Realtime API.

## Features
- **Automatische Lip Sync**: Reagiert auf Audio-Amplitude für realistische Mundbewegungen
- **Freundlicher Gesichtsausdruck**: Basis-Lächeln mit natürlichen Variationen
- **Smooth Animation**: Weiche Übergänge und natürliche Bewegungen
- **Auto-Detection**: Findet automatisch ReadyPlayerMe BlendShapes
- **Debug Tools**: Umfangreiche Test- und Debug-Funktionen

## Setup Schritte

### 1. LipSync Component hinzufügen
1. Wählen Sie das ReadyPlayerMe GameObject in der Szene aus
2. Fügen Sie das `ReadyPlayerMeLipSync` Component hinzu:
   - Component → Scripts → Animation → ReadyPlayerMeLipSync

### 2. NPCController konfigurieren
1. Wählen Sie das NPC System GameObject aus
2. Im NPCController Component:
   - Ziehen Sie das ReadyPlayerMe GameObject in das "Lip Sync Controller" Feld

### 3. Automatische Konfiguration
Das System erkennt automatisch:
- SkinnedMeshRenderer mit BlendShapes (normalerweise "Wolf3D_Head")
- Audio Source für Playback
- BlendShape Indices für "mouthOpen" und "mouthSmile"

## BlendShape Konfiguration

### Standard BlendShape Namen:
- **mouthOpen**: Kontrolle der Mundöffnung (0-1)
- **mouthSmile**: Kontrolle des Lächelns (0-1)

### Falls andere Namen verwendet werden:
Im ReadyPlayerMeLipSync Component können Sie die BlendShape Namen anpassen:
```
Mouth Open BlendShape Name: "Ihr_MouthOpen_Name"
Mouth Smile BlendShape Name: "Ihr_MouthSmile_Name"
```

## Einstellungen

### Lip Sync Settings:
- **Enable Lip Sync**: Ein/Aus schalten
- **Lip Sync Sensitivity**: Empfindlichkeit für Audio-Amplitude (Standard: 3.0)
- **Smoothing Speed**: Geschwindigkeit der Animationsübergänge (Standard: 10.0)
- **Lip Sync Curve**: Kurve für Amplitude-zu-Bewegung Mapping

### Mouth Animation:
- **Mouth Open Multiplier**: Verstärkung der Mundöffnung (Standard: 1.0)
- **Mouth Smile Base**: Basis-Lächeln (Standard: 0.1)
- **Mouth Smile Variation**: Zusätzliche Lächel-Variation (Standard: 0.1)
- **Speaking Rate**: Geschwindigkeit der Sprechbewegungen (Standard: 3.0)

## Testen des Systems

### Debug Context Menus (im NPCController):
- **Test Lip Sync**: Testet die Mundanimation für 3 Sekunden
- **Reset Facial Expression**: Setzt Gesichtsausdruck auf neutral zurück

### Debug Context Menus (im ReadyPlayerMeLipSync):
- **Test Mouth Open**: Testet Mundöffnungs-Animation
- **Reset Expression**: Neutral-Position
- **List BlendShapes**: Zeigt alle verfügbaren BlendShapes im Console

## Algorithmische Details

### Mundöffnung (mouthOpen):
```
Audio Amplitude → Lip Sync Curve → Mouth Open Multiplier → BlendShape Weight (0-100)
```

### Lächeln (mouthSmile):
```
Base Smile + Sin-Wave Variation (während Sprechen) + Random Micro-Movements
```

### Timing:
- **Audio Sampling**: 256 Samples bei 50fps für responsive Bewegungen
- **Smoothing**: Lerp-basierte Interpolation für natürliche Übergänge
- **Speaking Rate**: Sinuswellen-basierte Variationen für lebendige Bewegungen

## Fehlerbehebung

### Problem: Keine Mundanimation
1. Überprüfen Sie die Console auf BlendShape-Erkennungsmeldungen
2. Verwenden Sie "List BlendShapes" um verfügbare Shapes zu sehen
3. Stellen Sie sicher, dass Audio über RealtimeAudioManager abgespielt wird

### Problem: Falsche BlendShape Namen
1. Verwenden Sie "List BlendShapes" um korrekte Namen zu finden
2. Passen Sie die Namen in den LipSync-Einstellungen an

### Problem: Zu starke/schwache Animation
1. Adjustieren Sie "Lip Sync Sensitivity"
2. Modifizieren Sie "Mouth Open Multiplier"
3. Passen Sie die "Lip Sync Curve" an

### Problem: Ruckelige Animation
1. Erhöhen Sie "Smoothing Speed"
2. Verringern Sie "Speaking Rate"
3. Überprüfen Sie die Framerate

## Integration mit ReadyPlayerMe

Das System ist optimiert für ReadyPlayerMe Avatare mit:
- Standard Wolf3D_Head Mesh
- Morph3D BlendShapes
- 52 Standard Facial Expressions

Funktioniert aber auch mit anderen Avataren, die "mouthOpen" und "mouthSmile" BlendShapes haben.

## Performance

- **CPU Impact**: Minimal (Audio Sampling + BlendShape Updates)
- **Memory**: ~1KB für Audio Sample Buffer
- **Kompatibilität**: Alle Unity Versionen mit BlendShape Support

## Erweiterte Nutzung

### Programmierung eigener Animationen:
```csharp
// Zugriff auf das LipSync System
var lipSync = GetComponent<ReadyPlayerMeLipSync>();

// Manuelle Kontrolle
lipSync.SetMouthOpen(0.5f);     // 50% Mundöffnung
lipSync.SetBaseSmile(0.2f);     // 20% Basis-Lächeln
lipSync.SetSpeaking(true);      // Sprechanimation starten
```

### Event-basierte Integration:
Das System reagiert automatisch auf:
- AudioManager.OnAudioPlaybackStarted
- AudioManager.OnAudioPlaybackFinished
- Audio Amplitude Changes

## Nächste Schritte

Nach erfolgreicher Einrichtung können Sie:
1. Die Einstellungen für Ihren spezifischen Avatar optimieren
2. Zusätzliche BlendShapes für Emotionen integrieren
3. Gesture-Animationen hinzufügen
4. Erweiterte Facial Expressions implementieren
