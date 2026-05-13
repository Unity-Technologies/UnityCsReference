// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    [UxmlElement]
    internal partial class ReadOnlyFloatField : VisualElement
    {
        readonly Label m_Label;
        readonly Label m_ValueLabel;

        public ReadOnlyFloatField()
        {
            AddToClassList("read-only-float-field");
            m_Label = new Label()
            {
                name = "read-only-float-field__label"
            };
            Add(m_Label);

            m_ValueLabel = new Label()
            {
                name = "read-only-float-field__value-label"
            };
            Add(m_ValueLabel);

            ApplyStyleSheet();
        }

        public Label Label => m_Label;

        public Label ValueLabel => m_ValueLabel;

        void ApplyStyleSheet()
        {
            var uss = EditorGUIUtility.Load("ReadOnlyFloatField.uss") as StyleSheet;
            styleSheets.Add(uss);
        }
    }
}
