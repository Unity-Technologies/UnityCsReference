// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class PopupWindow : TextElement
    {
        public new class UxmlFactory : UxmlFactory<PopupWindow, UxmlTraits> {}

        public new class UxmlTraits : TextElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield return new UxmlChildElementDescription(typeof(VisualElement));
                }
            }
        }

        private VisualElement m_ContentContainer;

        public new static readonly string ussClassName = "unity-popup-window";
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
