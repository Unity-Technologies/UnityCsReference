using UnityEngine;
using UnityEngine.UIElements;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.UIElements
{
    static class Tooltip
    {
        [RequiredByNativeCode]
        internal static void SetTooltip(float mouseX, float mouseY)
        {
            //mouseX,mouseY are screen relative.
            GUIView view = GUIView.mouseOverView;
            if (view != null && view.windowBackend != null)
            {
                // Pick expect view relative coordinates.
                string tooltip;
                Rect screenRectPosition;
                if (view.windowBackend.GetTooltip(new Vector2(mouseX, mouseY) - view.screenPosition.position,
                    out tooltip, out screenRectPosition))
                {
                    GUIStyle.SetMouseTooltip(tooltip, screenRectPosition);
                }
            }
        }
    }
}
