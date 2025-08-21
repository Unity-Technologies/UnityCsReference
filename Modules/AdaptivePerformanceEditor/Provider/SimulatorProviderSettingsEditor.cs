// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AdaptivePerformance.Editor;

namespace UnityEditor.AdaptivePerformance.Simulator.Editor
{
    /// <summary>
    /// This is custom Editor for Simulator Provider Settings.
    /// </summary>
    [CustomEditor(typeof(SimulatorProviderSettings))]
    public class SimulatorProviderSettingsEditor : ProviderSettingsEditor
    {
        /// <summary>
        /// Override of Editor callback to display custom settings.
        /// </summary>
        protected override BuildTargetGroup CurrentTargetGroup => BuildTargetGroup.Standalone;

        /// <summary>
        /// Shows the setting for simulator provider.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DisplayProviderSettings();
        }
    }
}
