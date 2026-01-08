// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static class UIElementsEditorRuntimeUtility
    {
        public static void CreateRuntimePanelDebug(IPanel panel)
        {
            var panelDebug = new PanelDebug(panel);
            (panel as Panel).panelDebug = panelDebug;
        }
    }
}
