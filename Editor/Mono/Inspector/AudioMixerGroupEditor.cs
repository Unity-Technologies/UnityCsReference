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

        internal override void DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            if (m_EffectView == null)
                return;

            AudioMixerGroupController group = target as AudioMixerGroupController;

            base.DrawHeaderHelpAndSettingsGUI(r);
            Rect rect = new Rect(r.x + 44f, r.yMax - 20f, r.width - 50f, 15f);
            GUI.Label(rect, GUIContent.Temp(group.controller.name), EditorStyles.miniLabel);
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
