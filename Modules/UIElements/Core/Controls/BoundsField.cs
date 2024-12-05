// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="Bounds"/> editor field. For more information, refer to [[wiki:UIE-uxml-element-BoundsField|UXML element BoundsField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class BoundsField : BaseField<Bounds>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Bounds>.UxmlSerializedData, IUxmlSerializedDataCustomAttributeHandler
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Bounds>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new BoundsField();

            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var cx = UxmlUtility.TryParseFloatAttribute("cx", bag, ref foundAttributeCounter);
                var cy = UxmlUtility.TryParseFloatAttribute("cy", bag, ref foundAttributeCounter);
                var cz = UxmlUtility.TryParseFloatAttribute("cz", bag, ref foundAttributeCounter);
                var ex = UxmlUtility.TryParseFloatAttribute("ex", bag, ref foundAttributeCounter);
                var ey = UxmlUtility.TryParseFloatAttribute("ey", bag, ref foundAttributeCounter);
                var ez = UxmlUtility.TryParseFloatAttribute("ez", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    Value = new Bounds(new Vector3(cx, cy, cz), new Vector3(ex, ey, ez));
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("cx");
                        uxmlAsset.RemoveAttribute("cy");
                        uxmlAsset.RemoveAttribute("cz");
                        uxmlAsset.RemoveAttribute("ex");
                        uxmlAsset.RemoveAttribute("ey");
                        uxmlAsset.RemoveAttribute("ez");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(Value));
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="BoundsField"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<BoundsField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BoundsField"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseField<Bounds>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_CenterXValue = new UxmlFloatAttributeDescription { name = "cx" };
            UxmlFloatAttributeDescription m_CenterYValue = new UxmlFloatAttributeDescription { name = "cy" };
            UxmlFloatAttributeDescription m_CenterZValue = new UxmlFloatAttributeDescription { name = "cz" };

            UxmlFloatAttributeDescription m_ExtentsXValue = new UxmlFloatAttributeDescription { name = "ex" };
            UxmlFloatAttributeDescription m_ExtentsYValue = new UxmlFloatAttributeDescription { name = "ey" };
            UxmlFloatAttributeDescription m_ExtentsZValue = new UxmlFloatAttributeDescription { name = "ez" };

            /// <summary>
            /// Initialize <see cref="BoundsField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                BoundsField f = (BoundsField)ve;
                f.SetValueWithoutNotify(new Bounds(
                    new Vector3(m_CenterXValue.GetValueFromBag(bag, cc), m_CenterYValue.GetValueFromBag(bag, cc), m_CenterZValue.GetValueFromBag(bag, cc)),
                    new Vector3(m_ExtentsXValue.GetValueFromBag(bag, cc), m_ExtentsYValue.GetValueFromBag(bag, cc), m_ExtentsZValue.GetValueFromBag(bag, cc))));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-bounds-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of center fields in elements of this type.
        /// </summary>
        public static readonly string centerFieldUssClassName = ussClassName + "__center-field";
        /// <summary>
        /// USS class name of extents fields in elements of this type.
        /// </summary>
        public static readonly string extentsFieldUssClassName = ussClassName + "__extents-field";

        private Vector3Field m_CenterField;
        private Vector3Field m_ExtentsField;

        /// <summary>
        /// Initializes and returns an instance of BoundsField.
        /// </summary>
        public BoundsField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of BoundsField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public BoundsField(string label)
            : base(label, null)
        {
            delegatesFocus = false;
            visualInput.focusable = false;

            AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_CenterField = new Vector3Field("Center");
            m_CenterField.name = "unity-m_Center-input";
            m_CenterField.delegatesFocus = true;
            m_CenterField.AddToClassList(centerFieldUssClassName);

            m_CenterField.RegisterValueChangedCallback(e =>
            {
                Bounds current = value;
                current.center = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_CenterField);

            m_ExtentsField = new Vector3Field("Extents");
            m_ExtentsField.name = "unity-m_Extent-input";
            m_ExtentsField.delegatesFocus = true;
            m_ExtentsField.AddToClassList(extentsFieldUssClassName);
            m_ExtentsField.RegisterValueChangedCallback(e =>
            {
                Bounds current = value;
                current.extents = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_ExtentsField);
        }

        public override void SetValueWithoutNotify(Bounds newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_CenterField.SetValueWithoutNotify(rawValue.center);
            m_ExtentsField.SetValueWithoutNotify(rawValue.extents);
        }

        protected override void UpdateMixedValueContent()
        {
            m_CenterField.showMixedValue = showMixedValue;
            m_ExtentsField.showMixedValue = showMixedValue;
        }
    }
}
