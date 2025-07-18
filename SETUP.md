# 🚀 Unity OpenAI Realtime NPC - Setup Guide

**Get your gapless voice NPC running in 5 minutes**

---

## 📋 Quick Start Checklist

- [ ] ✅ Unity 2022.3 LTS or newer
- [ ] 🔑 OpenAI API key with Realtime API access  
- [ ] 🎤 Working microphone
- [ ] 🔊 Audio output (speakers/headphones)
- [ ] 💻 Windows/Mac/Linux

---

## 🎯 1. Project Setup (2 minutes)

### **Option A: Clone Repository**
```bash
git clone https://github.com/your-repo/unity-openai-npc.git
cd unity-openai-npc
```

### **Option B: Download ZIP**
1. Download project ZIP from GitHub
2. Extract to your Unity projects folder
3. Open Unity Hub → **Add project from disk**

### **Open in Unity**
1. Launch **Unity Hub**
2. Click **Add** → Select project folder
3. **Open** the project (Unity 2022.3+ required)

---

## 🔑 2. Configure OpenAI API (1 minute)

### **Create OpenAI Settings**
1. In Unity **Project window** → Right-click
2. **Create** → **OpenAI** → **Settings**
3. Name it `OpenAISettings`
4. **Select the created asset**

### **Enter Your API Key**
```
🔑 API Key: [Your OpenAI API key here]
🤖 Model: gpt-4o-realtime-preview (default)
🗣️ Voice: alloy (or: echo, fable, onyx, nova, shimmer)
```

> **🔑 Get API Key:** Visit [OpenAI Platform](https://platform.openai.com/api-keys)  
> **💡 Realtime API:** Ensure your account has Realtime API access

---

## 🛠️ 3. Automated Setup (30 seconds)

### **Run the Magic Setup Script**
1. In Unity menu bar: **OpenAI NPC** → **Quick Setup**
2. ✨ **Watch the magic happen** - All components auto-configured!
3. ✅ **Setup complete** - Ready to test

### **What the Setup Creates:**
- 🤖 **NPCController** with all references linked
- 🎙️ **RealtimeAudioManager** with gapless streaming
- 🌐 **RealtimeClient** connected to your settings
- 🎛️ **UI Canvas** for testing and configuration
- 🔊 **Audio sources** properly configured

---

## 🧪 4. Test Your NPC (1 minute)

### **First Test - Voice Chat**
1. **Press Play** in Unity
2. Click **\"Connect\"** button
3. Wait for **\"Connected\"** status  
4. Click **\"Start Conversation\"**
5. **Speak to your NPC!** 🗣️

### **Backup Test - Text Chat**
1. Use the **text input field** at the bottom
2. Type a message and press **Send**
3. NPC will respond with voice

### **Expected Behavior:**
```
✅ Smooth, gapless audio playback
✅ Automatic voice activity detection
✅ Natural conversation flow
✅ Zero audible gaps between audio chunks
```

---

## 🎛️ 5. Configuration Options

### **Voice Selection (Runtime)**
- **Dropdown in game UI** - Switch voices during play
- **Personalities:** 
  - `alloy` - Balanced, friendly
  - `echo` - Deeper, more masculine
  - `fable` - British accent, storytelling
  - `onyx` - Deep, authoritative
  - `nova` - Energetic, younger
  - `shimmer` - Soft, gentle

### **Audio Settings**
```csharp
🎚️ Microphone Volume: 0.0 - 2.0 (default: 1.0)
🔊 Output Volume: 0.0 - 1.0 (default: 1.0)  
⚡ Voice Activity Detection: On/Off
🎯 VAD Threshold: 0.01 - 0.1 (default: 0.02)
🔇 VAD Silence Duration: 0.5 - 3.0s (default: 1.0s)
```

### **Advanced Settings (Inspector)**
```csharp
🌊 Use Gapless Streaming: true (recommended)
📊 Stream Buffer Size: 1024 samples (optimal)
🐛 Enable Debug Logging: true (for troubleshooting)
```

---

## 🔧 Troubleshooting

### **🚫 \"Connection Failed\"**

**Cause:** API key or network issues
```
✅ Check API key in OpenAISettings
✅ Verify internet connection  
✅ Confirm Realtime API access on OpenAI account
✅ Check firewall/proxy settings
```

### **🔇 \"No Audio Input\"**

**Cause:** Microphone permissions or hardware
```
✅ Check Windows microphone permissions
✅ Test microphone in other apps
✅ Verify microphone device in Unity Audio Settings
✅ Try text input as backup test
```

### **🔊 \"No Audio Output\"**

**Cause:** Audio routing or volume issues
```
✅ Check system volume levels
✅ Verify Unity Audio Settings
✅ Check AudioSource configuration on NPC
✅ Test with headphones vs speakers
```

### **⚡ \"Choppy/Robotic Audio\"**

**Cause:** Old audio system or buffer issues
```
✅ Ensure \"Use Gapless Streaming\" = true
✅ Set Stream Buffer Size = 1024
✅ Check internet connection stability
✅ Monitor Unity Console for audio warnings
```

### **🔄 \"NPC Won't Respond After First Answer\"**

**Cause:** State management or recording issues
```
✅ Check Console logs for state transitions
✅ Verify \"Auto Return to Listening\" is enabled
✅ Manual restart: Stop → Start Conversation
✅ Check for error messages in Console
```

---

## 📊 Debug Information

### **Enable Verbose Logging**
```csharp
// In OpenAISettings
enableDebugLogging = true;
```

### **Monitor These Logs:**
```
[GAPLESS] Started gapless audio streaming
[NPCController] AI Assistant successfully started recording
[RealtimeClient] Connected to OpenAI Realtime API
[GAPLESS] OnAudioRead filled 1408 samples. Remaining buffers: 47
```

### **Performance Monitoring**
```csharp
// Check audio status
string status = audioManager.GetGaplessStreamDebugInfo();
Debug.Log(status);
// Output: \"Gapless Streaming: ENABLED | Buffers: 23 | Started: True\"
```

---

## 🎮 Controls Reference

| **Control** | **Function** | **Keyboard** |
|-------------|--------------|--------------|
| Connect | Establish OpenAI connection | - |
| Start Conversation | Begin voice interaction | - |
| Stop Conversation | End voice interaction | - |
| Text Input | Send text message | Enter |
| Volume Slider | Adjust output volume | - |
| Voice Dropdown | Change NPC voice | - |

---

## 🔄 VR Setup (Optional)

### **Enable VR Mode**
1. **File** → **Build Settings** → **Player Settings**
2. **XR Plug-in Management** → Install XR provider
3. **Configure XR settings** for your headset
4. **Test in VR** - All controls work in VR!

### **VR-Specific Settings**
```csharp
🥽 Spatial Audio: Recommended for immersion
📐 NPC Height: Adjust for VR perspective  
👁️ Eye Contact: NPCs will look at VR headset
🎯 Proximity Detection: Auto-start conversations
```

---

## 📈 Performance Tips

### **Optimize for Your Platform**
```csharp
// Windows (High Performance)
streamBufferSize = 1024;
enableDebugLogging = false;  // In production

// Mobile/WebGL (Lower Latency)  
streamBufferSize = 512;
audioChunkSizeMs = 100;

// VR (Balanced)
streamBufferSize = 1024;
vadThreshold = 0.015f;  // More sensitive
```

### **Memory Optimization**
```csharp
// Disable when not needed
enableDebugLogging = false;

// Clear audio history
audioManager.ClearAudioHistory();

// Restart connection periodically
await client.RestartSession();
```

---

## 🆘 Getting Help

### **Common Solutions**
1. **Restart Unity** - Fixes most initialization issues
2. **Check Console** - Error messages provide specific guidance  
3. **Test with Text** - Isolates audio vs API issues
4. **Verify Settings** - Ensure OpenAISettings is properly configured

### **Support Channels**
- 📝 **Create GitHub Issue** - For bugs and feature requests
- 💬 **Unity Console Logs** - Include full error messages
- 📖 **Technical Documentation** - See [TECHNICAL.md](TECHNICAL.md)

### **Diagnostic Information to Include**
```
Unity Version: 2022.3.x
Platform: Windows/Mac/Linux
OpenAI Model: gpt-4o-realtime-preview
Audio Device: [Your microphone/speakers]
Error Message: [Full console output]
```

---

## 🎉 Success! What's Next?

### **🎭 Customize Your NPC**
- **Change voice** using the dropdown
- **Modify personality** in System Instructions
- **Add custom animations** to ReadyPlayerMe avatar
- **Create multiple NPCs** with different settings

### **🚀 Advanced Features**
- **Custom tools/functions** for NPC capabilities
- **Multi-language support** for global audiences  
- **Integration with game systems** (inventory, quests, etc.)
- **Analytics and conversation logging**

### **🌟 Share Your Creation**
- Upload to **Unity Asset Store**
- Share on **social media** with #UnityAI
- **Contribute back** to the open source project
- **Join the community** and help others

---

**🎊 Congratulations! You now have a production-ready AI NPC with gapless voice streaming!**

*For technical details, see [TECHNICAL.md](TECHNICAL.md)*  
*For project overview, see [README.md](README.md)*

---

## 🗣️ Optional: Professionelles LipSync mit uLipSync

**uLipSync** ist ein Open-Source, hochqualitatives LipSync-System für Unity, das perfekt mit diesem Projekt funktioniert.

### Installation von uLipSync

1. **Unity öffnen** (Projekt geladen)
2. Menü: **Window → Package Manager**
3. Oben rechts: **+** → **Add package from Git URL...**
4. URL eingeben:
   ```
   https://github.com/hecomi/uLipSync.git#upm
   ```
5. **Add** klicken und Installation abwarten
6. Nach der Installation: **Setup-Script erneut ausführen** (OpenAI NPC → Quick Setup)

**Hinweis:**
- Das Setup-Script erkennt uLipSync automatisch und konfiguriert es als bevorzugtes LipSync-System.
- Ist uLipSync nicht installiert, wird automatisch das Fallback-System aktiviert.
- Für beste Ergebnisse: uLipSync BlendShape-Mapping im Inspector prüfen und ggf. anpassen.

**Weitere Infos:**
- [uLipSync GitHub](https://github.com/hecomi/uLipSync)
- [uLipSync Dokumentation](https://github.com/hecomi/uLipSync#readme)

---

## 🗣️ Professionelles LipSync mit uLipSync – Automatische Einrichtung & Hintergrund

Ab Version X.X wird uLipSync vollständig automatisch durch das Setup-Skript konfiguriert. Die folgenden Schritte werden dabei ausgeführt und sind für das Verständnis und die Fehlersuche wichtig:

### Was passiert automatisch?

1. **uLipSync-Profil wird zugewiesen**
   - Das Skript weist der `uLipSync`-Komponente auf der PlaybackAudioSource automatisch das Standardprofil `uLipSync-Profile-Sample` zu (Pfad: `Library/PackageCache/com.hecomi.ulipsync.../Assets/Profiles`).
   - **Warum ist das wichtig?**
     - Das Profil enthält sogenannte **Phoneme** (Laute wie A, I, U, E, O, N), die als akustische Muster (MFCC-Templates) gespeichert sind.
     - uLipSync vergleicht das eingehende Audiosignal mit diesen Mustern, erkennt so gesprochene Laute und kann daraus Mundbewegungen ableiten.
     - Ohne Profil kann uLipSync keine Sprache erkennen und keine Mundanimation erzeugen.

2. **On LipSync Update-Event wird verbunden**
   - Das Skript verbindet das Event „On LipSync Update (LipSyncInfo)“ der `uLipSync`-Komponente automatisch mit der Methode `uLipSyncBlendShape.OnLipSyncUpdate` auf dem Avatar (z.B. `PreviewAvatar`).
   - Dadurch werden erkannte Phoneme direkt an die BlendShape-Steuerung des Avatars weitergegeben.

3. **uLipSyncBlendShape wird konfiguriert**
   - Das Skript setzt im `uLipSyncBlendShape`-Script auf dem Avatar automatisch den richtigen SkinnedMeshRenderer (z.B. `Renderer_Head`).
   - Unter „Blend Shapes“ werden für jedes Phonem Regeln erstellt, welches BlendShape wie animiert wird (z.B. A → mouthOpen, I → mouthSmile).
   - Der Wert „Max Blend Shape Value“ wird auf **1** gesetzt, damit die BlendShapes im Bereich 0–1 animiert werden (wie von ReadyPlayerMe erwartet).

### Was sind Phoneme und wie funktioniert das?
- **Phoneme** sind die kleinsten bedeutungsunterscheidenden Laute einer Sprache (z.B. A, I, U, E, O, N).
- Das uLipSync-Profil enthält für jedes Phonem ein akustisches Muster (MFCC), das beim Sprechen erkannt wird.
- uLipSync analysiert das Audiosignal in Echtzeit, erkennt die Phoneme und löst für jedes erkannte Phonem ein Event aus.
- Die `uLipSyncBlendShape`-Komponente setzt dann die zugehörigen BlendShapes (z.B. `mouthOpen`, `mouthSmile`) mit Werten zwischen 0 und 1, sodass die Mundbewegung des Avatars synchron zur Sprache animiert wird.

**Hinweis:**
- Die automatische Einrichtung deckt alle nötigen Schritte ab. Für individuelle Anpassungen (z.B. andere BlendShapes oder eigene Profile) kann die Konfiguration im Inspector nachträglich angepasst werden.

---