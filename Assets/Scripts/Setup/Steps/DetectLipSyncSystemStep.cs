using UnityEngine;
using System.IO;

namespace Setup.Steps
{
    /// <summary>
    /// Step to detect which LipSync system (uLipSync or fallback) is available.
    /// </summary>
    public class DetectLipSyncSystemStep
    {
        private System.Action<string> log;
        public LipSyncSystemInfo SystemInfo { get; private set; }

        public DetectLipSyncSystemStep(System.Action<string> log)
        {
            this.log = log;
        }

        public void Execute()
        {
            log("🔍 Step 5.1: LipSync System Detection");
            SystemInfo = DetectAvailableSystems();
            LogSystemDetection(SystemInfo);
        }

        private LipSyncSystemInfo DetectAvailableSystems()
        {
            var info = new LipSyncSystemInfo();

            // Robustly search for uLipSync types in all loaded assemblies
            System.Type uLipSyncType = null;
            System.Type blendShapeType = null;

            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                try
                {
                    var t1 = assembly.GetType("uLipSync.uLipSync", false);
                    var t2 = assembly.GetType("uLipSync.uLipSyncBlendShape", false);

                    if (uLipSyncType == null && t1 != null)
                    {
                        uLipSyncType = t1;
                        log($"[DEBUG] ✅ Found uLipSync.uLipSync in {assembly.GetName().Name}");
                    }
                    if (blendShapeType == null && t2 != null)
                    {
                        blendShapeType = t2;
                        log($"[DEBUG] ✅ Found uLipSync.uLipSyncBlendShape in {assembly.GetName().Name}");
                    }
                    if (uLipSyncType != null && blendShapeType != null) break;
                }
                catch (System.Exception ex)
                {
                    log($"[DEBUG] ⚠️ Error checking assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }

            info.HasULipSync = uLipSyncType != null;
            info.HasULipSyncBlendShape = blendShapeType != null;
            info.ULipSyncType = uLipSyncType;
            info.ULipSyncBlendShapeType = blendShapeType;

            log($"[DEBUG] Type resolution results:");
            log($"[DEBUG]   uLipSync: {(uLipSyncType != null ? uLipSyncType.FullName : "NOT FOUND")}");
            log($"[DEBUG]   uLipSyncBlendShape: {(blendShapeType != null ? blendShapeType.FullName : "NOT FOUND")}");

            // Check for uLipSync assemblies if types not found (legacy fallback)
            if (!info.HasULipSync)
            {
                string[] assemblyNames = { "uLipSync.Runtime", "uLipSync" };
                foreach (string assemblyName in assemblyNames)
                {
                    try
                    {
                        var assembly = System.Reflection.Assembly.Load(assemblyName);
                        if (assembly != null)
                        {
                            info.HasULipSync = true;
                            break;
                        }
                    }
                    catch { /* Assembly not found */ }
                }
            }

            // Check for uLipSync directories
            if (!info.HasULipSync)
            {
                string[] searchPaths = {
                    "Assets/uLipSync",
                    "Assets/Plugins/uLipSync",
                    "Packages/com.hecomi.ulipsync"
                };

                foreach (string path in searchPaths)
                {
                    if (System.IO.Directory.Exists(path))
                    {
                        info.HasULipSync = true;
                        info.ULipSyncPath = path;
                        break;
                    }
                }
            }

            return info;
        }

        private void LogSystemDetection(LipSyncSystemInfo info)
        {
            log($"   uLipSync Available: {(info.HasULipSync ? "✅ YES" : "❌ NO")}");
            if (info.HasULipSync && !string.IsNullOrEmpty(info.ULipSyncPath))
            {
                log($"   Location: {info.ULipSyncPath}");
            }
        }

        // Static helpers to get the types from detection (single point of truth)
        public static System.Type GetULipSyncType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                var t1 = assembly.GetType("uLipSync.uLipSync", false);
                if (t1 != null) return t1;
            }
            return null;
        }
        public static System.Type GetULipSyncBlendShapeType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                var t2 = assembly.GetType("uLipSync.uLipSyncBlendShape", false);
                if (t2 != null) return t2;
            }
            return null;
        }
    }

    /// <summary>
    /// Holds information about the available LipSync systems.
    /// </summary>
    public class LipSyncSystemInfo
    {
        public bool HasULipSync = false;
        public bool HasULipSyncBlendShape = false;
        public string ULipSyncPath = "";
        public bool CanInstallULipSync { get { return !HasULipSync; } }
        public System.Type ULipSyncType;
        public System.Type ULipSyncBlendShapeType;
    }
}