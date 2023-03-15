// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    [Overlay(typeof(GraphViewEditorWindow), idValue, "Blackboard", defaultDisplay = true,
        defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.Panel, defaultWidth = 300, defaultHeight = 400)]
    [Icon( "Icons/GraphToolsFoundation/PanelsToolbar/Blackboard.png")]
    sealed class BlackboardOverlay_Internal : Overlay
    {
        public const string idValue = "gtf-blackboard";

        BlackboardView m_BlackboardView;

        public BlackboardOverlay_Internal()
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
                m_BlackboardView?.Dispose();
                m_BlackboardView = window.CreateBlackboardView();
                if (m_BlackboardView != null)
                {
                    m_BlackboardView.AddToClassList("unity-theme-env-variables");
                    m_BlackboardView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                    return m_BlackboardView;
                }
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(BlackboardView.ussClassName);
            placeholder.AddStylesheet_Internal("BlackboardView.uss");
            return placeholder;
        }

        /// <inheritdoc />
        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            m_BlackboardView?.Dispose();
            m_BlackboardView = null;
        }
    }
}
