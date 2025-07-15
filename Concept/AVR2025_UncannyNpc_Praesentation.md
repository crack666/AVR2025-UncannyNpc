# AVR2025-UncannyNpc: OpenAI Realtime NPC Integration
**Interaktive VR-NPCs mit KI-gesteuerter Sprachverarbeitung**

---

## 📋 **Präsentationsübersicht**

### **Slide 1: Projekttitel & Team**
- **Projekttitel**: AVR2025-UncannyNpc
- **Untertitel**: Interactive NPCs with Real-time Voice AI in Unity VR
- **Team**: crack666 & maiossa
- **Repository**: https://github.com/crack666/AVR2025-UncannyNpc
- **Technologie**: Unity 2022.3 + OpenAI Realtime API + Meta Quest 3

---

### **Slide 2: Vision & Problemstellung**
**🎯 Vision**
Entwicklung immersiver VR-Erlebnisse mit AI-gesteuerten NPCs, die natürliche Sprachkonversationen in Echtzeit führen können.

**🔍 Problemstellung**
- Traditionelle NPCs: Vorgefertigte Dialoge, begrenzte Interaktion
- Uncanny Valley: Wie glaubwürdig wirken AI-NPCs auf Nutzer:innen?
- Technische Herausforderung: Gapless Audio Streaming in VR

**🎪 Lösung**
Integration von OpenAI's Realtime API mit Unity VR für nahtlose Sprachinteraktion

---

### **Slide 3: Technologie-Stack**
**🛠️ Core Technologies**
- **Engine**: Unity 2022.3 LTS mit XR Interaction Toolkit
- **VR Platform**: Meta Quest 2/3, SteamVR Compatible
- **AI Backend**: OpenAI Realtime API (gpt-4o-realtime-preview)
- **Avatar System**: ReadyPlayerMe + Mixamo Integration
- **Audio**: 24kHz WebSocket Streaming, Zero-gap Playback
- **LipSync**: uLipSync Professional + Custom Fallback System

**🔧 Key Innovations**
- Thread-safe Audio Pipeline
- VR-optimized UI Components  
- Automated Setup System
- Dual LipSync Implementation

---

### **Slide 4: Architektur-Übersicht**
```
🎮 Unity VR Interface
        ↓
🤖 NPCController (State Management)
        ↓
🌐 RealtimeClient (WebSocket zu OpenAI)
        ↓
🎵 RealtimeAudioManager (Gapless Streaming)
        ↓
🎭 LipSync System (uLipSync/Fallback)
        ↓
👤 ReadyPlayerMe Avatar (Animation)
```

**📊 Performance Targets**
- Audio Latency: < 1000ms End-to-End
- VR Frame Rate: 72/90 FPS (Quest 2/3)
- Zero audible gaps in audio stream
- Memory Usage: < 50MB für Audio-Buffers

---

### **Slide 5: Key Features - Audio System**
**🗣️ Gapless Voice Streaming**
- Server-side VAD (Voice Activity Detection)
- PCM16 → Float32 Real-time Konvertierung
- Queue-basierte Buffer-Management (1024 Samples/Buffer)
- Stream End Detection mit intelligenter Silence-Timeout

**🎛️ Professional Voice Selection**
- 8 OpenAI Voices: Alloy, Ash, Ballad, Coral, Echo, Sage, Shimmer, Verse
- Runtime Voice Switching während aktiver Sessions
- VR-optimierte Voice Selection UI
---

### **Slide 6: Key Features - VR Integration**
**🎭 Avatar & Animation**
- ReadyPlayerMe Photorealistic Avatars
- State-driven Animation System (Idle, Listening, Speaking, Error)
- Professional LipSync mit uLipSync MFCC-Analyse
- Fallback-System für Basic Mouth Animation

**🔧 Developer Experience**
- Ein-Klick Automated Setup
- Audio Diagnostics Suite
- Canvas Interaction Fix Tools
- Comprehensive Troubleshooting Guides

**🥽 VR/XR Optimierung**
- WorldSpace Canvas für natürliche VR-Interaktion
- XR Ray Interactor Support
- Quest 3 Controller Kompatibilität
---

### **Slide 7: Implementierte Funktionen**
**✅ Fertige Core Features**
- Gapless Audio Streaming System
- Dual LipSync Implementation (Professional + Fallback)
- Complete VR/XR Integration
- Automated Project Setup
- 8-Voice Selection System
- ReadyPlayerMe Avatar Support
- Error Handling & Reconnection Logic
- Developer Troubleshooting Suite

**🔄 Testing & Optimization**
- Performance Optimization
- User Experience Testing
- Bug Fixes & Stability Improvements
- Documentation Completion

**📅 Research Phase (Geplant)**
- Uncanny Valley Studies
- User Acceptance Testing
- Data Collection & Analysis

---

### **Slide 8: Ausblick & Forschungsaspekte**
**🔬 Uncanny Valley Investigation**
- **Hypothese**: Avatar-Stil und Stimme beeinflussen wahrgenommene Glaubwürdigkeit
- **Variablen**: 
  - Visuelle Darstellung (Low-Poly → Fotorealistisch)
  - Stimme (8 verschiedene AI-Voices)
  - Dialogqualität (NLP Performance)
- **Messung**: 5-Punkt-Likert-Skala für Glaubwürdigkeit

**📊 Planned Data Collection**
- Minimum 20 Teilnehmer:innen für statistische Signifikanz
- A/B Testing verschiedener Konfigurationen
- Qualitative Post-Experience Interviews
- Privacy-konforme Interaktionsdaten

**🎯 Expected Insights**
- Optimal Avatar-Voice Combinations
- VR Presence Impact Factors
- Technical Performance vs. User Experience Correlation

---

### **Slide 9: Meilensteine & Zeitplan**
**Phase 1: Core Development ✅ (Mai 2025)**
- OpenAI Realtime API Integration
- Basic Audio Streaming
- Unity Project Structure

**Phase 2: Audio & Animation ✅ (Juni 2025)**
- Gapless Audio Optimization  
- LipSync System Implementation
- ReadyPlayerMe Integration

**Phase 3: VR Optimization ✅ (Juli 2025)**
- XR Toolkit Integration
- Quest Compatibility
- VR UI Components

**Phase 4: Developer Experience ✅ (Juli 2025)**
- Automated Setup System
- Diagnostics & Troubleshooting
- Complete Documentation

**Phase 5: Research & Evaluation 🔄 (Zukünftig)**
- User Studies, Data Collection, Analysis

---

### **Slide 10: Demo & Use Cases**
**🎮 Interactive Demo**
- Live VR Conversation mit AI-NPC
- Voice Selection in Real-time
- LipSync Animation Showcase
- Error Recovery Demonstration

**🎯 Anwendungsbereiche**
- **Gaming**: Immersive RPG Characters
- **Education**: Virtual Teaching Assistants  
- **Training**: Professional Coaching NPCs
- **Healthcare**: Therapy & Counseling Applications
- **Research**: Human-AI Interaction Studies
---

*Präsentation erstellt für: AVR2025 Projekt*  
*Zielgruppe: Akademisch/Technisch*  
*Empfohlene Präsentationsdauer: 15-20 Minuten + Q&A*
