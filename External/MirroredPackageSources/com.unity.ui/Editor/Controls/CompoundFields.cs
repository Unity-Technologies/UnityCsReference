using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A <see cref="Rect"/> editor field.
    /// </summary>
    public class RectField : BaseCompositeField<Rect, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Rect r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Rect r, float v) => r.y = v),
                new FieldDescription("W", "unity-width-input", r => r.width, (ref Rect r, float v) => r.width = v),
                new FieldDescription("H", "unity-height-input", r => r.height, (ref Rect r, float v) => r.height = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="RectField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RectField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RectField"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<Rect, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_WValue = new UxmlFloatAttributeDescription { name = "w" };
            UxmlFloatAttributeDescription m_HValue = new UxmlFloatAttributeDescription { name = "h" };

            /// <summary>
            /// Initialize <see cref="RectField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var r = (RectField)ve;
                r.SetValueWithoutNotify(new Rect(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_WValue.GetValueFromBag(bag, cc), m_HValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rect-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of RectField.
        /// </summary>
        public RectField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of RectField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public RectField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            AddToClassList(twoLinesVariantUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

    /// <summary>
    /// A <see cref="RectInt"/> editor field.
    /// </summary>
    public class RectIntField : BaseCompositeField<RectInt, IntegerField, int>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref RectInt r, int v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref RectInt r, int v) => r.y = v),
                new FieldDescription("W", "unity-width-input", r => r.width, (ref RectInt r, int v) => r.width = v),
                new FieldDescription("H", "unity-height-input", r => r.height, (ref RectInt r, int v) => r.height = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="RectIntField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RectIntField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RectIntField"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<RectInt, IntegerField, int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_XValue = new UxmlIntAttributeDescription { name = "x" };
            UxmlIntAttributeDescription m_YValue = new UxmlIntAttributeDescription { name = "y" };
            UxmlIntAttributeDescription m_WValue = new UxmlIntAttributeDescription { name = "w" };
            UxmlIntAttributeDescription m_HValue = new UxmlIntAttributeDescription { name = "h" };

            /// <summary>
            /// Initializes the <see cref="UxmlTraits"/> for the <see cref="RectIntField"/>.
            /// </summary>
            /// <param name="ve">The <see cref="VisualElement"/> to be initialized.</param>
            /// <param name="bag">Bags of attributes where the values come from.</param>
            /// <param name="cc">Creation Context, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var r = (RectIntField)ve;
                r.SetValueWithoutNotify(new RectInt(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_WValue.GetValueFromBag(bag, cc), m_HValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rect-int-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of RectIntField.
        /// </summary>
        public RectIntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of RectIntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public RectIntField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            AddToClassList(twoLinesVariantUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

    /// <summary>
    /// A <see cref="Vector2"/> editor field.
    /// </summary>
    public class Vector2Field : BaseCompositeField<Vector2, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector2 r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector2 r, float v) => r.y = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="Vector2Field"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Vector2Field, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector2Field"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<Vector2, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };

            /// <summary>
            /// Initialize <see cref="Vector2Field"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector2Field)ve;
                f.SetValueWithoutNotify(new Vector2(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector2-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of Vector2Field.
        /// </summary>
        public Vector2Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector2Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector2Field(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

    /// <summary>
    /// A <see cref="Vector3"/> editor field.
    /// </summary>
    public class Vector3Field : BaseCompositeField<Vector3, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector3 r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector3 r, float v) => r.y = v),
                new FieldDescription("Z", "unity-z-input", r => r.z, (ref Vector3 r, float v) => r.z = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="Vector3Field"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Vector3Field, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vetor3Field"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<Vector3, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_ZValue = new UxmlFloatAttributeDescription { name = "z" };

            /// <summary>
            /// Initialize <see cref="Vector3Field"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector3Field)ve;
                f.SetValueWithoutNotify(new Vector3(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_ZValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector3-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of Vector3Field.
        /// </summary>
        public Vector3Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector3Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector3Field(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }


    /// <summary>
    /// A <see cref="Vector4"/> editor field.
    /// </summary>
    public class Vector4Field : BaseCompositeField<Vector4, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector4 r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector4 r, float v) => r.y = v),
                new FieldDescription("Z", "unity-z-input", r => r.z, (ref Vector4 r, float v) => r.z = v),
                new FieldDescription("W", "unity-w-input", r => r.w, (ref Vector4 r, float v) => r.w = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="Vector4Field"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Vector4Field, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector4Field"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<Vector4, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_ZValue = new UxmlFloatAttributeDescription { name = "z" };
            UxmlFloatAttributeDescription m_WValue = new UxmlFloatAttributeDescription { name = "w" };

            /// <summary>
            /// Initialize <see cref="Vector4Field"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector4Field)ve;
                f.SetValueWithoutNotify(new Vector4(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_ZValue.GetValueFromBag(bag, cc), m_WValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector4-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of Vector4Field.
        /// </summary>
        public Vector4Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector4Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector4Field(string label)
            : base(label, 4)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }


    /// <summary>
    /// A <see cref="Vector2Int"/> editor field.
    /// </summary>
    public class Vector2IntField : BaseCompositeField<Vector2Int, IntegerField, int>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector2Int r, int v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector2Int r, int v) => r.y = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="Vector2IntField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Vector2IntField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector2IntField"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<Vector2Int, IntegerField, int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_XValue = new UxmlIntAttributeDescription { name = "x" };
            UxmlIntAttributeDescription m_YValue = new UxmlIntAttributeDescription { name = "y" };

            /// <summary>
            /// Initializes the <see cref="UxmlTraits"/> for the <see cref="Vector2IntField"/>.
            /// </summary>
            /// <param name="ve"><see cref="VisualElement"/> to initialize.</param>
            /// <param name="bag">Bag of attributes where to get them.</param>
            /// <param name="cc">Creation Context, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector2IntField)ve;
                f.SetValueWithoutNotify(new Vector2Int(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector2-int-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of Vector2IntField.
        /// </summary>
        public Vector2IntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector2IntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector2IntField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

    /// <summary>
    /// A <see cref="Vector3Int"/> editor field.
    /// </summary>
    public class Vector3IntField : BaseCompositeField<Vector3Int, IntegerField, int>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector3Int r, int v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector3Int r, int v) => r.y = v),
                new FieldDescription("Z", "unity-z-input", r => r.z, (ref Vector3Int r, int v) => r.z = v),
            };
        }

        /// <summary>
        /// Instantiates a <see cref="Vector3IntField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Vector3IntField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Vector3IntField"/>.
        /// </summary>
        public new class UxmlTraits : BaseCompositeField<Vector3Int, IntegerField, int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_XValue = new UxmlIntAttributeDescription { name = "x" };
            UxmlIntAttributeDescription m_YValue = new UxmlIntAttributeDescription { name = "y" };
            UxmlIntAttributeDescription m_ZValue = new UxmlIntAttributeDescription { name = "z" };

            /// <summary>
            /// Initializes the <see cref="UxmlTraits"/> for the <see cref="Vector3IntField"/>.
            /// </summary>
            /// <param name="ve">VisualElement to initialize.</param>
            /// <param name="bag">Bag of attributes where to get them.</param>
            /// <param name="cc">Context Creation, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector3IntField)ve;
                f.SetValueWithoutNotify(new Vector3Int(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_ZValue.GetValueFromBag(bag, cc)));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector3-int-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of Vector3IntField.
        /// </summary>
        public Vector3IntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector3IntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector3IntField(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
