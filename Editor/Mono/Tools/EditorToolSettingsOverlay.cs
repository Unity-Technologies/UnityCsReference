// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    [Overlay(typeof(SceneView), "Tool Settings", true, priority = (int)OverlayPriority.ToolSettings, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top, defaultDockIndex = 0, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/ToolSettings.png")]
    sealed class EditorToolSettingsOverlay : Overlay, ICreateToolbar, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        Editor m_ToolEditor, m_ContextEditor;
        Editor m_DefaultToolEditor, m_DefaultContextEditor;

        protected internal override Layout supportedLayouts
        {
            get
            {
                var ret = Layout.Panel;

                if (m_ToolEditor == null && m_ContextEditor == null)
                    return ret;

                if ((m_ToolEditor is ICreateHorizontalToolbar || m_ToolEditor is ICreateToolbar || m_ToolEditor is IOverrideToolbar)
                    && (m_ContextEditor is ICreateHorizontalToolbar || m_ContextEditor is ICreateToolbar || m_ContextEditor is IOverrideToolbar))
                    ret |= Layout.HorizontalToolbar;

                if ((m_ToolEditor is ICreateVerticalToolbar || m_ToolEditor is ICreateToolbar || m_ToolEditor is IOverrideToolbar)
                    && (m_ContextEditor is ICreateVerticalToolbar || m_ContextEditor is ICreateToolbar || m_ContextEditor is IOverrideToolbar))
                    ret |= Layout.VerticalToolbar;

                return ret;
            }
        }

        public EditorToolSettingsOverlay()
        {
            ToolManager.activeToolChanged += OnToolChanged;
            ToolManager.activeContextChanged += OnToolChanged;
            CreateEditor();
        }

        public override void OnWillBeDestroyed()
        {
            UnityObject.DestroyImmediate(m_ToolEditor);
            UnityObject.DestroyImmediate(m_ContextEditor);
            UnityObject.DestroyImmediate(m_DefaultToolEditor);
            UnityObject.DestroyImmediate(m_DefaultContextEditor);
        }

        void CreateEditor()
        {
            UnityObject.DestroyImmediate(m_ContextEditor);
            UnityObject.DestroyImmediate(m_ToolEditor);

            m_ContextEditor = Editor.CreateEditor(EditorToolManager.activeToolContext);
            m_ToolEditor = Editor.CreateEditor(EditorToolManager.activeTool);
            
            UnityObject.DestroyImmediate(m_DefaultContextEditor);
            UnityObject.DestroyImmediate(m_DefaultToolEditor);
            m_DefaultContextEditor = Editor.CreateEditor(EditorToolManager.activeToolContext, typeof(GameObjectToolContextCustomEditor));
            // ManipulationToolCustomEditor, despite its name, is the default editor for ALL EditorTools
            m_DefaultToolEditor = Editor.CreateEditor(EditorToolManager.activeTool, typeof(ManipulationToolCustomEditor));
        }

        void OnToolChanged()
        {
            CreateEditor();
            RebuildContent();
        }

        // As a rule, Overlays should be compatible with toolbars or not. This class is an exception, hence the
        // possibility for ICreateToolbar implementations to return null.

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            var root = new OverlayToolbar();

            if(m_ContextEditor is ICreateHorizontalToolbar ctx)
                root.Add(ctx.CreateHorizontalToolbarContent());
            else if(m_ContextEditor is ICreateToolbar ctxToolbar)
                root.Add(EditorToolbar.CreateOverlay(ctxToolbar.toolbarElements, containerWindow));
            else if (m_ContextEditor is IOverrideToolbar)
            {
                var elements = EditorToolbarUtility.GetToolbarOverrideElementIds(m_ContextEditor, m_DefaultContextEditor, OverridableToolbar.ToolSettings);
                root.Add(EditorToolbar.CreateOverlay(elements, containerWindow));
            }

            if(m_ToolEditor is ICreateHorizontalToolbar tool)
                root.Add(tool.CreateHorizontalToolbarContent());
            else if(m_ToolEditor is ICreateToolbar toolToolbar)
                root.Add(EditorToolbar.CreateOverlay(toolToolbar.toolbarElements, containerWindow));
            if (m_ToolEditor is IOverrideToolbar)
            {
                var elements = EditorToolbarUtility.GetToolbarOverrideElementIds(m_ToolEditor, m_DefaultToolEditor, OverridableToolbar.ToolSettings);
                root.Add(EditorToolbar.CreateOverlay(elements, containerWindow));
            }
            
            return root;
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            var root = new OverlayToolbar();

            if (m_ContextEditor is ICreateVerticalToolbar ctx)
                root.Add(ctx.CreateVerticalToolbarContent());
            else if (m_ContextEditor is ICreateToolbar ctxToolbar)
                root.Add(EditorToolbar.CreateOverlay(ctxToolbar.toolbarElements, containerWindow));
            else if (m_ContextEditor is IOverrideToolbar)
            {
                var elements = EditorToolbarUtility.GetToolbarOverrideElementIds(m_ContextEditor, m_DefaultContextEditor, OverridableToolbar.ToolSettings);
                root.Add(EditorToolbar.CreateOverlay(elements, containerWindow));
            }
            
            if (m_ToolEditor is ICreateVerticalToolbar tool)
                root.Add(tool.CreateVerticalToolbarContent());
            else if (m_ToolEditor is ICreateToolbar toolToolbar)
                root.Add(EditorToolbar.CreateOverlay(toolToolbar.toolbarElements, containerWindow));
            else if (m_ToolEditor is IOverrideToolbar)
            {
                var elements = EditorToolbarUtility.GetToolbarOverrideElementIds(m_ToolEditor, m_DefaultToolEditor, OverridableToolbar.ToolSettings);
                root.Add(EditorToolbar.CreateOverlay(elements, containerWindow));
            }

            return root;
        }
        
        public IEnumerable<string> toolbarElements
        {
            get
            {
                if (m_ContextEditor is IOverrideToolbar)
                {
                    foreach (var id in EditorToolbarUtility.GetToolbarOverrideElementIds(m_ContextEditor, m_DefaultContextEditor, OverridableToolbar.ToolSettings))
                        yield return id;
                }
                else if (m_ContextEditor is ICreateToolbar ctxToolbar)
                {
                    foreach (var id in ctxToolbar.toolbarElements)
                        yield return id;
                }

                if (m_ToolEditor is IOverrideToolbar)
                {
                    foreach (var id in EditorToolbarUtility.GetToolbarOverrideElementIds(m_ToolEditor, m_DefaultToolEditor, OverridableToolbar.ToolSettings))
                        yield return id;
                }
                else if (m_ToolEditor is ICreateToolbar toolToolbar)
                {
                    foreach (var id in toolToolbar.toolbarElements)
                        yield return id;
                }
            }
        }

        VisualElement GetPanelContent(Editor editor, Editor defaultEditor)
        {
            if (editor == null)
                return null;

            var root = editor.CreateInspectorGUI();

            if (root != null)
                return root;

            // If the Editor does not provide an OnInspectorGUI, try to fall back to a toolbar.
            var inspector = editor.GetType().GetMethod("OnInspectorGUI",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (inspector == null || inspector.DeclaringType != editor.GetType())
            {
                if (editor is ICreateToolbar toolbar)
                    return EditorToolbar.CreateOverlay(toolbar.toolbarElements, containerWindow);
                
                if (editor is IOverrideToolbar)
                {
                    var elements = EditorToolbarUtility.GetToolbarOverrideElementIds(editor, defaultEditor, OverridableToolbar.ToolSettings);
                    return EditorToolbar.CreateOverlay(elements, containerWindow);
                }

                if (editor is ICreateHorizontalToolbar horizontal)
                    return horizontal.CreateHorizontalToolbarContent();

                if (editor is ICreateVerticalToolbar vertical)
                    return vertical.CreateVerticalToolbarContent();
            }

            return new IMGUIContainer(editor.OnInspectorGUI);
        }

        public override VisualElement CreatePanelContent()
        {
            var context = GetPanelContent(m_ContextEditor, m_DefaultContextEditor);
            var tool = GetPanelContent(m_ToolEditor, m_DefaultToolEditor);
            var root = context is OverlayToolbar && tool is OverlayToolbar
                ? new OverlayToolbar()
                : new VisualElement();
            root.Add(context);
            root.Add(tool);
            return root;
        }

        [Shortcut("Overlays/Show Tool Settings", typeof(SceneView), KeyCode.S)]
        static void ShowOverlay(ShortcutArguments args)
        {
            var window = args.context as EditorWindow;
            if (window is ISupportsOverlays)
                window.overlayCanvas.ShowPopupAtMouse<EditorToolSettingsOverlay>();
        }
    }
}
