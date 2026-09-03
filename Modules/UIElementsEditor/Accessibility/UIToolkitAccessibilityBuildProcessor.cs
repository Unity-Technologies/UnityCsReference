// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Ships the experimental accessibility setting into player builds: when the setting is on, a
    /// flag TextAsset ("1") is placed in a temporary Resources folder for the duration of the
    /// build, and <see cref="UITKAccessibilityBridge"/> reads it at the first panel attach in
    /// players.
    /// </summary>
    /// <remarks>
    /// Why a TextAsset and not a ScriptableObject is explained on
    /// <see cref="UITKAccessibilityBridge.playerConfigurationResourceName"/>, next to the code
    /// that reads it. Preprocessing always cleans stale leftovers first, so a build that failed
    /// hard enough to skip postprocessing self-heals on the next build.
    /// </remarks>
    internal class UIToolkitAccessibilityBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string k_TempFolder = "Assets/UIToolkitBuildTemp";
        const string k_TempResourcesFolder = k_TempFolder + "/Resources";
        internal const string tempAssetPath =
            k_TempResourcesFolder + "/" + UITKAccessibilityBridge.playerConfigurationResourceName + ".txt";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.buildType == BuildType.AssetBundle)
                return;

            RemoveShippedConfiguration();

            if (UIToolkitAccessibilitySettingsEditor.generateHierarchies)
                AddShippedConfiguration();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.buildType == BuildType.AssetBundle)
                return;

            RemoveShippedConfiguration();
        }

        internal static void AddShippedConfiguration()
        {
            Directory.CreateDirectory(k_TempResourcesFolder);
            File.WriteAllText(tempAssetPath, "1");
            AssetDatabase.ImportAsset(tempAssetPath);
        }

        internal static void RemoveShippedConfiguration()
        {
            if (AssetDatabase.IsValidFolder(k_TempFolder))
                AssetDatabase.DeleteAsset(k_TempFolder);
        }
    }
}
