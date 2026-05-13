// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for overlay toolbars.
    /// </summary>
    [UnityRestricted]
    internal class Toolbar : ToolbarOverlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        /// <summary>
        /// Whether the overlay toolbar should be enabled or not.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The graph tool.
        /// </summary>
        protected GraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        protected GraphViewEditorWindow GraphEditorWindow => containerWindow as GraphViewEditorWindow;

        // The graph is loaded after the toolbar is built, meaning custom elements from
        // GraphToolbarElementAttribute are not present on the initial build.
        // We observe ToolStateComponent to detect when a graph is loaded and append the missing elements.
        StateObserver m_GraphLoadedObserver;

        public override void OnCreated()
        {
            base.OnCreated();
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public override void OnWillBeDestroyed()
        {
            rootVisualElement.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            rootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            base.OnWillBeDestroyed();
        }

        /// <inheritdoc />
        public override IEnumerable<string> toolbarElements
        {
            get
            {
                var window = GraphEditorWindow;
                if (window != null)
                    return window.GetToolbarDefinition(this)?.ElementIds;
                return Array.Empty<string>();
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var graphTool = GraphTool;
            if (graphTool == null || m_GraphLoadedObserver != null)
                return;

            m_GraphLoadedObserver = new ToolbarGraphLoadedObserver(
                graphTool.ToolState,
                OnGraphLoaded);

            graphTool.ObserverManager.RegisterObserver(m_GraphLoadedObserver);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_GraphLoadedObserver == null)
                return;
            GraphTool?.ObserverManager?.UnregisterObserver(m_GraphLoadedObserver);
            m_GraphLoadedObserver = null;
        }

        void OnGraphLoaded()
        {
            rootVisualElement.schedule.Execute(() =>
            {
                var overlayToolbar = rootVisualElement.Q<OverlayToolbar>();
                var definition = GraphEditorWindow?.GetToolbarDefinition(this);
                if (overlayToolbar == null || definition == null)
                    return;

                var toRemove = new List<VisualElement>();
                foreach (var child in overlayToolbar.Children())
                {
                    foreach (var kvp in definition.CustomElementMap)
                    {
                        if (child.GetType() == kvp.Value.ElementType)
                        {
                            toRemove.Add(child);
                            break;
                        }
                    }
                }

                foreach (var child in toRemove)
                    overlayToolbar.Remove(child);

                AppendCustomElements(overlayToolbar, definition);
            });
        }

        // BuildToolbar() is called by all three layout paths (horizontal, vertical, panel) to ensure
        // custom elements are always present when the user switches the overlay layout mode.
        // The graph loaded observer only covers the initial graph load. It cannot cover layout switches
        // since those are triggered by Unity's overlay system directly calling these methods.
        OverlayToolbar BuildToolbar()
        {
            var toolbar = new OverlayToolbar();
            var definition = GraphEditorWindow?.GetToolbarDefinition(this);
            if (definition == null) return toolbar;

            var baseContent = base.CreatePanelContent();
            var children = new List<VisualElement>();
            foreach (var child in baseContent.Children())
                children.Add(child);
            foreach (var child in children)
                toolbar.Add(child);

            AppendCustomElements(toolbar, definition);

            return toolbar;
        }

        void AppendCustomElements(OverlayToolbar toolbar, ToolbarDefinition definition)
        {
            var orderedElements = new List<(int order, Type elementType)>();
            foreach (var kvp in definition.CustomElementMap)
                orderedElements.Add((kvp.Value.Order, kvp.Value.ElementType));

            orderedElements.Sort((a, b) => a.order.CompareTo(b.order));

            foreach (var (_, elementType) in orderedElements)
            {
                VisualElement element = null;
                try
                {
                    element = Activator.CreateInstance(elementType) as VisualElement;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to create toolbar element {elementType.Name}: {e}");
                }

                if (element != null)
                {
                    if (element is IAccessContainerWindow windowElement)
                        windowElement.containerWindow = containerWindow;
                    toolbar.Add(element);
                }
            }
        }

        OverlayToolbar ICreateHorizontalToolbar.CreateHorizontalToolbarContent()
            => BuildToolbar();

        OverlayToolbar ICreateVerticalToolbar.CreateVerticalToolbarContent()
            => BuildToolbar();

        public override VisualElement CreatePanelContent()
            => BuildToolbar();

        /// <summary>
        /// Adds a stylesheet to the toolbar root visual element.
        /// </summary>
        /// <param name="stylesheet">The stylesheet to add.</param>
        /// <remarks>
        /// 'AddStylesheet' adds a stylesheet to the toolbar's root visual element, which enables custom styling of the toolbar.
        /// By adding a stylesheet, you can apply Unity Style Sheet (USS) styles to the visual elements, so the
        /// toolbar adheres to the desired design and layout specifications.
        /// </remarks>
        protected void AddStylesheet(StyleSheet stylesheet)
        {
            rootVisualElement.styleSheets.Add(stylesheet);
        }
    }
}
