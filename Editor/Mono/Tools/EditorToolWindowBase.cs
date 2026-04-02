// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.EditorTools
{
    abstract class EditorToolWindowBase : EditorWindow, ISupportsOverlays, ISupportsToolsOverlays
    {
        public abstract Camera handlesCamera { get; }
        
        VisualElement m_ToolsIMGUIContainer;
        
        VisualElement toolsIMGUIContainer
        {
            get
            {
                if (m_ToolsIMGUIContainer == null)
                    m_ToolsIMGUIContainer = CreateToolsIMGUIContainer();
        
                return m_ToolsIMGUIContainer;
            }
        }
        
        protected virtual void OnEnable()
        {
            rootVisualElement.Add(toolsIMGUIContainer);
        }
        
        void OnContainerGUI()
        {
            var toolsCamera = handlesCamera;
            if (toolsCamera != null)
            {
                var prevAspect = toolsCamera.aspect;
                var prevHandlesCamera = Handles.currentCamera;
                var prevCameraEnabled = toolsCamera.enabled;
                try
                {
                    var containerSize = toolsIMGUIContainer.worldBound.size;
                    toolsCamera.aspect = containerSize.y > 0f ? containerSize.x / containerSize.y : 1f;
                    toolsCamera.enabled = true;
                    Handles.SetCamera(toolsIMGUIContainer.worldBound, toolsCamera);
                    
                    EditorToolManager.OnToolGUI(this);
                }
                finally
                {
                    toolsCamera.enabled = prevCameraEnabled;
                    toolsCamera.aspect  = prevAspect;
                    Handles.currentCamera = prevHandlesCamera;
                }
            }
        }
        
        VisualElement CreateToolsIMGUIContainer()
        {
            var toolsIMGUIContainer = new IMGUIContainer()
            {
                onGUIHandler = OnContainerGUI,
                name = "EditorToolsWindowIMGUIContainer",
                pickingMode = PickingMode.Position,
                viewDataKey = name,
                renderHints = RenderHints.ClipWithScissors,
                requireMeasureFunction = false
            };
        
            UIElementsEditorUtility.AddDefaultEditorStyleSheets(toolsIMGUIContainer);
            toolsIMGUIContainer.style.overflow = Overflow.Hidden;
            toolsIMGUIContainer.style.flexGrow = 1;
            
            return toolsIMGUIContainer;
        }
    }
}
