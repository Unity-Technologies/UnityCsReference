// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal sealed class EditorPanel : Panel
    {
        readonly EditorCursorManager m_CursorManager = new EditorCursorManager();
        [NoAutoStaticsCleanup]
        static EditorContextualMenuManager s_ContextualMenuManager = new EditorContextualMenuManager();

        public static Panel FindOrCreate(ScriptableObject ownerObject)
        {
            var id = ownerObject.GetEntityId();
            Panel panel;
            if (UIElementsUtility.TryGetPanel(id, out panel))
                return panel;
            panel = new EditorPanel(ownerObject);
            UIElementsUtility.RegisterCachedPanel(id, panel);
            return panel;
        }

        public EditorPanel(ScriptableObject ownerObject)
            : base(ownerObject, ContextType.Editor, EventDispatcher.editorDispatcher, InitEditorUpdater)
        {
            TimeSinceStartupFunc = () => EditorApplication.timeSinceStartup;

            name = ownerObject.GetType().Name;
            cursorManager = m_CursorManager;
            contextualMenuManager = s_ContextualMenuManager;
            panelDebug = new PanelDebug(this);
            uiElementsBridge = new EditorUIElementsBridge();
            CreateMenuFunctor = () => new GenericOSMenu();
            hostViewChecks = new HostViewChecks(this);
        }

        public static void InitEditorUpdater(BaseVisualElementPanel panel, VisualTreeUpdater visualTreeUpdater)
        {
            var editorUpdater = new VisualTreeEditorUpdater(panel);
            visualTreeUpdater.visualTreeEditorUpdater = editorUpdater;

            var assetTracker = editorUpdater.GetUpdater(VisualTreeEditorUpdatePhase.AssetChange) as ILiveReloadSystem;
            panel.liveReloadSystem = assetTracker;
        }

        internal float? GetBackingScaleFactor()
        {
            return GetBackingScaleFactor(ownerObject);
        }

        float? GetBackingScaleFactor(object obj)
        {
            return obj switch
            {
                GUIView view => view.GetBackingScaleFactor(),
                View view => GetBackingScaleFactor(view.parent), //MainView and SplitView are not GUIView
                EditorWindow editorWindow => GetBackingScaleFactor(editorWindow?.m_Parent),
                IEditorWindowModel ewm => GetBackingScaleFactor(ewm.window),
                _ => null,
            };
        }

        internal HostViewChecks hostViewChecks { get; }

        // Set by UI test fixtures whose window size the native window may not have applied.
        internal bool skipRenderTargetCheckForTests { get; set; }

        public override void Render()
        {
            // macOS clips the native view to its screen in GUIView::SetPosition and the managed size never follows.
            hostViewChecks.CheckRenderTarget();
            base.Render();
        }

        protected override void UpdateForRepaint()
        {
            visualTreeUpdater.UpdateForEditor();
            base.UpdateForRepaint();
        }
    }
}
