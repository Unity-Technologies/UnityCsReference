// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleFoldoutField<THeaderInputElement> : StyleFoldout
        where THeaderInputElement : VisualElement, new()
    {
        static readonly string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/StyleFoldoutField";

        protected static readonly string k_DraggerFieldUssClassName = BuilderConstants.FoldoutFieldPropertyName + "__dragger-field";
        protected static readonly char k_FieldStringSeparator = ' '; // Formatting the header field with multiple values

        THeaderInputElement m_HeaderInputField;

        public THeaderInputElement headerInputField => m_HeaderInputField;

        public StyleFoldoutField() : this(null) { }

        public StyleFoldoutField(string text) : base(text)
        {
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + ".uss"));
            AddToClassList(BuilderConstants.FoldoutFieldPropertyName);
            AddToClassList(StyleRowBinding.overrideBarUssClassName);

            m_HeaderInputField = new THeaderInputElement();
            foldout.header.Add(m_HeaderInputField);

            overrideContainer = m_HeaderInputField;
        }

        /// <summary>
        /// Override this method in inheritors to update the header field with the values from the child fields.
        /// </summary>
        public virtual void UpdateFromChildFields()
        {
        }

        /// <summary>
        /// Override this method to handle changes in the values of the child fields.
        /// </summary>
        protected virtual void Refresh()
        {
            UpdateFromChildFields();
        }
    }
}
