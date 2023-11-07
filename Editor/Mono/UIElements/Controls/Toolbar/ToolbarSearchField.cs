// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A search field for the toolbar. For more information, refer to [[wiki:UIE-uxml-element-ToolbarSearchField|UXML element ToolbarSearchField]].
    /// </summary>
    public class ToolbarSearchField : SearchFieldBase<TextField, string>
    {
        // KEEP BELOW CODE TO BE BACKWARD COMPATIBLE WITH 2019.1 ToolbarSearchField
        /// <summary>
        /// USS class name of text elements in elements of this type.
        /// </summary>
        public new static readonly string textUssClassName = SearchFieldBase<TextField, string>.textUssClassName;
        /// <summary>
        /// USS class name of search buttons in elements of this type.
        /// </summary>
        public new static readonly string searchButtonUssClassName = SearchFieldBase<TextField, string>.searchButtonUssClassName;
        /// <summary>
        /// USS class name of cancel buttons in elements of this type.
        /// </summary>
        public new static readonly string cancelButtonUssClassName = SearchFieldBase<TextField, string>.cancelButtonUssClassName;
        /// <summary>
        /// USS class name of cancel buttons in elements of this type, when they are off.
        /// </summary>
        public new static readonly string cancelButtonOffVariantUssClassName = SearchFieldBase<TextField, string>.cancelButtonOffVariantUssClassName;
        /// <summary>
        /// USS class name of elements of this type, when they are using a popup menu.
        /// </summary>
        public new static readonly string popupVariantUssClassName = SearchFieldBase<TextField, string>.popupVariantUssClassName;

        /// <summary>
        /// The search button.
        /// </summary>
        protected new Button searchButton
        {
            get { return base.searchButton; }
        }

        // KEEP ABOVE CODE TO BE BACKWARD COMPATIBLE WITH 2019.1 ToolbarSearchField

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toolbar-search-field";

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : SearchFieldBase<TextField, string>.UxmlSerializedData
        {
            public override object CreateInstance() => new ToolbarSearchField();
        }

        /// <summary>
        /// Instantiates a <see cref="ToolbarSearchField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarSearchField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarSearchField"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a ToolbarSearchField element that you can
        /// use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : SearchFieldBase<TextField, string>.UxmlTraits {}

        /// <summary>
        /// Constructor.
        /// </summary>
        public ToolbarSearchField()
        {
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Removes the text when clearing the field.
        /// </summary>
        protected override void ClearTextField()
        {
            value = String.Empty;
        }

        /// <summary>
        /// Tells if the string is null or empty.
        /// </summary>
        protected override bool FieldIsEmpty(string fieldValue)
        {
            return string.IsNullOrEmpty(fieldValue);
        }
    }
}
