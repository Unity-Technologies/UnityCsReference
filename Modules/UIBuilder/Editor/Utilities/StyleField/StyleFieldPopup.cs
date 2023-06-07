// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleFieldPopup : VisualElement
    {
        static readonly string s_UssClassName = "unity-style-field-popup";
        static readonly string s_NativeWindowUssClassName = s_UssClassName + "--native-window";
        const int k_PopupMaxWidth = 350;

        private VisualElement m_AnchoredControl;
        private bool m_UsesNativeWindow;
        private StyleFieldPopupWindow m_Window;
        private Rect m_AnchoredControlScreenPos;

        public event Action onShow;
        public event Action onHide;

        public bool isOpened => usesNativeWindow ? m_Window != null : resolvedStyle.display == DisplayStyle.Flex;

        public VisualElement anchoredControl
        {
            get
            {
                return m_AnchoredControl;
            }
            set
            {
                if (m_AnchoredControl == value)
                    return;

                if (m_AnchoredControl != null)
                {
                    m_AnchoredControl.UnregisterCallback<GeometryChangedEvent>(OnAnchoredControlGeometryChanged);
                    m_AnchoredControl.UnregisterCallback<DetachFromPanelEvent>(OnAnchoredControlDetachedFromPath);
                }

                m_AnchoredControl = value;

                if (m_AnchoredControl != null)
                {
                    m_AnchoredControl.RegisterCallback<GeometryChangedEvent>(OnAnchoredControlGeometryChanged);
                    m_AnchoredControl.RegisterCallback<DetachFromPanelEvent>(OnAnchoredControlDetachedFromPath);
                }
            }
        }

        /// <summary>
        /// Indicates whether the popup uses a native window.
        /// </summary>
        public bool usesNativeWindow
        {
            get => m_UsesNativeWindow;
            set
            {
                m_UsesNativeWindow = value;
                EnableInClassList(s_NativeWindowUssClassName, value);
            }
        }

        public StyleFieldPopupWindow nativeWindow => m_Window;

        public StyleFieldPopup()
        {
            AddToClassList(s_UssClassName);
            // Popup is hidden by default
            AddToClassList(BuilderConstants.HiddenStyleClassName);
            this.RegisterCallback<GeometryChangedEvent>(e => EnsureVisibilityInParent());

            // Prevent PointerDownEvent on a child from switching focus.
            this.RegisterCallback<PointerDownEvent>(e => focusController.IgnoreEvent(e), TrickleDown.TrickleDown);
            Hide();
        }

        public virtual void Show()
        {
            if (m_AnchoredControl.HasProperty(BuilderConstants.CompleterAnchoredControlScreenRectVEPropertyName))
            {
                m_AnchoredControlScreenPos = (Rect)m_AnchoredControl.GetProperty(BuilderConstants.CompleterAnchoredControlScreenRectVEPropertyName);
            }
            else
            {
                m_AnchoredControlScreenPos = GUIUtility.GUIToScreenRect(m_AnchoredControl.worldBound);
            }

            AdjustGeometry();
            onShow?.Invoke();
            m_Window?.Close();
            RemoveFromClassList(BuilderConstants.HiddenStyleClassName);

            // Create a new window
            if (usesNativeWindow)
            {
                m_Window = ScriptableObject.CreateInstance<StyleFieldPopupWindow>();

                m_Window.ShowAsDropDown(m_AnchoredControlScreenPos, new Vector2(Mathf.Max(200, m_AnchoredControl.worldBound.width), 30), null, ShowMode.PopupMenu, false);

                // Reset the min and max size of the window
                m_Window.minSize = Vector2.zero;
                m_Window.maxSize = new Vector2(1000, 1000);
                m_Window.content = this;

                // Load assets.
                var mainSS = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder.uss");
                var themeSS = EditorGUIUtility.isProSkin
                    ? BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderDark.uss")
                    : BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderLight.uss");

                // Load styles.
                m_Window.rootVisualElement.styleSheets.Add(mainSS);
                m_Window.rootVisualElement.styleSheets.Add(themeSS);
                m_Window.closed += () => Hide(true);
            }
        }

        public virtual void Hide()
        {
            if (!isOpened)
                return;
            Hide(false);
        }

        void Hide(bool closingWindow)
        {
            if (usesNativeWindow)
            {
                if (closingWindow)
                {
                    RemoveFromHierarchy();
                    onHide?.Invoke();
                    m_Window = null;
                }
                else
                {
                    m_Window.Close();
                }
            }
            else
            {
                AddToClassList(BuilderConstants.HiddenStyleClassName);
                onHide?.Invoke();
            }
        }

        void OnAnchoredControlDetachedFromPath(DetachFromPanelEvent e)
        {
            Hide();
        }

        void OnAnchoredControlGeometryChanged(GeometryChangedEvent e)
        {
            AdjustGeometry();
        }

        public virtual void AdjustGeometry()
        {
            if (m_AnchoredControl != null && m_AnchoredControl.visible && parent != null && !float.IsNaN(layout.width) && !float.IsNaN(layout.height))
            {
                if (usesNativeWindow)
                {
                    if (m_Window != null)
                    {
                        var pos = m_AnchoredControlScreenPos;

                        pos.y += m_AnchoredControl.layout.height;

                        var h = resolvedStyle.height;
                        var size = m_Window.position.size;
                        size.y = h;

                        m_Window.position = new Rect(pos.position, size);
                    }
                }
                else
                {
                    var pos = m_AnchoredControl.ChangeCoordinatesTo(parent, Vector2.zero);

                    style.left = pos.x;
                    style.top = pos.y + m_AnchoredControl.layout.height;
                    style.width = Math.Max(k_PopupMaxWidth, m_AnchoredControl.resolvedStyle.width);
                }
            }
        }

        public virtual Vector2 GetAdjustedPosition()
        {
            if (m_AnchoredControl == null)
            {
                return new Vector2(Mathf.Min(style.left.value.value, parent.layout.width - resolvedStyle.width),
                    Mathf.Min(style.top.value.value, parent.layout.height - resolvedStyle.height));
            }
            else
            {
                var currentPos = new Vector2(style.left.value.value, style.top.value.value);
                var newPos = new Vector2(Mathf.Min(currentPos.x, parent.layout.width - resolvedStyle.width), currentPos.y);
                var fieldTopLeft = m_AnchoredControl.ChangeCoordinatesTo(parent, Vector2.zero);
                var fieldBottom = fieldTopLeft.y + m_AnchoredControl.layout.height;
                const float tolerance = 2f;

                newPos.y = (fieldBottom < parent.layout.height / 2) ? (currentPos.y) : (fieldTopLeft.y - resolvedStyle.height);

                if (Math.Abs(newPos.x - currentPos.x) > tolerance || Math.Abs(newPos.y - currentPos.y) > tolerance)
                    return newPos;
                return currentPos;
            }
        }

        private void EnsureVisibilityInParent()
        {
            if (parent != null && !float.IsNaN(layout.width) && !float.IsNaN(layout.height) && !usesNativeWindow)
            {
                var pos = GetAdjustedPosition();

                style.left = pos.x;
                style.top = pos.y;
            }
        }
    }
}
