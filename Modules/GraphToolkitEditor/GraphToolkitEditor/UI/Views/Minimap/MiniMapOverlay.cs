// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "MiniMap",
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.Panel
        , defaultWidth = 200, defaultHeight = 150
     )]
    [Icon("Icons/GraphToolkit/PanelsToolbar/MiniMap.png")]
    [UnityRestricted]
    internal sealed class MiniMapOverlay : OverlayWithView
    {
        public const string idValue = "gtf-minimap";

        MiniMapView MiniMapView => OverlayWrapper?.RootView as MiniMapView;

        public override RootView RootView => MiniMapView;

        public MiniMapOverlay()
        {
            minSize = new Vector2(100, 100);
            maxSize = Vector2.positiveInfinity;
        }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            var window = containerWindow as GraphViewEditorWindow;
            if (window != null )
            {
                if (OverlayWrapper != null)
                {
                    window.UnregisterView(MiniMapView);
                    OverlayWrapper.DisposeRoot();
                }

                OverlayWrapper = window.CreateAndSetupMiniMapView();
                if (window.GraphView != null)
                {
                    if (MiniMapView != null)
                    {
                        return OverlayWrapper;
                    }
                }
            }

            var placeholder = new VisualElement();
            if (OverlayWrapper != null)
                OverlayWrapper.RootView = placeholder;

            placeholder.AddToClassList(MiniMapView.ussClassName);
            placeholder.AddPackageStylesheet("MiniMapView.uss");

            return OverlayWrapper ?? placeholder;
        }

        /// <inheritdoc />
        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();

            if (OverlayWrapper != null)
            {
                var window = containerWindow as GraphViewEditorWindow;
                if (window != null)
                    window.UnregisterView(MiniMapView);

                OverlayWrapper.DisposeRoot();
                OverlayWrapper = null;
            }
        }
    }
}
