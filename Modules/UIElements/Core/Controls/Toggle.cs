// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A Toggle is a clickable element that represents a boolean value.
    /// </summary>
    /// <remarks>
    /// A Toggle control consists of a label and an input field. The input field contains a sprite for the control. By default,
    /// this is a checkbox (Unity does not provide a separate checkbox control type) in all of its possible states, for example,
    /// normal, hovered, checked, and unchecked. You can style a Toggle control to change its appearance to something else, for
    /// example, an on/off switch.
    ///
    /// When a Toggle is clicked, its state alternates between between true and false. You can also think of these states  as
    /// on and off, or enabled and disabled.
    ///
    /// To bind the Toggle's state to a boolean variable, set the`binding-path` property in a UI Document (.uxml file), or
    /// the C# `bindingPath` to the variable name.
    ///
    /// For more information, refer to [[wiki:UIE-uxml-element-Toggle|UXML element Toggle]].
    /// </remarks>
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/Toggle.png")]
    public partial class Toggle : BaseBoolField
    {
        /// <summary>
        /// USS class name for Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the Toggle element. Any styling applied to
        /// this class affects every Toggle located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-toggle";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name for Labels in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> sub-element of the <see cref="Toggle"/> if the Toggle has a Label.
        /// </remarks>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the input sub-element of the <see cref="Toggle"/>. The input sub-element provides
        /// responses to the manipulator.
        /// </remarks>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// USS class name of Toggle elements that have no text.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Toggle"/> if the Toggle does not have a label.
        /// </remarks>
        [Obsolete]
        public static readonly string noTextVariantUssClassName = ussClassName + "--no-text";
        /// <summary>
        /// USS class name of Images in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the Image sub-element of the <see cref="Toggle"/> that contains the checkmark image.
        /// </remarks>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        internal static readonly UniqueStyleString checkmarkUssClassNameUnique = new(checkmarkUssClassName);

        /// <summary>
        /// USS class name of Text elements in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to Text sub-elements of the <see cref="Toggle"/>.
        /// </remarks>
        public static readonly string textUssClassName = ussClassName + "__text";
        internal static readonly UniqueStyleString textUssClassNameUnique = new(textUssClassName);

        /// <summary>
        /// USS class name of Toggle elements that have mixed values
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to checkmark of the <see cref="Toggle"/> when it has mixed values.
        /// </remarks>
        public static readonly string mixedValuesUssClassName = ussClassName + "__mixed-values";
        internal static readonly UniqueStyleString mixedValuesUssClassNameUnique = new(mixedValuesUssClassName);

        /// <summary>
        /// Creates a <see cref="Toggle"/> with no label.
        /// </summary>
        public Toggle()
            : this(null) {}

        /// <summary>
        /// Creates a <see cref="Toggle"/> with a Label and a default manipulator.
        /// </summary>
        /// <remarks>
        /// The default manipulator makes it possible to activate the Toggle with a left mouse click.
        /// </remarks>
        /// <param name="label">The Label text.</param>
        public Toggle(string label)
            : base(label)
        {
            AddToClassList(ussClassNameUnique);

            visualInput.AddToClassList(inputUssClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);

            m_CheckMark.AddToClassList(checkmarkUssClassNameUnique);
        }

        protected override void InitLabel()
        {
            base.InitLabel();
            m_Label.AddToClassList(textUssClassNameUnique);
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.SetCheckedPseudoState(false);
                SetCheckedPseudoState(false);
                m_CheckMark.AddToClassList(mixedValuesUssClassNameUnique);
            }
            else
            {
                m_CheckMark.RemoveFromClassList(mixedValuesUssClassNameUnique);

                visualInput.SetCheckedPseudoState(value);
                SetCheckedPseudoState(value);
            }
        }
    }
}
