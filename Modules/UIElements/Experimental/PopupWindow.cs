// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
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

        public PopupWindow()
        {
            m_ContentContainer = new VisualElement() { name = "ContentContainer" };
            shadow.Add(m_ContentContainer);
        }

        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }
    }
}
