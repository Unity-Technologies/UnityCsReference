using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A search field for the toolbar.
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

        /// <summary>
        /// The object currently being exposed by the field.
        /// </summary>
        /// <remarks>
        /// If the new value is different from the current value, this method notifies registered callbacks with a ChangeEvent<string>.
        /// </remarks>
        public new string value
        {
            get { return base.value; }
            set { base.value = value; }
        }

        /// <summary>
        /// Sets the value for the toolbar search field without sending a change event.
        /// </summary>
        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
        }

        // KEEP ABOVE CODE TO BE BACKWARD COMPATIBLE WITH 2019.1 ToolbarSearchField

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toolbar-search-field";
        /// <summary>
        /// Instantiates a <see cref="ToolbarSearchField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarSearchField> {}

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
