// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    sealed class EditorPanel : Panel
    {
        readonly EditorCursorManager m_CursorManager = new EditorCursorManager();
        static EditorContextualMenuManager s_ContextualMenuManager = new EditorContextualMenuManager();

        public static Panel FindOrCreate(ScriptableObject ownerObject)
        {
            var id = ownerObject.GetInstanceID();
            Panel panel;
            if (UIElementsUtility.TryGetPanel(id, out panel))
                return panel;
            panel = new EditorPanel(ownerObject);
            UIElementsUtility.RegisterCachedPanel(id, panel);
            return panel;
        }

        EditorPanel(ScriptableObject ownerObject)
            : base(ownerObject, ContextType.Editor, EventDispatcher.editorDispatcher, InitEditorUpdater)
        {
            name = ownerObject.GetType().Name;
            cursorManager = m_CursorManager;
            contextualMenuManager = s_ContextualMenuManager;
            panelDebug = new PanelDebug(this);
            uiElementsBridge = new EditorUIElementsBridge();
            UpdateScalingFromEditorWindow = true;
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

        private void CheckPanelScaling()
        {
            // Can be disabled for setting a manual scale for testing
            if (UpdateScalingFromEditorWindow)
            {

                //check that the scaling is up to date
                var windowScaling = GetBackingScaleFactor();
                if (windowScaling == null || windowScaling.Value == -1)
                {
                    Debug.Assert(windowScaling != null, "got -1 here!!");
                    // if we have -1, we were able to get to a GuiView, but the native call returned -1 because there is no containerWindow
                    // if the windowScaling == null we were simply not able to get to a GuiView
                    // in both cases, we want to update the scaling like the old behavior.
                    pixelsPerPoint = GUIUtility.pixelsPerPoint;
                }
                else
                {
                    Debug.Assert(pixelsPerPoint == windowScaling.Value, $"Scaling mismatch between the EditorWindow ({windowScaling.Value}) and the Editor Panel {name} ({pixelsPerPoint}). OnBackingScaleFactorChangedInternal was probably not call upon scaling change");
                    pixelsPerPoint = windowScaling.Value;// AKA silence the assert after the first occurence
                }
            }
        }
        public override void ValidateLayout()
        {
            CheckPanelScaling();
            base.ValidateLayout();
        }
        public override void Repaint(Event e)
        {
            CheckPanelScaling();
            base.Repaint(e);
        }
        public override void Render()
        {
            CheckPanelScaling();
            base.Render();
        }

        internal override Color HyperlinkColor
        {
            get
            {
                ColorUtility.TryParseHtmlString(EditorGUIUtility.GetHyperlinkColorForSkin(), out Color color);
                return color;
            }
        }
    
    }
}
