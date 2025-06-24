# Advanced BlendShape Debugging Guide

## Quick Debug Tools

### 1. BlendShapeDebugger Component
Add the `BlendShapeDebugger` component to your ReadyPlayerMe avatar to quickly inspect and test BlendShapes:

```
1. Select your ReadyPlayerMe avatar in the hierarchy
2. Add Component > Debug > BlendShapeDebugger
3. In Play Mode, use the Inspector buttons:
   - "Log BlendShapes" - Lists all available BlendShapes
   - "Find Mouth Shapes" - Finds mouth-related BlendShapes
   - "Test MouthOpen" / "Test MouthSmile" - Tests specific shapes
   - "Reset All BlendShapes" - Resets all to 0
```

### 2. ReadyPlayerMeLipSync Debug Features
The lip sync component now has enhanced debugging:

**Inspector Settings:**
- `Enable Debug Logging` - Shows detailed initialization logs
- `Show BlendShape Values` - Continuously logs BlendShape values

**Context Menu Options (Right-click component):**
- `Log Component Status` - Shows current component state
- `Test MouthOpen 50%` - Tests mouth open BlendShape
- `Test MouthSmile 30%` - Tests mouth smile BlendShape
- `Reset All BlendShapes` - Resets all BlendShapes
- `Find All BlendShapes` - Lists all available BlendShapes

## Common Issues & Solutions

### Issue 1: "No BlendShapes found"
**Symptoms:** Console shows "0 BlendShapes" or "BlendShape not found"

**Debug Steps:**
1. Add `BlendShapeDebugger` component to avatar
2. Click "Log BlendShapes" in Inspector
3. Check if any BlendShapes are listed

**Solutions:**
- **If no BlendShapes:** Avatar might not be properly imported or might not have facial BlendShapes
- **If wrong names:** Update the BlendShape names in the lip sync component
- **If wrong mesh:** The component might be detecting the wrong SkinnedMeshRenderer

### Issue 2: "SkinnedMeshRenderer not found"
**Symptoms:** Console shows "No head mesh renderer found"

**Debug Steps:**
1. Check hierarchy structure of ReadyPlayerMe avatar
2. Look for objects with SkinnedMeshRenderer components
3. Use BlendShapeDebugger to see what renderers are found

**Solutions:**
- **Manual Assignment:** Drag the correct SkinnedMeshRenderer to the `headMeshRenderer` field
- **Check Hierarchy:** Ensure the lip sync component is on the root avatar object or a parent of the head mesh

### Issue 3: BlendShapes found but not animating
**Symptoms:** Console shows BlendShapes found but no visual animation

**Debug Steps:**
1. Use context menu "Test MouthOpen 50%" to manually test
2. Enable "Show BlendShape Values" to see if values are being applied
3. Check if `enableLipSync` is enabled

**Solutions:**
- **Audio Source:** Ensure correct AudioSource is assigned and playing
- **Enable Lip Sync:** Check the `enableLipSync` checkbox
- **Sensitivity:** Increase `lipSyncSensitivity` value
- **Manual Test:** Use context menu tests to verify BlendShape manipulation works

### Issue 4: Wrong BlendShape names
**Symptoms:** Console shows available BlendShapes but none match "mouthOpen" or "mouthSmile"

**Common ReadyPlayerMe BlendShape Names:**
- Mouth Open: `mouthOpen`, `jawOpen`, `mouthWide`, `viseme_aa`
- Mouth Smile: `mouthSmile`, `mouthLeft`, `mouthRight`, `viseme_I`

**Solution:**
1. Use "Find All BlendShapes" context menu to see all available names
2. Update the BlendShape name fields in the component inspector
3. Look for viseme BlendShapes if standard names don't exist

## Testing Procedure

### Quick BlendShape Test:
1. Enter Play Mode
2. Select ReadyPlayerMe avatar with lip sync component
3. Right-click component → "Test MouthOpen 50%"
4. Check if mouth opens visually
5. Right-click component → "Test MouthSmile 30%"
6. Check if mouth smiles visually
7. Right-click component → "Reset All BlendShapes"

### Audio Lip Sync Test:
1. Start Play Mode
2. Connect to OpenAI API
3. Send a message to trigger speech
4. Watch Console for lip sync debug messages
5. Check if mouth moves during audio playback

## Debug Log Analysis

**Normal Initialization Logs:**
```
[ReadyPlayerMeLipSync] Found SkinnedMeshRenderer: Wolf3D_Head
[ReadyPlayerMeLipSync] Mesh found: Wolf3D_Head with 52 BlendShapes
[ReadyPlayerMeLipSync] ✅ Found mouthOpen at index 23
[ReadyPlayerMeLipSync] ✅ Found mouthSmile at index 31
```

**Problem Logs:**
```
❌ No SkinnedMeshRenderer found in children
❌ BlendShape 'mouthOpen' not found!
❌ Cannot apply mouthOpen - BlendShape index not found!
```

## Advanced Debugging

### Custom BlendShape Names
If your avatar uses different BlendShape names:
1. Use "Find All BlendShapes" to see available names
2. Update `mouthOpenBlendShapeName` and `mouthSmileBlendShapeName` fields
3. Test with context menu options

### Multiple Mesh Renderers
If avatar has multiple SkinnedMeshRenderers:
1. The component will automatically select one with BlendShapes
2. For manual control, drag the correct renderer to `headMeshRenderer` field
3. Use BlendShapeDebugger to inspect each renderer separately

### Performance Monitoring
Enable `showBlendShapeValues` to see real-time BlendShape value changes in Console during audio playback.
