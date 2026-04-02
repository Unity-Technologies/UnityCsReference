// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    ///  Window used to create and edit bindings.
    /// </summary>
    class BindingWindow : EditorWindow
    {
        private const float k_WindowWidth = 560;
        private const float k_WindowHeight = 460;
        private const float k_Spacing = 10;

        public static readonly string k_AddBindingTitle = L10n.Tr("Add Binding");
        public static readonly string k_EditBindingTitle = L10n.Tr("Edit Binding");
        public static readonly string k_ViewBindingTitle = L10n.Tr("View Binding");

        static BindingWindow s_Window;
        private BindingView m_View;

        /// <summary>
        /// The active BindingWindow instance. There should only be one at a time since opening a new one will close the previous one.
        /// </summary>
        public static BindingWindow activeWindow => s_Window;

        /// <summary>
        /// The main view.
        /// </summary>
        public BindingView view => m_View;

        public static void OpenToCreate(VisualElement element, string bindingPath, VisualElement sourceField = null)
        {
            Open(element, bindingPath , BindingView.BindingViewMode.Create, sourceField);
        }

        public static void OpenToEdit(VisualElement element, string bindingPath, VisualElement anchorElement = null)
        {
            Open(element, bindingPath, BindingView.BindingViewMode.Edit, anchorElement);
        }

        public static void OpenToView(VisualElement element, string bindingPath, VisualElement anchorElement = null)
        {
            Open(element, bindingPath, BindingView.BindingViewMode.View, anchorElement);
        }

        public static void Open(VisualElement element, string bindingPath, BindingView.BindingViewMode mode, VisualElement sourceField = null)
        {
            var windowSize = new Vector2(k_WindowWidth, k_WindowHeight);
            var worldBound = Rect.zero;

            if (sourceField != null)
            {
                worldBound = sourceField.worldBound;
                // Adjust the position to align with left edge of the field
                worldBound.x -= k_WindowWidth + k_Spacing;
                worldBound.y -= (k_WindowHeight - worldBound.height) / 2;
            }

            worldBound = GUIUtility.GUIToScreenRect(worldBound);

            // Calls the active Binding window
            if (s_Window != null)
                s_Window.Close();

            Open(element, bindingPath, mode, worldBound.position, windowSize);
        }

        /// <summary>
        /// Opens a window with the specified mode, title and geometry.
        /// </summary>
        /// <param name="openToCreate">Indicates whether the window will be used for creeating or editing binding</param>
        /// <param name="bindingPath">The binding path of the UI property to bind</param>
        /// <param name="pos">The position of the window</param>
        /// <param name="windowSize">The size of the window</param>
        /// <returns></returns>
        static BindingWindow Open(VisualElement element, string bindingPath, BindingView.BindingViewMode mode, Vector2 pos, Vector2 windowSize)
        {
            var title = mode switch
            {
                BindingView.BindingViewMode.Create => k_AddBindingTitle,
                BindingView.BindingViewMode.Edit => k_EditBindingTitle,
                _ => k_ViewBindingTitle
            } ;

            s_Window = GetWindow<BindingWindow>(true, title);
            s_Window.position = new Rect(pos, windowSize);
            if (!s_Window.view.Start(element, bindingPath, mode))
            {
                s_Window.Close();
            }
            return s_Window;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            m_View = new BindingView() { style = { flexGrow = 1 } };
            rootVisualElement.Add(m_View);
            m_View.closeRequested += Close;
        }

        void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        private void OnDisable()
        {
            m_View?.OnClose();
            m_View = null;
            s_Window = null;

            AssemblyReloadEvents.beforeAssemblyReload -= Close;
        }
    }
}
