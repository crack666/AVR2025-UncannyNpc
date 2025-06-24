# AVR2025 - Uncanny NPC

Unity project integrating ReadyPlayerMe avatars with OpenAI Realtime API for natural voice conversations in VR.

## 🎯 Project Overview

This project creates interactive NPCs that can have real-time voice conversations using OpenAI's Realtime API. The NPCs feature:

- **Real-time voice chat** using OpenAI Realtime API
- **ReadyPlayerMe avatar integration** with animations and lip sync
- **Voice Activity Detection (VAD)** for natural conversation flow
- **Modular Unity architecture** for easy expansion
- **VR-ready implementation** for immersive experiences

## 🏗️ Architecture

### Core Components

- **RealtimeClientV2**: Production WebSocket client for OpenAI API
- **NPCController**: Main NPC behavior and integration logic
- **RealtimeAudioManager**: Audio processing and voice activity detection
- **NPCUIManager**: Testing UI and conversation management

### Project Structure

```
Assets/
├── Scripts/
│   ├── OpenAI/
│   │   ├── RealtimeAPI/           # WebSocket client and event handling
│   │   └── Models/                # Data structures and audio processing
│   ├── NPC/                       # NPC controllers and behavior
│   └── Managers/                  # UI and system managers
├── Settings/                      # Configuration (API keys, etc.)
└── Scenes/                        # Test scenes
```

## 🚀 Setup Instructions

### 1. Prerequisites

- Unity 2022.3 LTS or newer
- OpenAI API key with Realtime API access
- Git (for version control)
- Windows (for System.Net.WebSockets support)

### 2. Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/crack666/AVR2025-UncannyNpc.git
   cd AVR2025-UncannyNpc
   ```

2. **Open in Unity:**
   - Open Unity Hub
   - Add project from disk
   - Select the cloned project folder

3. **Install Dependencies:**
   - The project uses Newtonsoft.Json (should auto-import)
   - Ready Player Me SDK (if not already included)

### 3. Configuration

1. **Create OpenAI Settings:**
   - Right-click in Project window
   - Create > OpenAI > Settings
   - Enter your OpenAI API key
   - Configure voice model and system prompt

2. **Test Scene Setup:**
   - Open `Assets/Scenes/OpenAI_NPC_Test.unity`
   - Assign your OpenAI Settings to the NPC components
   - Configure microphone permissions in Unity

### 4. Running the Project

1. **Play Mode Testing:**
   - Press Play in Unity
   - Click "Connect" to establish WebSocket connection
   - Click "Start Conversation" to begin voice chat
   - Use the text input for testing without voice

2. **Build for VR:**
   - Switch to VR platform in Build Settings
   - Configure XR settings for your headset
   - Build and deploy

## 🔧 Development Progress

### ✅ Phase 1: Core Setup (Completed)
- [x] Basic Unity project structure
- [x] OpenAI Settings system
- [x] Audio processing pipeline
- [x] Event-based architecture
- [x] Git repository setup

### ✅ Phase 2: Production Integration (Completed)
- [x] Real WebSocket client implementation
- [x] Complete NPC controller
- [x] UI for testing and conversation management
- [x] Lip sync and animation integration
- [x] Error handling and reconnection logic

### 🚧 Phase 3: Advanced Features (In Progress)
- [ ] ReadyPlayerMe avatar integration
- [ ] Advanced animation system
- [ ] VR hand tracking integration
- [ ] Performance optimization

### 🎯 Phase 4: Polish & Deployment (Planned)
- [ ] Audio quality improvements
- [ ] Multi-language support
- [ ] Scene management system
- [ ] Production deployment tools

## 📖 Usage Examples

### Basic Text Conversation
```csharp
// Get reference to NPC controller
var npc = FindObjectOfType<NPCController>();

// Connect to OpenAI
await npc.ConnectToOpenAI();

// Send a message
npc.SendTextMessage("Hello, how are you today?");
```

### Voice Conversation Setup
```csharp
// Start listening for voice input
npc.StartConversation();

// The NPC will automatically:
// 1. Listen for user speech
// 2. Process audio through OpenAI
// 3. Play back AI response with lip sync
// 4. Continue conversation loop
```

## 🎮 Controls

- **Connect Button**: Establish connection to OpenAI API
- **Start Conversation**: Begin voice interaction
- **Text Input**: Send text messages for testing
- **Volume Slider**: Adjust audio output level
- **Stop Conversation**: End voice interaction

## 🛠️ Troubleshooting

### Common Issues

1. **Connection Failed:**
   - Check your API key in OpenAI Settings
   - Verify internet connection
   - Ensure Realtime API access on your OpenAI account

2. **No Audio Input:**
   - Check microphone permissions in Unity
   - Verify microphone device in Windows settings
   - Test with text input first

3. **No Audio Output:**
   - Check Unity Audio Settings
   - Verify AudioSource configuration on NPC
   - Check system volume levels

### Debug Information

Enable debug logging by setting `Debug.unityLogger.logEnabled = true` in your scripts. The console will show:
- WebSocket connection status
- Audio processing information
- API response details
- Error messages with stack traces

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📞 Support

For questions and support:
- Create an issue on GitHub
- Check the Unity console for error messages
- Review the OpenAI API documentation

## 🎉 Acknowledgments

- OpenAI for the Realtime API
- Ready Player Me for avatar technology
- Unity Technologies for the game engine
- The open-source community for various tools and libraries
