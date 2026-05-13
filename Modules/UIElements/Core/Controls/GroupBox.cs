// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is an enclosing container for a group of <see cref="IGroupBoxOption"/>. All group options within this
    /// container will interact together to allow a single selection, using the <see cref="DefaultGroupManager"/>.
    /// Default options are <see cref="RadioButton"/>, but users can provide other implementations.
    /// If no <see cref="IGroupBox"/> is found in the hierarchy, the default container will be the panel.
    /// </summary>
    [UxmlElement(libraryPath = "Containers")]
    [Icon("UIToolkit/Icons/GroupBox.png")]
    public partial class GroupBox : BindableElement, IGroupBox
    {
        internal static readonly BindingId textProperty = nameof(text);

        /// <summary>
        /// USS class name for GroupBox elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the GroupBox element. Any styling applied to
        /// this class affects every GroupBox located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-group-box";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name for Labels in GroupBox elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> sub-element of the <see cref="GroupBox"/> if the GroupBox has a Label.
        /// </remarks>
        public static readonly string labelUssClassName = ussClassName + "__label";
        internal static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        Label m_TitleLabel;

        // Needed by the UIBuilder for authoring in the viewport
        internal Label titleLabel
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_TitleLabel;
        }

        /// <summary>
        /// The title text of the box.
        /// </summary>
        [MultilineTextField]
        [CreateProperty]
        [UxmlAttribute]
        public string text
        {
            get => m_TitleLabel?.text;
            set
            {
                var previous = text;

                if (!string.IsNullOrEmpty(value))
                {
                    // Lazy allocation of label if needed...
                    if (m_TitleLabel == null)
                    {
                        m_TitleLabel = new Label(value);
                        m_TitleLabel.AddToClassList(labelUssClassNameUnique);
                        Insert(0, m_TitleLabel);
                    }

                    m_TitleLabel.text = value;
                }
                else if (m_TitleLabel != null)
                {
                    m_TitleLabel.RemoveFromHierarchy();
                    m_TitleLabel = null;
                }

                if (string.CompareOrdinal(previous, text) != 0)
                    NotifyPropertyChanged(textProperty);
            }
        }

        /// <summary>
        /// Creates a <see cref="GroupBox"/> with no label.
        /// </summary>
        public GroupBox()
            : this(null) {}

        /// <summary>
        /// Creates a <see cref="GroupBox"/> with a title.
        /// </summary>
        /// <param name="text">The title text.</param>
        public GroupBox(string text)
        {
            AddToClassList(ussClassNameUnique);

            this.text = text;
        }

        void IGroupBox.OnOptionAdded(IGroupBoxOption option) { /* Nothing to do here. */ }
        void IGroupBox.OnOptionRemoved(IGroupBoxOption option) { /* Nothing to do here. */ }
    }
}
