// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

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
    /// </remarks>
    public class Toggle : BaseBoolField
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseBoolField.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField, MultilineTextField] private string text;
            #pragma warning restore 649

            public override object CreateInstance() => new Toggle();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (Toggle)obj;
                e.text = text;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Toggle"/> using data from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Toggle, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Toggle"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription>
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            /// <summary>
            /// Initializes <see cref="Toggle"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Toggle)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name for Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the Toggle element. Any styling applied to
        /// this class affects every Toggle located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-toggle";
        /// <summary>
        /// USS class name for Labels in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> sub-element of the <see cref="Toggle"/> if the Toggle has a Label.
        /// </remarks>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the input sub-element of the <see cref="Toggle"/>. The input sub-element provides
        /// responses to the manipulator.
        /// </remarks>
        public new static readonly string inputUssClassName = ussClassName + "__input";
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
        /// <summary>
        /// USS class name of Text elements in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to Text sub-elements of the <see cref="Toggle"/>.
        /// </remarks>
        public static readonly string textUssClassName = ussClassName + "__text";
        /// <summary>
        /// USS class name of Toggle elements that have mixed values
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to checkmark of the <see cref="Toggle"/> when it has mixed values.
        /// </remarks>
        public static readonly string mixedValuesUssClassName = ussClassName + "__mixed-values";

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
            AddToClassList(ussClassName);

            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_CheckMark.AddToClassList(checkmarkUssClassName);
        }

        protected override void InitLabel()
        {
            base.InitLabel();
            m_Label.AddToClassList(textUssClassName);
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates &= ~PseudoStates.Checked;
                m_CheckMark.AddToClassList(mixedValuesUssClassName);
            }
            else
            {
                m_CheckMark.RemoveFromClassList(mixedValuesUssClassName);

                if (value)
                {
                    visualInput.pseudoStates |= PseudoStates.Checked;
                    pseudoStates |= PseudoStates.Checked;
                }
                else
                {
                    visualInput.pseudoStates &= ~PseudoStates.Checked;
                    pseudoStates &= ~PseudoStates.Checked;
                }
            }
        }
    }
}
