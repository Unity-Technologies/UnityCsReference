// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AudioAuthoring not yet converted
using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal partial class AudioMixerExposedParametersPopup : PopupWindowContent
    {
        // Shows pop up
        internal static void Popup(AudioMixerController controller, GUIStyle style, params GUILayoutOption[] options)
        {
            GUIContent content = GetButtonContent(controller);
            Rect buttonRect = GUILayoutUtility.GetRect(content, style, options);
            if (EditorGUI.DropdownButton(buttonRect, content, FocusType.Passive, style))
            {
                PopupWindow.Show(buttonRect, new AudioMixerExposedParametersPopup(controller));
            }
        }

        // Cache to prevent constructing string on every event
        static readonly GUIContent m_ButtonContent = EditorGUIUtility.TrTextContent("", "Audio Mixer parameters can be exposed to scripting. Select an Audio Mixer Group, right click one of its properties in the Inspector and select 'Expose'.");
        [NoAutoStaticsCleanup] // Display cache invalidation counter compared against controller state; -1 default re-syncs on next access, safe to persist across reload.
        static int m_LastNumExposedParams = -1;
        static GUIContent GetButtonContent(AudioMixerController controller)
        {
            if (controller.numExposedParameters != m_LastNumExposedParams)
            {
                m_ButtonContent.text = string.Format("Exposed Parameters ({0})", controller.numExposedParameters);
                m_LastNumExposedParams = controller.numExposedParameters;
            }
            return m_ButtonContent;
        }

        //
        private readonly AudioMixerExposedParameterView m_ExposedParametersView;

        AudioMixerExposedParametersPopup(AudioMixerController controller)
        {
            m_ExposedParametersView = new AudioMixerExposedParameterView(new ReorderableListWithRenameAndScrollView.State());
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            m_ExposedParametersView.OnMixerControllerChanged(controller);
            #pragma warning restore UAL0015
        }

        public override void OnGUI(Rect rect)
        {
            m_ExposedParametersView.OnEvent();
            m_ExposedParametersView.OnGUI(rect);
        }

        public override Vector2 GetWindowSize()
        {
            Vector2 size = m_ExposedParametersView.CalcSize();
            size.x = Math.Max(size.x, 125f);
            size.y = Math.Max(size.y, 23f);
            return size;
        }
    }
} // namespace
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
