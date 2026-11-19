// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // Element that shows an EditorAuthoringPanel, and through which the panel reaches the screen.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal interface IEditorAuthoringPanelHost
    {
        // Attached to an editor window; receives the events forwarded to the panel.
        VisualElement hostElement { get; }

        Rect PanelToHost(Rect panelRect);
    }

    // Editor-context panel that renders into a texture instead of a GUIView, for authoring previews of editor UI.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class EditorAuthoringPanel : Panel, IAuthoringPanel
    {
        // OS menus are placed in screen coordinates, so they are shown against the host element.
        sealed class HostedContextualMenuManager : EditorContextualMenuManager
        {
            protected internal override void DoDisplayMenu(DropdownMenu menu, EventBase triggerEvent)
            {
                if (!TryGetMenuHostRect(triggerEvent, out var hostElement, out var hostRect))
                    return;

                // The panel is drawn by its host's view, which gets no other chance to repaint while the menu is up.
                if (menu.repaintPanelBeforeDisplay && hostElement.elementPanel?.ownerObject is GUIView view)
                    view.RepaintImmediately();

                menu.DoDisplayEditorMenu(triggerEvent, hostRect, hostElement);
            }
        }

        internal static bool TryGetMenuHostRect(EventBase triggerEvent, out VisualElement hostElement, out Rect hostRect)
        {
            var target = triggerEvent.elementTarget;
            var host = (target?.elementPanel as EditorAuthoringPanel)?.host;
            hostElement = host?.hostElement;
            if (hostElement?.panel == null)
            {
                hostRect = default;
                return false;
            }

            var position = triggerEvent is IPointerEvent pointerEvent
                ? (Vector2)pointerEvent.position
                : target.worldBound.center;
            hostRect = hostElement.LocalToWorld(host.PanelToHost(new Rect(position, Vector2.zero)));
            return true;
        }

        [NoAutoStaticsCleanup]
        static readonly HostedContextualMenuManager s_ContextualMenuManager = new HostedContextualMenuManager();

        // Own dispatcher: events forwarded from a host editor panel must not queue behind the host's dispatch.
        public EditorAuthoringPanel(ScriptableObject ownerObject)
            : base(ownerObject, ContextType.Editor, EventDispatcher.CreateDefault(), EditorPanel.InitEditorUpdater)
        {
            TimeSinceStartupFunc = () => EditorApplication.timeSinceStartup;

            name = ownerObject.GetType().Name;
            contextualMenuManager = s_ContextualMenuManager;
            uiElementsBridge = new EditorUIElementsBridge();

            // OS menus are placed in screen coordinates, which a panel drawn into a texture cannot provide.
            CreateMenuFunctor = () => new GenericDropdownMenu();

            beforeAnyRepaint += OnBeforeAnyRepaint;
        }

        public RenderTexture targetTexture { get; set; }

        public IEditorAuthoringPanelHost host { get; set; }

        // IMGUIContainer only draws during an IMGUI repaint, which exists only while the host's GUIView repaints.
        public event Action hostRepainting;

        // Also true during the host's layout, so a host resize can render the panel before the host draws it.
        public bool isInHostRepaint => m_IsInHostRepaintingCallback || hostPanel is Panel { isRepainting: true };

        bool m_IsInHostRepaintingCallback;

        public BaseVisualElementPanel hostPanel => host?.hostElement?.elementPanel;

        void OnBeforeAnyRepaint(Panel repaintedPanel)
        {
            if (repaintedPanel == this || repaintedPanel != hostPanel)
                return;

            m_IsInHostRepaintingCallback = true;
            try
            {
                hostRepainting?.Invoke();
            }
            finally
            {
                m_IsInHostRepaintingCallback = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            beforeAnyRepaint -= OnBeforeAnyRepaint;
            base.Dispose(disposing);
        }

        public override void Render()
        {
            if (targetTexture == null || ownerObject == null)
                return;

            var oldCamera = Camera.current;
            var oldTarget = RenderTexture.active;
            var oldViewport = UnityEngine.UIElements.UIR.Utility.GetActiveViewport();

            Camera.SetupCurrent(null);
            RenderTexture.active = targetTexture;
            GL.Viewport(new Rect(0, 0, targetTexture.width, targetTexture.height));
            try
            {
                base.Render();
            }
            finally
            {
                Camera.SetupCurrent(oldCamera);
                RenderTexture.active = oldTarget;
                GL.Viewport(new Rect(oldViewport.x, oldViewport.y, oldViewport.width, oldViewport.height));
            }
        }

        protected override void UpdateForRepaint()
        {
            visualTreeUpdater.UpdateForEditor();
            base.UpdateForRepaint();
        }
    }
}
