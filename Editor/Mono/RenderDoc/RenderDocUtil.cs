// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    internal class RenderDocUtil
    {
        public const string captureRenderDocShortcutID = "Window/Capture frame with RenderDoc";
        public const string openInRenderDocTooltip = "Capture the current view and open in RenderDoc";

        public static GUIContent LoadRenderDocMenuItem => EditorGUIUtility.TrTextContent($"Load RenderDoc " +
            $"{KeyCombination.SequenceToMenuString(ShortcutManager.instance.GetShortcutBinding(captureRenderDocShortcutID).keyCombinationSequence)}");
    }
}
