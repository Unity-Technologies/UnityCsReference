// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal class StyleFoldoutField<THeaderInputElement> : OverrideFoldout
        where THeaderInputElement : VisualElement, new()
    {
        static readonly string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/StyleFoldoutField.uss";

        protected static readonly string FoldoutFieldPropertyName = "unity-foldout-field";
        protected static readonly string DraggerFieldUssClassName = FoldoutFieldPropertyName + "__dragger-field";
        protected static readonly char FieldStringSeparator = ' '; // Formatting the header field with multiple values
        protected static readonly string UssVariablePrefix = "--";

        THeaderInputElement m_HeaderInputField;

        public THeaderInputElement headerInputField => m_HeaderInputField;

        public StyleFoldoutField() : this(null) { }

        public StyleFoldoutField(string text) : base(text)
        {
            styleSheets.Add(EditorGUIUtility.Load(k_UssPath) as StyleSheet);
            AddToClassList(FoldoutFieldPropertyName);

            m_HeaderInputField = new THeaderInputElement();
            header.Add(m_HeaderInputField);
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
