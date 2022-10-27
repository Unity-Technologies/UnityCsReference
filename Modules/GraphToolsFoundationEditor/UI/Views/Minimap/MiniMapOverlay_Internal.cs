// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "MiniMap",
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.Panel, defaultWidth = 200, defaultHeight = 150)]
    [Icon( "Icons/GraphToolsFoundation/PanelsToolbar/MiniMap.png")]
    sealed class MiniMapOverlay_Internal : Overlay
    {
        public const string idValue = "gtf-minimap";

        public MiniMapOverlay_Internal()
        {
            minSize = new Vector2(100, 100);
            maxSize = Vector2.positiveInfinity;
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var window = containerWindow as GraphViewEditorWindow;
            if (window != null && window.GraphView != null)
            {
                var content = window.CreateMiniMapView();
                content.AddToClassList("unity-theme-env-variables");
                content.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                return content;
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(MiniMapView.ussClassName);
            placeholder.AddStylesheet_Internal("MiniMapView.uss");
            return placeholder;
        }
    }
}
