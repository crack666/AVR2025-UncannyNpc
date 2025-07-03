# 🔧 Audio Troubleshooting Guide

## 🎯 **Symptome und Lösungen**

### **1. Stotternde/Ruckelige Audio-Wiedergabe**

**Symptome:**
- Audio bricht ständig ab
- Roboterhafte Stimme
- Unterbrechungen im Audiostream

**Häufigste Ursachen & Lösungen:**

#### **A) Unity Audio-Einstellungen**
```
✅ LÖSUNG:
1. Edit → Project Settings → Audio
2. System Sample Rate = 24000
3. DSP Buffer Size = Best Latency (oder Good Latency)
4. Virtual Voice Count = 512
5. Real Voice Count = 32
```

#### **B) Windows Audio-Einstellungen**
```
✅ LÖSUNG:
1. Rechtsklick auf Lautsprecher-Symbol → Sounds
2. Recording → Mikrofon → Properties
3. Advanced Tab:
   - Sample Rate: 24000 Hz oder 48000 Hz
   - ❌ "Allow applications to take exclusive control" DEAKTIVIEREN
4. Enhancements Tab:
   - ❌ Alle Audio-Enhancements DEAKTIVIEREN
```

#### **C) RealtimeAudioManager Einstellungen**
```
✅ LÖSUNG:
1. Im Inspector: RealtimeAudioManager
2. Stream Buffer Size: 2048 oder 4096 (bei schwächeren PCs)
3. Use Adaptive Buffering: TRUE aktivieren
4. Performance Monitoring: TRUE aktivieren
```

---

### **2. Keine Audio-Antwort von OpenAI**

**Symptome:**
- Mikrofon funktioniert (VAD erkannt)
- Keine Audio-Rückmeldung
- Connection OK, aber stumm

**Häufigste Ursachen & Lösungen:**

#### **A) OpenAI API Probleme**
```
✅ PRÜFUNG:
1. Unity Console → Suche nach "RealtimeClient" errors
2. Häufige Fehler:
   - "rate_limit_exceeded" → API Quota überschritten
   - "invalid_request" → Audio zu lang/kurz
   - "connection_error" → Netzwerk-Problem
```

#### **B) Netzwerk-Verbindung**
```
✅ LÖSUNG:
1. Stabile Internet-Verbindung prüfen
2. Firewall/Antivirus temporär deaktivieren
3. VPN ausschalten (kann WebSocket blockieren)
4. Router neu starten
```

#### **C) Audio-Input Problem**
```
✅ LÖSUNG:
1. Zu kurze Aufnahmen → mindestens 1-2 Sekunden sprechen
2. Zu leise → Mikrofon-Lautstärke in Windows erhöhen
3. Falsche Sprache → auf Englisch sprechen (funktioniert am besten)
```

---

### **3. Mikrofon wird nicht erkannt**

**Symptome:**
- "No microphone devices found"
- VAD funktioniert nicht
- Stumme Aufnahme

**Lösungen:**

#### **A) Hardware-Prüfung**
```
✅ LÖSUNG:
1. Windows → Settings → Privacy → Microphone
2. "Allow apps to access microphone" AKTIVIEREN
3. "Allow desktop apps to access microphone" AKTIVIEREN
4. Unity als erlaubte App hinzufügen
```

#### **B) Default Device**
```
✅ LÖSUNG:
1. Rechtsklick Lautsprecher → Sounds
2. Recording Tab
3. Richtiges Mikrofon als "Default Device" setzen
4. Test → "Listen to this device" kurz aktivieren
```

#### **C) Unity Mikrofon-Test**
```
✅ LÖSUNG:
1. RealtimeAudioManager im Inspector
2. Context Menu (⋮) → "Run Audio Diagnostics"
3. Console-Output prüfen
4. Bei Problemen → Unity neu starten
```

---

## 🛠️ **Automatische Diagnose**

### **AudioDiagnostics Script verwenden:**

1. **Audio Diagnostics Component hinzufügen:**
   ```
   1. GameObject mit RealtimeAudioManager auswählen
   2. Add Component → AudioDiagnostics
   3. "Run Diagnostics On Start" aktivieren
   4. Play drücken → Console prüfen
   ```

2. **AudioTroubleshooting Script verwenden:**
   ```
   1. Add Component → AudioTroubleshooting  
   2. "Auto Fix Common Issues" aktivieren
   3. Play drücken → automatische Optimierung
   ```

---

## 📊 **Performance-Überwachung**

### **Adaptive Buffering aktivieren:**
```
✅ Im RealtimeAudioManager:
1. Use Adaptive Buffering = TRUE
2. Performance Monitoring = TRUE
3. Console überwachen für automatische Anpassungen:
   "Adapting buffer size: 1024 -> 2048 (increased for stability)"
```

### **Manual Performance Check:**
```
✅ Context Menu:
1. RealtimeAudioManager → "Force Audio Performance Check"
2. "Run Audio Diagnostics" für vollständige Info
3. "Reset Adaptive Settings" bei Problemen
```

---

## 🎯 **System-spezifische Lösungen**

### **Schwächere PCs:**
```
✅ OPTIMIERUNGEN:
- Stream Buffer Size: 4096
- DSP Buffer Size: Good Latency (nicht Best)
- Frame Rate Limit: 60 FPS in Unity
- Quality Settings: Reduzieren
```

### **Gaming-PCs mit Audio-Software:**
```
⚠️ KONFLIKTE VERMEIDEN:
- Razer Synapse Audio → deaktivieren
- Nvidia Audio Driver → auf Standard wechseln  
- Gaming Audio Software → temporär schließen
- Discord Audio-Processing → deaktivieren
```

### **Laptops:**
```
✅ LAPTOP-SPEZIFISCH:
- Power Plan: High Performance
- USB Audio-Geräte bevorzugen (externe Mikrofone)
- Interne Audio-Enhancements deaktivieren
- Windows Audio-Service neu starten
```

---

## 🚨 **Emergency-Lösungen**

### **Wenn gar nichts funktioniert:**

1. **Complete Reset:**
   ```
   1. Unity schließen
   2. Windows → Services → "Windows Audio" neu starten
   3. Default-Mikrofon in Windows neu setzen
   4. Unity neu starten
   5. OpenAI NPC → Quick Setup erneut ausführen
   ```

2. **Fallback Audio Settings:**
   ```
   1. Unity Project Settings → Audio:
      - System Sample Rate: 48000 (statt 24000)
      - DSP Buffer Size: Good Latency
   2. RealtimeAudioManager:
      - Stream Buffer Size: 4096
      - Use Adaptive Buffering: FALSE
   ```

3. **Minimal Test Setup:**
   ```
   1. Neues Test-Projekt erstellen
   2. Nur OpenAI NPC importieren
   3. Minimal Setup ohne Customizations
   4. Testen ob Audio funktioniert
   ```

---

## 📞 **Support & Debugging**

### **Debug-Logs sammeln:**
```
✅ FÜR SUPPORT:
1. Unity Console → Clear
2. AudioDiagnostics → Run Full Diagnostics
3. Problem reproduzieren
4. Console-Output kopieren
5. System-Info hinzufügen:
   - Windows Version
   - Unity Version  
   - Mikrofon-Modell
   - Audio-Hardware (Soundkarte)
```

### **Häufige Debug-Ausgaben:**
```
✅ NORMAL:
"[AudioManager] Good audio signal detected!"
"[RealtimeClient] Session created with ID: sess_xxx"
"[GAPLESS] Stream initialized: 24000Hz"

⚠️ PROBLEMATISCH:
"[AudioManager] Audio queue empty while playing"
"[RealtimeClient] buffer too small"
"[AudioManager] Low frame rate detected"

❌ KRITISCH:
"NO MICROPHONE DEVICES FOUND!"
"Failed to start microphone recording"
"WebSocket connection failed"
```

---

**💡 Tipp:** Die meisten Audio-Probleme lassen sich durch korrekte Unity Audio-Einstellungen (24kHz Sample Rate) und Windows Mikrofon-Konfiguration (Exclusive Mode deaktiviert) beheben!
