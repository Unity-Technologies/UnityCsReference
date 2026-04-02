// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides an Element displaying text. For more information, refer to [[wiki:UIE-uxml-element-Label|UXML element Label]].
    /// </summary>
    [UxmlElement(libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/Label.png")]
    public partial class Label : TextElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : TextElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Label();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-label";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// Constructs a Label with no initial text.
        /// </summary>
        /// <remarks>
        /// Use this constructor with no arguments to create an empty Label.
        /// </remarks>
        public Label() : this(String.Empty) {}
        /// <summary>
        /// Constructs a Label displaying the specified text.
        /// </summary>
        /// <param name="text">The initial text to be displayed in the Label.</param>
        /// <remarks>
        /// Use the @@text@@ parameter to create a Label with the specified value as the initial text.
        /// </remarks>
        public Label(string text)
        {
            AddToClassList(ussClassNameUnique);

            this.text = text;
        }
    }
}
