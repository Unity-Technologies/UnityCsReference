// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using Toolbar = UnityEditor.UIElements.Toolbar;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorPreviewWindow : BuilderPaneWindow, IHasCustomMenu
    {
        private static readonly string s_UssClassName = "unity-builder-selector-preview";
        static readonly string s_IdleLabelClassName = s_UssClassName + "__idle-label";
        static readonly string s_IdleLabelContent = "To preview text properties, choose any selector in the UI Builder.";
        private static readonly string s_WindowToolbarClassName = s_UssClassName + "__window-toolbar";

        Label m_IdleMessage;
        Toolbar m_Toolbar;
        BuilderInspector m_Inspector;
        
        public Label idleMessage => m_IdleMessage;
        public Toolbar toolbar => m_Toolbar;

        public static BuilderInspectorPreviewWindow ShowWindow()
        {
            return GetWindowAndInit<BuilderInspectorPreviewWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent(BuilderConstants.PreviewWindowTitle);
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            m_Inspector = ((Builder)viewportWindow).inspector;
            if (m_Inspector.previewWindow == null) 
                m_Inspector.ReloadPreviewWindow(this);
            var previewElement = m_Inspector.preview;
            
            // idle message when no selector is chosen
            m_IdleMessage = new Label(s_IdleLabelContent);
            m_IdleMessage.AddToClassList(s_IdleLabelClassName);
            m_IdleMessage.style.display = DisplayStyle.None;

            // toolbar with transparency toggle
            m_Toolbar = new Toolbar();
            m_Toolbar.RegisterCallback<MouseUpEvent>(OnMouseUp);
            m_Toolbar.AddToClassList(s_WindowToolbarClassName);
            m_Toolbar.Add(previewElement.backgroundToggle);
            
            root.Add(m_Toolbar);
            root.Add(m_IdleMessage);
            root.Add(previewElement);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == (int) MouseButton.RightMouse)
            {
                Close();
                evt.StopImmediatePropagation();
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        { 
            menu.AddItem(EditorGUIUtility.TrTextContent(BuilderConstants.PreviewDockToInspector), false, Close);
        }
        
        private void OnDestroy()
        {
            m_Inspector.ReattachPreview();
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            m_Inspector.ReattachPreview();
        }
    }
}
