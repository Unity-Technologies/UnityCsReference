// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class RectField : BaseCompoundField<Rect>
    {
        public new class UxmlFactory : UxmlFactory<RectField, UxmlTraits> {}

        public new class UxmlTraits : BaseCompoundField<Rect>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_WValue = new UxmlFloatAttributeDescription { name = "w" };
            UxmlFloatAttributeDescription m_HValue = new UxmlFloatAttributeDescription { name = "h" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                RectField r = (RectField)ve;
                r.value = new Rect(m_XValue.GetValueFromBag(bag), m_YValue.GetValueFromBag(bag), m_WValue.GetValueFromBag(bag), m_HValue.GetValueFromBag(bag));
            }
        }

        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Rect r, float v) => r.x = v),
                new FieldDescription("Y", r => r.y, (ref Rect r, float v) => r.y = v),
                new FieldDescription("W", r => r.width, (ref Rect r, float v) => r.width = v),
                new FieldDescription("H", r => r.height, (ref Rect r, float v) => r.height = v),
            };
        }
    }

    public class Vector2Field : BaseCompoundField<Vector2>
    {
        public new class UxmlFactory : UxmlFactory<Vector2Field, UxmlTraits> {}

        public new class UxmlTraits : BaseCompoundField<Vector2>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Vector2Field f = (Vector2Field)ve;
                f.value = new Vector2(m_XValue.GetValueFromBag(bag), m_YValue.GetValueFromBag(bag));
            }
        }

        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Vector2 r, float v) => r.x = v),
                new FieldDescription("Y", r => r.y, (ref Vector2 r, float v) => r.y = v),
            };
        }
    }

    public class Vector3Field : BaseCompoundField<Vector3>
    {
        public new class UxmlFactory : UxmlFactory<Vector3Field, UxmlTraits> {}

        public new class UxmlTraits : BaseCompoundField<Vector3>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_ZValue = new UxmlFloatAttributeDescription { name = "z" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Vector3Field f = (Vector3Field)ve;
                f.value = new Vector3(m_XValue.GetValueFromBag(bag), m_YValue.GetValueFromBag(bag), m_ZValue.GetValueFromBag(bag));
            }
        }

        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Vector3 r, float v) => r.x = v),
                new FieldDescription("Y", r => r.y, (ref Vector3 r, float v) => r.y = v),
                new FieldDescription("Z", r => r.z, (ref Vector3 r, float v) => r.z = v),
            };
        }
    }

    public class Vector4Field : BaseCompoundField<Vector4>
    {
        public new class UxmlFactory : UxmlFactory<Vector4Field, UxmlTraits> {}

        public new class UxmlTraits : BaseCompoundField<Vector4>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue = new UxmlFloatAttributeDescription { name = "x" };
            UxmlFloatAttributeDescription m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            UxmlFloatAttributeDescription m_ZValue = new UxmlFloatAttributeDescription { name = "z" };
            UxmlFloatAttributeDescription m_WValue = new UxmlFloatAttributeDescription { name = "w" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Vector4Field f = (Vector4Field)ve;
                f.value = new Vector4(m_XValue.GetValueFromBag(bag), m_YValue.GetValueFromBag(bag), m_ZValue.GetValueFromBag(bag), m_WValue.GetValueFromBag(bag));
            }
        }

        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Vector4 r, float v) => r.x = v),
                new FieldDescription("Y", r => r.y, (ref Vector4 r, float v) => r.y = v),
                new FieldDescription("Z", r => r.z, (ref Vector4 r, float v) => r.z = v),
                new FieldDescription("W", r => r.w, (ref Vector4 r, float v) => r.w = v),
            };
        }
    }
}
