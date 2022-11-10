// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace UnityEditor.UIElements
{
    sealed class EditorPanel : Panel
    {
        readonly EditorCursorManager m_CursorManager = new EditorCursorManager();
        static EditorContextualMenuManager s_ContextualMenuManager = new EditorContextualMenuManager();
        static Shader s_EditorShader;
        static readonly int s_EditorColorSpaceID = Shader.PropertyToID("_EditorColorSpace");

        static Shader EditorShader
        {
            get
            {
                if (s_EditorShader == null)
                {
                    if (UIElementsPackageUtility.IsUIEPackageLoaded)
                        s_EditorShader = Shader.Find("Hidden/UIE-Editor");
                    else
                        s_EditorShader = Shader.Find("Hidden/UIElements/EditorUIE");
                }
                return s_EditorShader;
            }
        }
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
            standardShader = EditorShader;
            updateMaterial += OnUpdateMaterial;
            uiElementsBridge = new EditorUIElementsBridge();
        }

        static void OnUpdateMaterial(Material mat)
        {
            mat?.SetFloat(s_EditorColorSpaceID, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
        }

        public static void InitEditorUpdater(BaseVisualElementPanel panel, VisualTreeUpdater visualTreeUpdater)
        {
            var editorUpdater = new VisualTreeEditorUpdater(panel);
            visualTreeUpdater.visualTreeEditorUpdater = editorUpdater;

            var assetTracker = editorUpdater.GetUpdater(VisualTreeEditorUpdatePhase.AssetChange) as ILiveReloadSystem;
            panel.liveReloadSystem = assetTracker;
        }
    }
}
