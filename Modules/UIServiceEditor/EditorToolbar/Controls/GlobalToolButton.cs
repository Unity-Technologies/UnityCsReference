// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class GlobalToolButton<T> : ToolButton
        where T : EditorTool
    {
        public GlobalToolButton() : base(Tool.Custom)
        {
            GUIContent content = EditorToolManager.GetSingleton<T>().toolbarIcon ?? EditorToolUtility.GetIcon(typeof(T));
            tooltip = content.tooltip;
            iconElement.style.backgroundImage = new StyleBackground(content.image as Texture2D);
        }

        protected override void SetToolActive()
        {
            if (!IsActiveTool())
                ToolManager.SetActiveTool<T>();
        }

        protected override bool IsActiveTool()
        {
            return ToolManager.activeToolType == typeof(T) && base.IsActiveTool();
        }
    }
}
