// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using NiceIO;
using UnityEngine;

namespace UnityEditor.Modules
{
    internal class DefaultBuildProperties : BuildProperties
    {
        public override DeploymentTargetRequirements GetTargetRequirements() { return null; }
    }

    internal abstract class DefaultBuildPostprocessor
        : IBuildPostprocessor
    {
        static readonly string kXrBootSettingsKey = "xr-boot-settings";
        public virtual void LaunchPlayer(BuildLaunchPlayerArgs args)
        {
            throw new NotSupportedException();
        }

        // Supports legacy interface before BuildProperties was introduced
        public virtual void PostProcess(BuildPostProcessArgs args)
        {
        }

        public virtual void PostProcess(BuildPostProcessArgs args, out BuildProperties outProperties)
        {
            PostProcess(args);

            // NOTE: For some reason, calling PostProcess seems like it can trigger this object to be GC'd
            //  so create is just before returning
            outProperties = ScriptableObject.CreateInstance<DefaultBuildProperties>();
        }

        // Note! Only called when running a bee build.
        public virtual void PostProcessCompletedBuild(BuildPostProcessArgs args)
        {
        }

        public virtual bool SupportsInstallInBuildFolder()
        {
            return false;
        }

        public virtual bool SupportsLz4Compression()
        {
            return false;
        }

        public virtual Compression GetDefaultCompression()
        {
            return Compression.None;
        }

        public virtual bool SupportsScriptsOnlyBuild()
        {
            return true;
        }

        public virtual bool UsesBeeBuild() => false;

        public virtual string PrepareForBuild(BuildPlayerOptions buildPlayerOptions)
        {
            return null;
        }

        public virtual void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options)
        {
            config.Set("wait-for-native-debugger", "0");
            if (config.Get("player-connection-debug") == "1")
            {
                config.Set("wait-for-managed-debugger", EditorUserBuildSettings.waitForManagedDebugger ? "1" : "0");
                config.Set("managed-debugger-fixed-port", EditorUserBuildSettings.managedDebuggerFixedPort.ToString());
            }

            config.Set("hdr-display-enabled", PlayerSettings.useHDRDisplay ? "1" : "0");
            if (BuildPipeline.IsFeatureSupported("ENABLE_SCRIPTING_GC_WBARRIERS", target))
            {
                if (PlayerSettings.gcWBarrierValidation)
                    config.AddKey("validate-write-barriers");
                if (PlayerSettings.gcIncremental)
                    config.Set("gc-max-time-slice", "3");
            }

            string xrBootSettings = UnityEditor.EditorUserBuildSettings.GetPlatformSettings(BuildPipeline.GetBuildTargetName(target), kXrBootSettingsKey);
            if (!String.IsNullOrEmpty(xrBootSettings))
            {
                var bootSettings = xrBootSettings.Split(';');
                foreach (var bootSetting in bootSettings)
                {
                    var setting = bootSetting.Split(':');
                    if (setting.Length == 2 && !String.IsNullOrEmpty(setting[0]) && !String.IsNullOrEmpty(setting[1]))
                    {
                        config.Set(setting[0], setting[1]);
                    }
                }
            }


            if ((options & BuildOptions.Development) != 0)
            {
                if ((options & BuildOptions.EnableDeepProfilingSupport) != 0)
                {
                    config.Set("profiler-enable-deep-profiling-support", "1");
                }
            }
        }

        protected static string GetIl2CppDataBackupFolderName(BuildPostProcessArgs args)
        {
            return $"{args.installPath.ToNPath().FileNameWithoutExtension}_BackUpThisFolder_ButDontShipItWithYourGame";
        }

        public virtual string GetExtension(BuildTarget target, int subtarget, BuildOptions options)
        {
            return string.Empty;
        }
    }
}
