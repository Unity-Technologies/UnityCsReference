// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.AdaptivePerformance.Editor
{
    [CustomEditor(typeof(AdaptivePerformanceGeneralSettingsPerBuildTarget))]
    internal class AdaptivePerformanceGeneralSettingsPerBuildTargetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Intentionally empty - settings are managed via Project Settings > Adaptive Performance
        }
    }
}
