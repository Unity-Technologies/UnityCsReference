// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "Inspector", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.Panel, defaultWidth = 300, defaultHeight = 400)]
    [Icon( "Icons/GraphToolsFoundation/PanelsToolbar/Inspector.png")]
    sealed class ModelInspectorOverlay_Internal : Overlay
    {
        public const string idValue = "gtf-inspector";

        public ModelInspectorOverlay_Internal()
        {
            minSize = new Vector2(100, 100);
            maxSize = Vector2.positiveInfinity;
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var window = containerWindow as GraphViewEditorWindow;
            if (window != null)
            {
                var content = window.CreateModelInspectorView();
                if (content != null)
                {
                    content.AddToClassList("unity-theme-env-variables");
                    content.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                    return content;
                }
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(ModelInspectorView.ussClassName);
            placeholder.AddStylesheet_Internal("ModelInspector.uss");
            return placeholder;
        }
    }
}
