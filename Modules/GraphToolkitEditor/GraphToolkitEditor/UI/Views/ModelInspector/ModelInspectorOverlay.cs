// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "Graph Inspector", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.Panel
        , defaultWidth = 300, defaultHeight = 400
     )]
    [Icon("Icons/GraphToolkit/PanelsToolbar/Inspector.png")]
    sealed class ModelInspectorOverlay : OverlayWithView
    {
        public const string idValue = "gtf-inspector";

        ModelInspectorView m_ModelInspectorView => OverlayWrapper?.RootView as ModelInspectorView;

        public override RootView RootView => m_ModelInspectorView;

        public ModelInspectorOverlay()
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
                if (OverlayWrapper != null)
                {
                    window.UnregisterView(m_ModelInspectorView);
                    OverlayWrapper.DisposeRoot();
                }

                OverlayWrapper = window.CreateAndSetupInspectorView();
                if (m_ModelInspectorView != null)
                    return OverlayWrapper;
            }

            var placeholder = new VisualElement();
            if (OverlayWrapper != null)
                OverlayWrapper.RootView = placeholder;

            placeholder.AddToClassList(ModelInspectorView.ussClassName);
            placeholder.AddPackageStylesheet("ModelInspector.uss");

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
                    window.UnregisterView(m_ModelInspectorView);

                OverlayWrapper.DisposeRoot();
                OverlayWrapper = null;
            }
        }
    }
}
