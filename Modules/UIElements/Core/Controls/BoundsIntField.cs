// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="BoundsInt"/> field. For more information, refer to [[wiki:UIE-uxml-element-BoundsIntField|UXML element BoundsIntField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class BoundsIntField : BaseField<BoundsInt>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<BoundsInt>.UxmlSerializedData, IUxmlSerializedDataCustomAttributeHandler
        {
            public override object CreateInstance() => new BoundsIntField();

            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var px = UxmlUtility.TryParseIntAttribute("px", bag, ref foundAttributeCounter);
                var py = UxmlUtility.TryParseIntAttribute("py", bag, ref foundAttributeCounter);
                var pz = UxmlUtility.TryParseIntAttribute("pz", bag, ref foundAttributeCounter);
                var sx = UxmlUtility.TryParseIntAttribute("sx", bag, ref foundAttributeCounter);
                var sy = UxmlUtility.TryParseIntAttribute("sy", bag, ref foundAttributeCounter);
                var sz = UxmlUtility.TryParseIntAttribute("sz", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    Value = new BoundsInt(new Vector3Int(px, py, pz), new Vector3Int(sx, sy, sz));
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("px");
                        uxmlAsset.RemoveAttribute("py");
                        uxmlAsset.RemoveAttribute("pz");
                        uxmlAsset.RemoveAttribute("sx");
                        uxmlAsset.RemoveAttribute("sy");
                        uxmlAsset.RemoveAttribute("sz");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(Value));
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="BoundsIntField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<BoundsIntField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BoundsIntField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<BoundsInt>.UxmlTraits
        {
            UxmlIntAttributeDescription m_PositionXValue = new UxmlIntAttributeDescription { name = "px" };
            UxmlIntAttributeDescription m_PositionYValue = new UxmlIntAttributeDescription { name = "py" };
            UxmlIntAttributeDescription m_PositionZValue = new UxmlIntAttributeDescription { name = "pz" };

            UxmlIntAttributeDescription m_SizeXValue = new UxmlIntAttributeDescription { name = "sx" };
            UxmlIntAttributeDescription m_SizeYValue = new UxmlIntAttributeDescription { name = "sy" };
            UxmlIntAttributeDescription m_SizeZValue = new UxmlIntAttributeDescription { name = "sz" };

            /// <summary>
            /// Initializes the <see cref="UxmlTraits"/> for the <see cref="BoundsIntField"/>.
            /// </summary>
            /// <param name="ve">The <see cref="VisualElement"/> to be initialized.</param>
            /// <param name="bag">Bag of attributes.</param>
            /// <param name="cc">CreationContext, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (BoundsIntField)ve;
                f.SetValueWithoutNotify(new BoundsInt(
                    new Vector3Int(m_PositionXValue.GetValueFromBag(bag, cc), m_PositionYValue.GetValueFromBag(bag, cc), m_PositionZValue.GetValueFromBag(bag, cc)),
                    new Vector3Int(m_SizeXValue.GetValueFromBag(bag, cc), m_SizeYValue.GetValueFromBag(bag, cc), m_SizeZValue.GetValueFromBag(bag, cc))));
            }
        }
        private Vector3IntField m_PositionField;
        private Vector3IntField m_SizeField;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-bounds-int-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of position fields in elements of this type.
        /// </summary>
        public static readonly string positionUssClassName = ussClassName + "__position-field";
        /// <summary>
        /// USS class name of size fields in elements of this type.
        /// </summary>
        public static readonly string sizeUssClassName = ussClassName + "__size-field";

        /// <summary>
        /// Initializes and returns an instance of BoundsIntField.
        /// </summary>
        public BoundsIntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of BoundsIntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public BoundsIntField(string label)
            : base(label, null)
        {
            delegatesFocus = false;
            visualInput.focusable = false;

            AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_PositionField = new Vector3IntField("Position");
            m_PositionField.name = "unity-m_Position-input";
            m_PositionField.delegatesFocus = true;
            m_PositionField.AddToClassList(positionUssClassName);
            m_PositionField.RegisterValueChangedCallback(e =>
            {
                var current = value;
                current.position = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_PositionField);


            m_SizeField = new Vector3IntField("Size");
            m_SizeField.name = "unity-m_Size-input";
            m_SizeField.delegatesFocus = true;
            m_SizeField.AddToClassList(sizeUssClassName);
            m_SizeField.RegisterValueChangedCallback(e =>
            {
                var current = value;
                current.size = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_SizeField);
        }

        public override void SetValueWithoutNotify(BoundsInt newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_PositionField.SetValueWithoutNotify(rawValue.position);
            m_SizeField.SetValueWithoutNotify(rawValue.size);
        }

        protected override void UpdateMixedValueContent()
        {
            m_PositionField.showMixedValue = showMixedValue;
            m_SizeField.showMixedValue = showMixedValue;
        }
    }
}
