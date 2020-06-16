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
