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
    /// A <see cref="BoundsInt"/> field. For more information, refer to [[wiki:UIE-uxml-element-BoundsIntField|UXML element BoundsIntField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields")]
    [Icon("UIToolkit/Icons/BoundsIntField.png")]
    public partial class BoundsIntField : BaseField<BoundsInt>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<BoundsInt>.UxmlSerializedData, IUxmlSerializedDataCustomAttributeHandler
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<BoundsInt>.UxmlSerializedData.Register();
            }

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

        private Vector3IntField m_PositionField;
        private Vector3IntField m_SizeField;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-bounds-int-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// USS class name of position fields in elements of this type.
        /// </summary>
        public static readonly string positionUssClassName = ussClassName + "__position-field";
        internal static readonly UniqueStyleString positionUssClassNameUnique = new(positionUssClassName);

        /// <summary>
        /// USS class name of size fields in elements of this type.
        /// </summary>
        public static readonly string sizeUssClassName = ussClassName + "__size-field";
        internal static readonly UniqueStyleString sizeUssClassNameUnique = new(sizeUssClassName);

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

            AddToClassList(ussClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);

            m_PositionField = new Vector3IntField("Position");
            m_PositionField.name = "unity-m_Position-input";
            m_PositionField.delegatesFocus = true;
            m_PositionField.AddToClassList(positionUssClassNameUnique);
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
            m_SizeField.AddToClassList(sizeUssClassNameUnique);
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
