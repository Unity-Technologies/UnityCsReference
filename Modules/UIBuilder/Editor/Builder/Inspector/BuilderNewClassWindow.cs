// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UIEHelpBox = UnityEngine.UIElements.HelpBox;

namespace Unity.UI.Builder
{
    /// <summary>
    ///  Window used to create a new class.
    /// </summary>
    class BuilderNewClassWindow : EditorWindow, IBuilderWindowResizeTracker
    {
        static readonly string ussClassName = "unity-builder-new-class-view";
        static readonly string containerUssName = "unity-builder-new-class-input";
        static readonly string newClassFieldUssName = "new-class-field";
        static readonly string cancelButtonUssName = "cancel-button";
        static readonly string okButtonUssName = "ok-button";

        const string k_WindowTitle = "Add Class";

        static BuilderNewClassWindow s_Window;

        const float k_DefaultWidth = 285;
        const float k_DefaultHeight = 190;

        public static BuilderNewClassWindow activeWindow => s_Window;

        internal Button m_OkButton;
        internal Button m_CancelButton;
        VisualElement m_View;
        TextField m_NewClassField;
        UIEHelpBox m_WarningBox;

        public Action<StyleComplexSelector> OnClassCreated { get; set; }

        public VisualElement view => m_View;
        internal TextField newClassField => m_NewClassField;
        internal Button okButton => m_OkButton;
        internal UIEHelpBox warningBox => m_WarningBox;

        /// <summary>
        /// Opens a window with the specified mode, title and geometry.
        /// </summary>
        /// <param name="pos">The position of the window</param>
        /// <returns></returns>
        public static BuilderNewClassWindow Open(Rect pos)
        {
            var windowSize = new Vector2(k_DefaultWidth, k_DefaultHeight);

            s_Window = GetWindow<BuilderNewClassWindow>(true, k_WindowTitle);
            s_Window.position = new Rect(pos.position, windowSize);
            s_Window.minSize = windowSize;
            return s_Window;
        }

        void OnEnable()
        {
            // Ensure this window closes when the UIBuilder window closes.
            if (Builder.ActiveWindow)
            {
                Builder.ActiveWindow.closing += Close;
            }

            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        private void OnDisable()
        {
            if (Builder.ActiveWindow)
            {
                Builder.ActiveWindow.closing -= Close;
            }

            m_View = null;
            s_Window = null;

            AssemblyReloadEvents.beforeAssemblyReload -= Close;
        }

        internal override void OnResized()
        {
            base.OnResized();
            m_OnRectChanged?.Invoke(this, null);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            m_View = new VisualElement();

            // Load assets.
            var sheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_NewClassWindow);

            // Load styles.
            root.styleSheets.Add(sheet);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/NewClassWindow.uxml");
            template.CloneTree(m_View);

            m_View.AddToClassList(ussClassName);
            m_NewClassField = m_View.Q<TextField>(newClassFieldUssName);

            m_OkButton = m_View.Q<Button>(okButtonUssName);
            m_OkButton.clicked += OnSubmit;
            m_CancelButton = m_View.Q<Button>(cancelButtonUssName);
            m_CancelButton.clicked += Close;

            root.Add(m_View);
        }

        void OnSubmit()
        {
            CreateNewSelector(m_NewClassField.value);
        }

        internal void CreateNewSelector(string className)
        {
            var builder = Builder.ActiveWindow;

            className = className.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);
            if (!VerifyNewClassNameIsValid(className))
            {
                m_NewClassField.Focus();
                return;
            }

            var styleSheet = builder.document.activeStyleSheet;
            if (styleSheet == null)
            {
                if (BuilderStyleSheetsUtilities.CreateNewUSSAsset(builder))
                {
                    styleSheet = builder.document.firstStyleSheet;

                    // The EditorWindow will no longer have Focus after we show the
                    // Save dialog, so we need to manually refocus it.
                    var p = (EditorPanel)builder.rootVisualElement.panel;
                    if (p.ownerObject is HostView view && view)
                    {
                        view.Focus();
                    }
                }
                else
                {
                    Close();
                    return;
                }
            }

            var selectorContainerElement = builder.viewport.styleSelectorElementContainer;
            var selectorString = BuilderConstants.UssSelectorClassNameSymbol + className;
            var selector = BuilderSharedStyles.CreateNewSelector(selectorContainerElement, styleSheet, selectorString);

            OnClassCreated?.Invoke(selector);

            builder.selection.NotifyOfHierarchyChange();
            builder.selection.NotifyOfStylingChange();

            // Close the window.
            Close();
        }

        bool VerifyNewClassNameIsValid(string className)
        {
            if (string.IsNullOrEmpty(className))
                return false;

            var warningMessage = BuilderStyleSheetsUtilities.GetClassNameValidationError(className);

            if (!string.IsNullOrEmpty(warningMessage))
            {
                if (m_WarningBox != null)
                {
                    m_WarningBox.text = warningMessage;
                    return false;
                }
                m_WarningBox ??= new UIEHelpBox(warningMessage, HelpBoxMessageType.Warning) { name = "warning-box"};

                m_View.Q<VisualElement>(className: containerUssName).Add(m_WarningBox);
                return false;
            }

            return true;
        }

        event EventHandler m_OnRectChanged;

        public event EventHandler onRectChanged
        {
            add => m_OnRectChanged += value;
            remove => m_OnRectChanged -= value;
        }
    }
}
