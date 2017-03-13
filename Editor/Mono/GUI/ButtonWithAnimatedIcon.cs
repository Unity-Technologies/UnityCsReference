// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        internal static bool ButtonWithRotatedIcon(Rect rect, GUIContent guiContent, float iconAngle, bool mouseDownButton, GUIStyle style)
        {
            // Button with text and background (no icon - it's rendered separately below)
            bool buttonPressed;
            if (mouseDownButton)
                buttonPressed = DropdownButton(rect, GUIContent.Temp(guiContent.text, guiContent.tooltip), FocusType.Passive, style);
            else
                buttonPressed = GUI.Button(rect, GUIContent.Temp(guiContent.text, guiContent.tooltip), style);

            // Icon (rendered left of text)
            if (Event.current.type == EventType.Repaint && guiContent.image != null)
            {
                Vector2 iconSize = EditorGUIUtility.GetIconSize();
                if (iconSize == Vector2.zero)
                {
                    iconSize.x = iconSize.y = rect.height - style.padding.vertical;
                }

                const float spaceBetweenIconAndText = 3f;
                const float spaceBetweenIconAndTop = 1f;
                Rect iconRect = new Rect(rect.x + style.padding.left - spaceBetweenIconAndText - iconSize.x, rect.y + style.padding.top + spaceBetweenIconAndTop, iconSize.x, iconSize.y);
                if (iconAngle == 0f)
                {
                    GUI.DrawTexture(iconRect, guiContent.image);
                }
                else
                {
                    Matrix4x4 prevMatrix = GUI.matrix;
                    GUIUtility.RotateAroundPivot(iconAngle, iconRect.center);
                    GUI.DrawTexture(iconRect, guiContent.image);
                    GUI.matrix = prevMatrix;
                }
            }
            return buttonPressed;
        }
    }

    // Ensure to call Clear() before setting a instance to null to prevent mem leaking
    // due to CallbackController using a delegate for update calls (if not de-registering this
    // delegate, it will keep the instance from being gc'ed)
    internal class ButtonWithAnimatedIconRotation
    {
        readonly CallbackController m_CallbackController; // used for continuous repaints
        readonly Func<float> m_AngleCallback;
        readonly bool m_MouseDownButton;

        public ButtonWithAnimatedIconRotation(Func<float> angleCallback, Action repaintCallback, float repaintsPerSecond, bool mouseDownButton)
        {
            m_CallbackController = new CallbackController(repaintCallback, repaintsPerSecond);
            m_AngleCallback = angleCallback;
            m_MouseDownButton = mouseDownButton;
        }

        public bool OnGUI(Rect rect, GUIContent guiContent, bool animate, GUIStyle style)
        {
            if (animate && !m_CallbackController.active)
                m_CallbackController.Start();
            if (!animate && m_CallbackController.active)
                m_CallbackController.Stop();

            float iconAngle = animate ? m_AngleCallback() : 0f;
            return EditorGUI.ButtonWithRotatedIcon(rect, guiContent, iconAngle, m_MouseDownButton, style);
        }

        public void Clear()
        {
            m_CallbackController.Stop();
        }
    }
}
