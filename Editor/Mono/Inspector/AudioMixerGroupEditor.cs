// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Security.Permissions;
using UnityEngine;
using UnityEditor.Audio;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioMixerGroupController))]
    internal class AudioMixerGroupEditor : Editor
    {
        private AudioMixerEffectView m_EffectView = null;
        private readonly TickTimerHelper m_Ticker = new TickTimerHelper(1.0 / 20.0);
        public static readonly string kPrefKeyForShowCpuUsage = "AudioMixerShowCPU";

        void OnEnable()
        {
            EditorApplication.update += Update;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        public void Update()
        {
            if (EditorApplication.isPlaying && m_Ticker.DoTick())
            {
                Repaint();  // Ensure repaint to update vu meters and effects in playmode
            }
        }

        public override void OnInspectorGUI()
        {
            AudioMixerDrawUtils.InitStyles();
            if (m_EffectView == null)
                m_EffectView = new AudioMixerEffectView();

            AudioMixerGroupController group = target as AudioMixerGroupController;
            m_EffectView.OnGUI(group);
        }

        public override bool UseDefaultMargins()
        {
            // Makes inspector be full width
            return false;
        }

        internal override Rect DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            if (m_EffectView == null)
                return new Rect(r.xMax, r.y, 0, r.height);

            AudioMixerGroupController group = target as AudioMixerGroupController;

            var helpAndSettingsRect = base.DrawHeaderHelpAndSettingsGUI(r);
            float rectX = r.x + 44f;
            float rectWidth = (helpAndSettingsRect.x - r.x) - 44f;
            Rect rect = new Rect(rectX, r.yMax - 20f, rectWidth, 15f);
            GUI.Label(rect, GUIContent.Temp(group.controller.name), EditorStyles.miniLabel);

            return helpAndSettingsRect; // Return the helpAndSettings part, not the label under
        }

        // Add item to the context menu of the AudioMixerGroupController inspector header
        [MenuItem("CONTEXT/AudioMixerGroupController/Copy all effect settings to all snapshots")]
        static void CopyAllEffectToSnapshots(MenuCommand command)
        {
            AudioMixerGroupController group = command.context as AudioMixerGroupController;
            AudioMixerController controller = group.controller;
            if (controller == null)
                return;

            Undo.RecordObject(controller, "Copy all effect settings to all snapshots");
            controller.CopyAllSettingsToAllSnapshots(group, controller.TargetSnapshot);
        }

        [MenuItem("CONTEXT/AudioMixerGroupController/Toggle CPU usage display (only available on first editor instance)")]
        static void ShowCPUUsage(MenuCommand command)
        {
            bool value = EditorPrefs.GetBool(kPrefKeyForShowCpuUsage, false);
            EditorPrefs.SetBool(kPrefKeyForShowCpuUsage, !value);
        }
    }

    [CustomEditor(typeof(AudioMixerSnapshotController))]
    [CanEditMultipleObjects]
    internal class AudioMixerSnapshotControllerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
        }
    }
}
