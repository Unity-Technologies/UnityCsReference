// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AdaptivePerformance.Editor;
using UnityEngine.AdaptivePerformance.Basic;

namespace UnityEditor.AdaptivePerformance.Basic.Editor
{

    [CustomEditor(typeof(BasicProviderSettings))]
    internal class BasicProviderSettingsEditor : ProviderSettingsEditor
    {
        protected override BuildTargetGroup CurrentTargetGroup => BuildTargetGroup.Unknown;
        public override bool ShowTargetGroupSelection => false;
        public override string UnsupportedInfo => L10n.Tr("Adaptive Performance Basic provider is not supported on this platform");

        protected override bool IsAutoPerformanceModeAvailable => false;
        protected override bool IsBoostAvailable => false;
        protected override bool IsThermalActionDelayAvailable => false;
        public override void OnInspectorGUI()
        {
            DisplayProviderSettings();
        }
    }
}
