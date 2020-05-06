using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Styled visual element that matches the EditorGUILayout.Popup IMGUI element.
    /// </summary>
    public class PopupWindow : TextElement
    {
        /// <summary>
        /// Instantiates a <see cref="PopupWindow"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<PopupWindow, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PopupWindow"/>.
        /// </summary>
        public new class UxmlTraits : TextElement.UxmlTraits
        {
            /// <summary>
            /// Returns an empty enumerable, as popup windows generally do not have children.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield return new UxmlChildElementDescription(typeof(VisualElement));
                }
            }
        }

        private VisualElement m_ContentContainer;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-popup-window";
        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string contentUssClassName = ussClassName + "__content-container";

        public PopupWindow()
        {
            AddToClassList(ussClassName);

            m_ContentContainer = new VisualElement() { name = "unity-content-container"};
            m_ContentContainer.AddToClassList(contentUssClassName);
            hierarchy.Add(m_ContentContainer);
        }

        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }
    }
}
