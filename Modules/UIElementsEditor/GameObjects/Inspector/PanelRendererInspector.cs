// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(PanelRenderer))]
    class PanelRendererInspector : PanelComponentInspectorBase
    {
        protected override Type parentObjectType => typeof(PanelRenderer);

        public override void OnInspectorGUI()
        {
            // UUM-137205:  Explicitly prevent the default IMGUI inspector (which would shows the material)
        }

        [DrawGizmo(GizmoType.Selected)]
        static void PanelRendererDrawGizmosSelected(PanelRenderer panelRenderer, GizmoType gizmoType)
        {
            if (panelRenderer == null || panelRenderer.rootVisualElement == null)
                return;

            PanelComponentUtils.DrawGizmoBounds(panelRenderer, panelRenderer.pixelsPerUnit);
        }
    }
}
