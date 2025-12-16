// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "Blackboard", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.Panel
        , defaultWidth = 300, defaultHeight = 400
     )]
    [Icon("Icons/GraphToolkit/PanelsToolbar/Blackboard.png")]
    sealed class BlackboardOverlay : OverlayWithView
    {
        public const string idValue = "gtf-blackboard";

        BlackboardView BlackboardView => OverlayWrapper?.RootView as BlackboardView;

        public override RootView RootView => BlackboardView;

        public BlackboardOverlay()
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
                    window.UnregisterBlackboardView(BlackboardView);
                    OverlayWrapper.DisposeRoot();
                }

                OverlayWrapper = window.CreateAndSetupBlackboardView();
                if (OverlayWrapper != null)
                    return OverlayWrapper;
            }

            var placeholder = new VisualElement();
            if (OverlayWrapper != null)
                OverlayWrapper.RootView = placeholder;

            placeholder.AddToClassList(BlackboardView.ussClassName);
            placeholder.AddPackageStylesheet("BlackboardView.uss");

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
                    window.UnregisterView(BlackboardView);

                OverlayWrapper.DisposeRoot();
                OverlayWrapper = null;
            }
        }
    }
}
