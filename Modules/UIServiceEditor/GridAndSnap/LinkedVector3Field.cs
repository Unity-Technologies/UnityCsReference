// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Snap
{
    class LinkedVector3Field : Vector3Field
    {
        public readonly string linkToggleClassName = ussClassName + "__link-toggle";

        bool m_Linked;
        readonly VisualElement m_LinkedToggle;
        readonly FloatField m_YField;
        readonly FloatField m_ZField;

        public Action<bool> linkedChanged;

        public bool linked
        {
            get => m_Linked;
            set
            {
                if (m_Linked == value)
                    return;

                m_Linked = value;

                if (m_Linked)
                    this.value = this.value.x * Vector3.one;

                UpdateLinkedState();
                linkedChanged?.Invoke(m_Linked);
            }
        }

        public LinkedVector3Field() : this(null)
        {
        }

        public LinkedVector3Field(string label) : base(label)
        {
            styleSheets.Add((StyleSheet)EditorGUIUtility.Load("StyleSheets/SceneViewToolbarElements/LinkedVector3FieldCommon.uss"));
            styleSheets.Add((StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? "StyleSheets/SceneViewToolbarElements/LinkedVector3FieldDark.uss"
                : "StyleSheets/SceneViewToolbarElements/LinkedVector3FieldLight.uss"));

            var xField = this.Q<FloatField>("unity-x-input");
            m_YField = this.Q<FloatField>("unity-y-input");
            m_ZField = this.Q<FloatField>("unity-z-input");

            m_LinkedToggle = new VisualElement();
            m_LinkedToggle.AddToClassList(linkToggleClassName);
            Insert(IndexOf(this.Q(classes: inputUssClassName)), m_LinkedToggle);
            m_LinkedToggle.AddManipulator(new Clickable(() => linked = !linked));
            UpdateLinkedState();
        }

        void UpdateLinkedState()
        {
            if (linked)
                m_LinkedToggle.pseudoStates |= PseudoStates.Checked;
            else
                m_LinkedToggle.pseudoStates &= ~PseudoStates.Checked;

            m_YField.SetEnabled(!linked);
            m_ZField.SetEnabled(!linked);
        }

        void UpdateLinkedDisplay()
        {
            if (!linked)
                return;

            m_YField.SetValueWithoutNotify(value.x);
            m_ZField.SetValueWithoutNotify(value.x);
        }

        public override void SetValueWithoutNotify(Vector3 newValue)
        {
            base.SetValueWithoutNotify(m_Linked ? newValue.x * Vector3.one : newValue);

            UpdateLinkedDisplay();
        }
    }
}
