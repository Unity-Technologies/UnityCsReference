// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    [CustomEditor(typeof(AdaptivePerformanceManagerSettings))]
    internal class AdaptivePerformanceManagerSettingsEditor : UnityEditor.Editor
    {
        AdaptivePerformanceLoaderOrderUI m_LoaderUi = new AdaptivePerformanceLoaderOrderUI();

        internal BuildTargetGroup BuildTarget
        {
            get;
            set;
        }

        public void Reload()
        {
            m_LoaderUi.CurrentBuildTargetGroup = BuildTargetGroup.Unknown;
        }

        /// <summary>
        /// <see href="https://docs.unity3d.com/ScriptReference/Editor.OnInspectorGUI.html">Editor Documentation</see>
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            serializedObject.Update();

            m_LoaderUi.OnGUI(BuildTarget);

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
