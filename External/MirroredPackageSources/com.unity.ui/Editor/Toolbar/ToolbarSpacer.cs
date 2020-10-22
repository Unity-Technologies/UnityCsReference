using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A toolbar spacer of static size.
    /// </summary>
    public class ToolbarSpacer : VisualElement
    {
        /// <summary>
        /// Instantiates a <see cref="ToolbarSpacer"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarSpacer> {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-toolbar-spacer";

        /// <summary>
        /// USS class name of elements of this type, when they are of fixed size.
        /// </summary>
        [Obsolete("The `fixedSpacerVariantUssClassName` style has been deprecated as is it now the default style.")]
        public static readonly string fixedSpacerVariantUssClassName = ussClassName + "--fixed";

        /// <summary>
        /// USS class name of elements of this type, when they are of flexible size.
        /// </summary>
        public static readonly string flexibleSpacerVariantUssClassName = ussClassName + "--flexible";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ToolbarSpacer()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// True if the spacer will stretch or shring to occupy available space.
        /// </summary>
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
