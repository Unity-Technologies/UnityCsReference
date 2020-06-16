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
        [InitializeOnLoad]
        static class EditorShaderLoader
        {
            static EditorShaderLoader()
            {
                // TODO: Remove this once case 1148851 has been fixed.
                UnityEngine.UIElements.UIR.UIRenderDevice.getEditorShader = () => EditorShader;
            }
        }
        // Case 1183719 - The delegate getEditorShader is being reset upon domain reload and InitializeOnLoad is not rerun
        // Hence a static constructor to Initialize the Delegate. EditorShaderLoader is still needed for Batch mode where EditorPanel may not be created
        static EditorPanel()
        {
            // TODO: Remove this once case 1148851 has been fixed.
            UnityEngine.UIElements.UIR.UIRenderDevice.getEditorShader = () => EditorShader;
        }

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
            : base(ownerObject, ContextType.Editor, EventDispatcher.editorDispatcher)
        {
            name = ownerObject.GetType().Name;
            cursorManager = m_CursorManager;
            contextualMenuManager = s_ContextualMenuManager;
            panelDebug = new PanelDebug(this);
            standardShader = EditorShader;
            updateMaterial += OnUpdateMaterial;
        }

        static void OnUpdateMaterial(Material mat)
        {
            mat.SetFloat(s_EditorColorSpaceID, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
        }
    }
}
