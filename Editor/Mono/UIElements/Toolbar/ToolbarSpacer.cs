// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarSpacer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarSpacer> {}

        public static readonly string ussClassName = "unity-toolbar-spacer";
        public static readonly string fixedSpacerVariantUssClassName = ussClassName + "--fixed";
        public static readonly string flexibleSpacerVariantUssClassName = ussClassName + "--flexible";

        public ToolbarSpacer()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(ussClassName);
            AddToClassList(fixedSpacerVariantUssClassName);
        }

        bool m_Flex;
        public bool flex
        {
            get { return m_Flex; }
            set
            {
                if (m_Flex != value)
                {
                    m_Flex = value;

                    if (m_Flex)
                    {
                        AddToClassList(flexibleSpacerVariantUssClassName);
                        RemoveFromClassList(fixedSpacerVariantUssClassName);
                    }
                    else
                    {
                        RemoveFromClassList(flexibleSpacerVariantUssClassName);
                        AddToClassList(fixedSpacerVariantUssClassName);
                    }
                }
            }
        }
    }
}
