// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: MecanimAnimation not yet converted
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
            #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
            : this(null) { }
            #pragma warning restore UAL0015

        public UIAnimationClipField(string label)
            : base(label, null)
        {
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            m_ObjectField = new ObjectField().WithClassList(objectFieldUssClassName);
            #pragma warning restore UAL0015
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
