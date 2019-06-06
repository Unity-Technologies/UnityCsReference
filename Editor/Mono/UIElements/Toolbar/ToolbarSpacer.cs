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

        [Obsolete("The `fixedSpacerVariantUssClassName` style has been deprecated as is it now the default style.")]
        public static readonly string fixedSpacerVariantUssClassName = ussClassName + "--fixed";

        public static readonly string flexibleSpacerVariantUssClassName = ussClassName + "--flexible";

        public ToolbarSpacer()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(ussClassName);
        }

        public bool flex
        {
            get { return ClassListContains(flexibleSpacerVariantUssClassName); }
            set
            {
                if (flex != value)
                {
                    EnableInClassList(flexibleSpacerVariantUssClassName, value);
                }
            }
        }
    }
}
