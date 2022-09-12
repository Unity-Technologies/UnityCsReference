// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;
using UnityEditor;
using UnityEngine.Scripting;

namespace UnityEditorInternal
{
    internal class PluginsHelper
    {
        [RequiredByNativeCode]
        public static bool CheckFileCollisions(BuildTarget buildTarget, string[] defineConstraints)
        {
            // Checks that plugins don't collide with each other
            IPluginImporterExtension pluginImporterExtension = null;
            if (ModuleManager.IsPlatformSupported(buildTarget))
                pluginImporterExtension = ModuleManager.GetPluginImporterExtension(buildTarget);
            if (pluginImporterExtension == null)
            {
                // Some platforms don't have platform specific settings for plugins, but we still want to check that plugins don't collide, use default path in this case
                if (BuildPipeline.GetBuildTargetGroup(buildTarget) == BuildTargetGroup.Standalone)
                    pluginImporterExtension = new DesktopPluginImporterExtension();
                else
                    pluginImporterExtension = new DefaultPluginImporterExtension(null);
            }

            if (pluginImporterExtension.CheckFileCollisions(BuildPipeline.GetBuildTargetName(buildTarget), defineConstraints))
                return true;

            return false;
        }
    }
}
