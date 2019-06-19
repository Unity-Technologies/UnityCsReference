// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarSearchField : SearchFieldBase<TextField, string>
    {
        // KEEP BELOW CODE TO BE BACKWARD COMPATIBLE WITH 2019.1 ToolbarSearchField
        public new static readonly string textUssClassName = SearchFieldBase<TextField, string>.textUssClassName;
        public new static readonly string searchButtonUssClassName = SearchFieldBase<TextField, string>.searchButtonUssClassName;
        public new static readonly string cancelButtonUssClassName = SearchFieldBase<TextField, string>.cancelButtonUssClassName;
        public new static readonly string cancelButtonOffVariantUssClassName = SearchFieldBase<TextField, string>.cancelButtonOffVariantUssClassName;
        public new static readonly string popupVariantUssClassName = SearchFieldBase<TextField, string>.popupVariantUssClassName;

        protected new Button searchButton
        {
            get { return base.searchButton; }
        }

        public new string value
        {
            get { return base.value; }
            set { base.value = value; }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
        }

        // KEEP ABOVE CODE TO BE BACKWARD COMPATIBLE WITH 2019.1 ToolbarSearchField

        public new static readonly string ussClassName = "unity-toolbar-search-field";
        public new class UxmlFactory : UxmlFactory<ToolbarSearchField> {}

        public ToolbarSearchField()
        {
            AddToClassList(ussClassName);
        }

        protected override void ClearTextField()
        {
            value = String.Empty;
        }

        protected override bool FieldIsEmpty(string fieldValue)
        {
            return string.IsNullOrEmpty(fieldValue);
        }
    }
}
