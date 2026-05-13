// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.UIElements
{
    [NativeHeader("Editor/Src/TooltipManager.bindings.h")]
    static class TooltipManager
    {
        [RequiredByNativeCode]
        internal static void GetTooltip(float mouseX, float mouseY)
        {
            //mouseX,mouseY are screen relative.
            GUIView view = GUIView.mouseOverView;
            if (view != null && view.windowBackend != null)
            {
                // Pick expect view relative coordinates.
                if (view.windowBackend.GetTooltip(new Vector2(mouseX, mouseY) - view.screenPosition.position,
                    out string tooltip, out var windowRectPosition))
                {
                    SetTooltip(tooltip, new(windowRectPosition.position + view.screenPosition.position, windowRectPosition.size));
                }
            }
        }

        [FreeFunction("TooltipManagerBindings::SetTooltip")]
        private static extern void SetTooltip(string tooltip, Rect rect);
    }
}
