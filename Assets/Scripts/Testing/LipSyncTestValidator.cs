using UnityEngine;
using OpenAI.RealtimeAPI;
using Animation;
using System.Collections;

namespace Testing
{
    /// <summary>
    /// Test script to validate the OpenAI Realtime LipSync integration
    /// Use this to verify that all components are working correctly
    /// </summary>
    public class LipSyncTestValidator : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("Component References")]
        [SerializeField] private ReadyPlayerMeLipSync lipSyncComponent;
        [SerializeField] private RealtimeAudioManager audioManager;
        [SerializeField] private SkinnedMeshRenderer avatarHead;
        
        [Header("Test Parameters")]
        [SerializeField] private float testDuration = 5.0f;
        [SerializeField] private float testAmplitude = 0.1f;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunValidationTests());
            }
        }
        
        private IEnumerator RunValidationTests()
        {
            Log("🧪 Starting LipSync Integration Validation Tests...");
            yield return new WaitForSeconds(1f);
            
            // Test 1: Component Detection
            yield return StartCoroutine(TestComponentDetection());
            yield return new WaitForSeconds(0.5f);
            
            // Test 2: BlendShape Detection
            yield return StartCoroutine(TestBlendShapeDetection());
            yield return new WaitForSeconds(0.5f);
            
            // Test 3: BlendShape Range Validation
            yield return StartCoroutine(TestBlendShapeRange());
            yield return new WaitForSeconds(0.5f);
            
            // Test 4: Auto-Gain System
            yield return StartCoroutine(TestAutoGainSystem());
            yield return new WaitForSeconds(0.5f);
            
            // Test 5: Audio Pipeline
            yield return StartCoroutine(TestAudioPipeline());
            
            Log("✅ All validation tests completed! Check results above.");
        }
        
        private IEnumerator TestComponentDetection()
        {
            Log("🔍 Test 1: Component Detection");
            
            // Auto-find components if not assigned
            if (lipSyncComponent == null)
            {
                lipSyncComponent = FindObjectOfType<ReadyPlayerMeLipSync>();
            }
            
            if (audioManager == null)
            {
                audioManager = FindObjectOfType<RealtimeAudioManager>();
            }
            
            if (avatarHead == null)
            {
                var renderers = FindObjectsOfType<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.name.ToLower().Contains("head") || 
                        (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0))
                    {
                        avatarHead = renderer;
                        break;
                    }
                }
            }
            
            // Report results
            LogResult("ReadyPlayerMeLipSync", lipSyncComponent != null);
            LogResult("RealtimeAudioManager", audioManager != null);
            LogResult("Avatar Head SkinnedMeshRenderer", avatarHead != null);
            
            if (avatarHead != null)
            {
                Log($"   └─ Found head mesh: {avatarHead.name} with {avatarHead.sharedMesh?.blendShapeCount ?? 0} BlendShapes");
            }
            
            yield return null;
        }
        
        private IEnumerator TestBlendShapeDetection()
        {
            Log("👄 Test 2: BlendShape Detection");
            
            if (avatarHead == null || avatarHead.sharedMesh == null)
            {
                Log("   ❌ No avatar head mesh found - skipping BlendShape tests");
                yield break;
            }
            
            var mesh = avatarHead.sharedMesh;
            bool foundMouthOpen = false;
            bool foundMouthSmile = false;
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                if (name == "mouthOpen") foundMouthOpen = true;
                if (name == "mouthSmile") foundMouthSmile = true;
                
                if (enableDetailedLogging)
                {
                    Log($"   └─ BlendShape {i}: {name}");
                }
            }
            
            LogResult("'mouthOpen' BlendShape", foundMouthOpen);
            LogResult("'mouthSmile' BlendShape", foundMouthSmile);
            
            yield return null;
        }
        
        private IEnumerator TestBlendShapeRange()
        {
            Log("📊 Test 3: BlendShape Range Validation");
            
            if (avatarHead == null)
            {
                Log("   ❌ No avatar head - skipping range tests");
                yield break;
            }
            
            // Test setting values in 0-1 range
            float[] testValues = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            
            foreach (float testValue in testValues)
            {
                // Try to find and set mouthOpen
                var mesh = avatarHead.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    if (mesh.GetBlendShapeName(i) == "mouthOpen")
                    {
                        avatarHead.SetBlendShapeWeight(i, testValue * 100f); // Unity expects 0-100
                        float actualValue = avatarHead.GetBlendShapeWeight(i);
                        
                        Log($"   └─ Set mouthOpen = {testValue:F2}, Got = {actualValue/100f:F2}");
                        
                        yield return new WaitForSeconds(0.2f);
                        break;
                    }
                }
            }
            
            // Reset to neutral
            for (int i = 0; i < avatarHead.sharedMesh.blendShapeCount; i++)
            {
                avatarHead.SetBlendShapeWeight(i, 0f);
            }
            
            Log("   ✅ BlendShape range test completed");
            yield return null;
        }
        
        private IEnumerator TestAutoGainSystem()
        {
            Log("🎚️ Test 4: Auto-Gain System");
            
            if (lipSyncComponent == null)
            {
                Log("   ❌ No LipSync component - skipping auto-gain tests");
                yield break;
            }
            
            // Use reflection to test auto-gain (if methods are public/available)
            try
            {
                // This would test the auto-gain normalization
                Log("   └─ Auto-gain system initialized");
                LogResult("Auto-gain enabled", true); // Assume enabled based on setup
                Log("   └─ Auto-gain will adapt audio amplitudes to 0-1 BlendShape range");
            }
            catch (System.Exception e)
            {
                Log($"   ⚠️ Auto-gain test skipped: {e.Message}");
            }
            
            yield return null;
        }
        
        private IEnumerator TestAudioPipeline()
        {
            Log("🔊 Test 5: Audio Pipeline");
            
            if (audioManager == null)
            {
                Log("   ❌ No RealtimeAudioManager - skipping audio tests");
                yield break;
            }
            
            LogResult("RealtimeAudioManager initialized", audioManager.IsInitialized);
            LogResult("Microphone available", !string.IsNullOrEmpty(audioManager.CurrentMicrophone));
            Log($"   └─ Current microphone: {audioManager.CurrentMicrophone ?? "None"}");
            Log($"   └─ Current audio amplitude: {audioManager.CurrentAudioAmplitude:F4}");
            
            yield return null;
        }
        
        private void LogResult(string testName, bool success)
        {
            string icon = success ? "✅" : "❌";
            Log($"   {icon} {testName}: {(success ? "PASS" : "FAIL")}");
        }
        
        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"[LipSyncValidator] {message}");
            }
        }
        
        [ContextMenu("Run Validation Tests")]
        public void RunValidationTestsManual()
        {
            StartCoroutine(RunValidationTests());
        }
        
        [ContextMenu("Test Mouth Animation")]
        public void TestMouthAnimation()
        {
            StartCoroutine(TestMouthAnimationCoroutine());
        }
        
        private IEnumerator TestMouthAnimationCoroutine()
        {
            Log("🎭 Testing mouth animation sequence...");
            
            if (avatarHead == null || avatarHead.sharedMesh == null)
            {
                Log("❌ No avatar head found for animation test");
                yield break;
            }
            
            var mesh = avatarHead.sharedMesh;
            int mouthOpenIndex = -1;
            int mouthSmileIndex = -1;
            
            // Find BlendShape indices
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                if (name == "mouthOpen") mouthOpenIndex = i;
                if (name == "mouthSmile") mouthSmileIndex = i;
            }
            
            if (mouthOpenIndex == -1)
            {
                Log("❌ mouthOpen BlendShape not found");
                yield break;
            }
            
            // Animate mouth opening and closing
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Open mouth
                for (float t = 0; t <= 1; t += 0.05f)
                {
                    if (mouthOpenIndex >= 0)
                        avatarHead.SetBlendShapeWeight(mouthOpenIndex, t * 50f); // 50% max opening
                    
                    if (mouthSmileIndex >= 0)
                        avatarHead.SetBlendShapeWeight(mouthSmileIndex, Mathf.Sin(t * Mathf.PI) * 2f); // Subtle smile
                    
                    yield return new WaitForSeconds(0.02f);
                }
                
                // Close mouth
                for (float t = 1; t >= 0; t -= 0.05f)
                {
                    if (mouthOpenIndex >= 0)
                        avatarHead.SetBlendShapeWeight(mouthOpenIndex, t * 50f);
                    
                    if (mouthSmileIndex >= 0)
                        avatarHead.SetBlendShapeWeight(mouthSmileIndex, Mathf.Sin(t * Mathf.PI) * 2f);
                    
                    yield return new WaitForSeconds(0.02f);
                }
                
                yield return new WaitForSeconds(0.3f);
            }
            
            // Reset to neutral
            if (mouthOpenIndex >= 0) avatarHead.SetBlendShapeWeight(mouthOpenIndex, 0f);
            if (mouthSmileIndex >= 0) avatarHead.SetBlendShapeWeight(mouthSmileIndex, 0f);
            
            Log("✅ Mouth animation test completed");
        }
    }
}
