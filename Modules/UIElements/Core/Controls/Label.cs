// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides an Element displaying text. For more information, refer to [[wiki:UIE-uxml-element-Label|UXML element Label]].
    /// </summary>
    public class Label : TextElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Label();
        }

        /// <summary>
        /// Instantiates a <see cref="Label"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<Label, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Label"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
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
