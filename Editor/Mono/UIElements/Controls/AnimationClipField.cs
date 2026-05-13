// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    [UxmlElement]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class UIAnimationClipField : BaseField<UIAnimationClip>
    {
        public static readonly string objectFieldUssClassName = "unity-multi-type-field__object-field";
        public new static readonly string inputUssClassName = "unity-multi-type-field__visual-input";

        readonly ObjectField m_ObjectField;

        public ObjectField objectField => m_ObjectField;

        public UIAnimationClipField()
            : this(null) { }

        public UIAnimationClipField(string label)
            : base(label, null)
        {
            m_ObjectField = new ObjectField().WithClassList(objectFieldUssClassName);
            m_ObjectField.objectType = typeof(UIAnimationClip);
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectField);
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = evt.newValue as UIAnimationClip;
            evt.StopImmediatePropagation();
        }

        public override void SetValueWithoutNotify(UIAnimationClip newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue);
            base.SetValueWithoutNotify(newValue);
        }
    }
}
