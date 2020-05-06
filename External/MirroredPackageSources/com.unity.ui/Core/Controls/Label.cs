using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides an Element displaying text.
    /// </summary>
    public class Label : TextElement
    {
        /// <summary>
        /// Instantiates a <see cref="Label"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Label, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Label"/>.
        /// </summary>
        public new class UxmlTraits : TextElement.UxmlTraits {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-label";


        /// <summary>
        /// Constructs a label.
        /// </summary>
        public Label() : this(String.Empty) {}
        /// <summary>
        /// Constructs a label.
        /// </summary>
        /// <param name="text">The text to be displayed.</param>
        public Label(string text)
        {
            AddToClassList(ussClassName);

            this.text = text;
        }
    }
}
