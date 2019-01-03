// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
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

        public new class UxmlFactory : UxmlFactory<RectField, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<Rect, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_WValue = new UxmlFloatAttributeDescription { name = "w" };
            UxmlFloatAttributeDescription m_HValue = new UxmlFloatAttributeDescription { name = "h" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var r = (RectField)ve;
                r.SetValueWithoutNotify(new Rect(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_WValue.GetValueFromBag(bag, cc), m_HValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-rect-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public RectField()
            : this(null) {}

        public RectField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            AddToClassList(twoLinesVariantUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

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

        public new class UxmlFactory : UxmlFactory<RectIntField, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<RectInt, IntegerField, int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_XValue = new UxmlIntAttributeDescription { name = "x" };
            UxmlIntAttributeDescription m_YValue = new UxmlIntAttributeDescription { name = "y" };
            UxmlIntAttributeDescription m_WValue = new UxmlIntAttributeDescription { name = "w" };
            UxmlIntAttributeDescription m_HValue = new UxmlIntAttributeDescription { name = "h" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var r = (RectIntField)ve;
                r.SetValueWithoutNotify(new RectInt(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_WValue.GetValueFromBag(bag, cc), m_HValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-rect-int-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public RectIntField()
            : this(null) {}

        public RectIntField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            AddToClassList(twoLinesVariantUssClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

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

        public new class UxmlFactory : UxmlFactory<Vector2Field, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<Vector2, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector2Field)ve;
                f.SetValueWithoutNotify(new Vector2(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-vector2-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public Vector2Field()
            : this(null) {}

        public Vector2Field(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

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

        public new class UxmlFactory : UxmlFactory<Vector3Field, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<Vector3, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_ZValue = new UxmlFloatAttributeDescription { name = "z" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector3Field)ve;
                f.SetValueWithoutNotify(new Vector3(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_ZValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-vector3-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public Vector3Field()
            : this(null) {}

        public Vector3Field(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }


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

        public new class UxmlFactory : UxmlFactory<Vector4Field, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<Vector4, FloatField, float>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_ZValue = new UxmlFloatAttributeDescription { name = "z" };
            UxmlFloatAttributeDescription m_WValue = new UxmlFloatAttributeDescription { name = "w" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector4Field)ve;
                f.SetValueWithoutNotify(new Vector4(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_ZValue.GetValueFromBag(bag, cc), m_WValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-vector4-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public Vector4Field()
            : this(null) {}

        public Vector4Field(string label)
            : base(label, 4)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }


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

        public new class UxmlFactory : UxmlFactory<Vector2IntField, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<Vector2Int, IntegerField, int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_XValue = new UxmlIntAttributeDescription { name = "x" };
            UxmlIntAttributeDescription m_YValue = new UxmlIntAttributeDescription { name = "y" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector2IntField)ve;
                f.SetValueWithoutNotify(new Vector2Int(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-vector2-int-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public Vector2IntField()
            : this(null) {}

        public Vector2IntField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }

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

        public new class UxmlFactory : UxmlFactory<Vector3IntField, UxmlTraits> {}

        public new class UxmlTraits : BaseCompositeField<Vector3Int, IntegerField, int>.UxmlTraits
        {
            UxmlIntAttributeDescription m_XValue = new UxmlIntAttributeDescription { name = "x" };
            UxmlIntAttributeDescription m_YValue = new UxmlIntAttributeDescription { name = "y" };
            UxmlIntAttributeDescription m_ZValue = new UxmlIntAttributeDescription { name = "z" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (Vector3IntField)ve;
                f.SetValueWithoutNotify(new Vector3Int(m_XValue.GetValueFromBag(bag, cc), m_YValue.GetValueFromBag(bag, cc), m_ZValue.GetValueFromBag(bag, cc)));
            }
        }

        public new static readonly string ussClassName = "unity-vector3-int-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public Vector3IntField()
            : this(null) {}

        public Vector3IntField(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
