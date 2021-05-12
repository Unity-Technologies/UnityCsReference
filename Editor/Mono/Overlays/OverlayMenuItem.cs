// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class OverlayMenuItem : VisualElement
    {
        const string k_UxmlPath = "UXML/Overlays/overlay-menu-item.uxml";
        const string k_VisibilityIconClass = "overlay-menu-item__visibility-icon";


        readonly Label m_Title;
        readonly VisualElement m_VisibilityIcon;
        Overlay m_Overlay;

        static VisualTreeAsset s_TreeAsset;

        public Overlay overlay
        {
            get => m_Overlay;
            set
            {
                if (m_Overlay != null)
                    m_Overlay.displayedChanged -= OnOverlayDisplayChanged;

                m_Overlay = value;
                name = overlay.rootVisualElement.name;
                m_Title.text = overlay.displayName;
                UpdateIconVisibilityState();

                if (m_Overlay != null)
                    m_Overlay.displayedChanged += OnOverlayDisplayChanged;
            }
        }

        public OverlayMenuItem()
        {
            if (s_TreeAsset == null)
                s_TreeAsset = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;

            s_TreeAsset.CloneTree(this);

            m_Title = this.Q<Label>("DisplayName");
            m_VisibilityIcon = this.Q("VisibilityIcon");

            RegisterCallback<MouseOverEvent>(OnMouseEnter);
            RegisterCallback<MouseOutEvent>(OnMouseLeave);
            this.AddManipulator(new Clickable(OnClick));
        }

        void OnOverlayDisplayChanged(bool state)
        {
            UpdateIconVisibilityState();
        }

        void UpdateIconVisibilityState()
        {
            if (m_Overlay == null)
                return;

            m_VisibilityIcon.EnableInClassList(k_VisibilityIconClass + "--visible", m_Overlay.displayed);
            m_VisibilityIcon.EnableInClassList(k_VisibilityIconClass + "--invisible", !m_Overlay.displayed);
        }

        void OnClick()
        {
            if (m_Overlay == null)
                return;

            m_Overlay.displayed = !m_Overlay.displayed;
        }

        void OnMouseLeave(MouseOutEvent evt)
        {
            m_Overlay?.Highlight(false);
        }

        void OnMouseEnter(MouseOverEvent evt)
        {
            m_Overlay?.Highlight(true);
        }
    }
}
