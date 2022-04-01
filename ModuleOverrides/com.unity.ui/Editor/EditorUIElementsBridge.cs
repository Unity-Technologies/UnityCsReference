// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class EditorUIElementsBridge : UIElementsBridge
    {
        public override void SetWantsMouseJumping(int value)
        {
            EditorGUIUtility.SetWantsMouseJumping(value);
        }
    }
}
