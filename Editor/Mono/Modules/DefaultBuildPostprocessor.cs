// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

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

        public virtual bool SupportsInstallInBuildFolder()
        {
            return false;
        }

        public virtual bool SupportsLz4Compression()
        {
            return false;
        }

        public virtual bool SupportsScriptsOnlyBuild()
        {
            return true;
        }

        public virtual string PrepareForBuild(BuildOptions options, BuildTarget target)
        {
            return null;
        }

        public virtual void UpdateBootConfig(BuildTarget target, BootConfigData config, BuildOptions options)
        {
            config.Set("wait-for-native-debugger", "0");
            if (config.Get("player-connection-debug") == "1")
            {
                if (EditorUserBuildSettings.GetPlatformSettings(BuildPipeline.GetBuildTargetName(target),
                        BuildPlayerWindow.kSettingDebuggingWaitForManagedDebugger) == "true")
                {
                    config.Set("wait-for-managed-debugger", "1");
                }
                else
                {
                    config.Set("wait-for-managed-debugger", "0");
                }
            }
        }

        public virtual string GetExtension(BuildTarget target, BuildOptions options)
        {
            return string.Empty;
        }
    }
}
