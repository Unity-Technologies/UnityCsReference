// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UxmlElement]
    [UsedImplicitly]
    [MovedFrom("Unity.UI.Builder")]
    internal class StyleFilterField : StylePropertyField<StyleList<FilterFunction>, FilterStyleField, List<FilterFunction>>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleList<FilterFunction>, FilterStyleField, List<FilterFunction>>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleList<FilterFunction>, FilterStyleField, List<FilterFunction>>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleFilterField();
        }

        // This is needed in order to not share references with the value being set
        public override StyleList<FilterFunction> value
        {
            get => base.value;
            set
            {
                if (!EqualsCurrentValue(value))
                {
                    if (value.keyword == StyleKeyword.Undefined)
                    {
                        base.value = new StyleList<FilterFunction>(new List<FilterFunction>(value.value));
                    }
                    else
                        base.value = new StyleList<FilterFunction>(value.keyword);
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-filter-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleFilterField() : this(null) {}

        public StyleFilterField(string label) : base(label, new FilterStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            // If label is null, remove the labelElement added with the affordance
            if (Contains(labelElement) && string.IsNullOrEmpty(labelElement.text))
            {
                AddToClassList(noLabelVariantUssClassName);
                labelElement.RemoveFromHierarchy();
            }

            valueField.RegisterCallback<FilterListChangedEvent>((e) =>
            {
                if (value.keyword == StyleKeyword.Undefined && e.newFilterList.Count == 0)
                {
                    value = StyleKeyword.None;
                }
            });

            // Reordering does not send a change event, so we need to handle that manually.
            valueField.RegisterCallback<FilterFunctionReorderedEvent>(e =>
            {
                using (var evt = ChangeEvent<StyleList<FilterFunction>>.GetPooled(null, value))
                {
                    evt.elementTarget = this;
                    SendEvent(evt);
                }
            });
        }

        protected override FilterStyleField CreateValueField()
        {
            return new FilterStyleField();
        }

        protected override StyleList<FilterFunction> CreateStyleValue(List<FilterFunction> v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleList<FilterFunction> v)
        {
            return v == value;
        }
    }
}
