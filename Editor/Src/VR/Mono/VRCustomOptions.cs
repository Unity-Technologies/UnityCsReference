// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal.VR
{
    internal abstract class VRCustomOptions
    {
        SerializedProperty editorSettings;
        SerializedProperty playerSettings;

        internal SerializedProperty FindPropertyAssert(string name)
        {
            SerializedProperty property = null;
            if (editorSettings == null && playerSettings == null)
            {
                Debug.LogError("No existing VR settings. Failed to find:" + name);
            }
            else
            {
                bool found = false;
                if (editorSettings != null)
                {
                    property = editorSettings.FindPropertyRelative(name);
                    if (property != null)
                        found = true;
                }
                if (!found && playerSettings != null)
                {
                    property = playerSettings.FindPropertyRelative(name);
                    if (property != null)
                        found = true;
                }
                if (!found)
                {
                    Debug.LogError("Failed to find property:" + name);
                }
            }
            return property;
        }

        public bool IsExpanded { get; set; }
        public virtual void Initialize(SerializedObject settings)
        {
            Initialize(settings, "");
        }

        public virtual void Initialize(SerializedObject settings, string propertyName)
        {
            editorSettings = settings.FindProperty("vrEditorSettings");
            if (editorSettings != null && !string.IsNullOrEmpty(propertyName))
            {
                editorSettings = editorSettings.FindPropertyRelative(propertyName);
            }

            playerSettings = settings.FindProperty("vrSettings");
            if (playerSettings != null && !string.IsNullOrEmpty(propertyName))
            {
                playerSettings = playerSettings.FindPropertyRelative(propertyName);
            }
        }

        abstract public Rect Draw(BuildTargetGroup target, Rect rect);
        abstract public float GetHeight();
    }

    internal class VRCustomOptionsNone : VRCustomOptions
    {
        public override Rect Draw(BuildTargetGroup target, Rect rect) { return rect; }
        public override float GetHeight() { return 0.0f; }
    }
}
