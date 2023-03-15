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

        ModelInspectorView m_ModelInspectorView;

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
                m_ModelInspectorView?.Dispose();
                m_ModelInspectorView = window.CreateModelInspectorView();
                if (m_ModelInspectorView != null)
                {
                    m_ModelInspectorView.AddToClassList("unity-theme-env-variables");
                    m_ModelInspectorView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                    return m_ModelInspectorView;
                }
            }

            var placeholder = new VisualElement();
            placeholder.AddToClassList(ModelInspectorView.ussClassName);
            placeholder.AddStylesheet_Internal("ModelInspector.uss");
            return placeholder;
        }

        /// <inheritdoc />
        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            m_ModelInspectorView?.Dispose();
            m_ModelInspectorView = null;
        }
    }
}
