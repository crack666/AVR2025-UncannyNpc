# 🤝 Contributing to Unity OpenAI Realtime NPC

Thank you for your interest in contributing! This project aims to make high-quality voice NPCs accessible to all Unity developers.

## 🎯 How to Contribute

### 🐛 **Report Bugs**
- Use GitHub Issues with detailed error logs
- Include Unity version, platform, and console output
- Test with the latest version first

### ✨ **Suggest Features**  
- Open a GitHub Issue with `[Feature Request]` tag
- Describe the use case and expected behavior
- Consider implementation complexity

### 🔧 **Submit Code**
1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Test thoroughly with the setup guide
4. Commit with clear messages: `git commit -m 'Add amazing feature'`
5. Push and create Pull Request

## 📋 Development Guidelines

### **Code Standards**
- Follow existing C# conventions
- Add XML documentation for public methods
- Include error handling and logging
- Test in both Editor and Build modes

### **Audio System Rules**
- Never block the audio thread
- Use `UnityMainThreadDispatcher` for Unity API calls
- Test gapless playback thoroughly
- Maintain compatibility with WebGL/Mobile

### **Testing Requirements**
- Test voice and text input modes
- Verify reconnection scenarios
- Check state transitions
- Monitor for memory leaks

## 🎮 Priority Areas

### **High Impact**
- VR/AR integration improvements
- Mobile platform optimization  
- Additional voice providers
- Performance optimizations

### **Medium Impact**
- More animation integrations
- Localization support
- Advanced conversation tools
- Analytics and logging

### **Low Impact**  
- UI/UX improvements
- Documentation updates
- Code cleanup
- Additional examples

## 📖 Resources

- [Technical Documentation](TECHNICAL.md) - Architecture details
- [Setup Guide](SETUP.md) - Getting started
- [Unity Best Practices](https://unity.com/how-to/programming-unity) - Unity guidelines

## 🏆 Recognition

Contributors are recognized in:
- README.md acknowledgments
- Git commit history
- Special contributor badges

## 📞 Questions?

- Open a GitHub Discussion
- Review existing Issues and PRs
- Check the documentation first

**Let's build amazing NPCs together!** 🚀