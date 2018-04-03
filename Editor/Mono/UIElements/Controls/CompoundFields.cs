// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class RectField : BaseCompoundField<Rect>
    {
        public class RectFieldFactory : UxmlFactory<RectField, RectFieldUxmlTraits> {}

        public class RectFieldUxmlTraits : BaseCompoundFieldUxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue;
            UxmlFloatAttributeDescription m_YValue;
            UxmlFloatAttributeDescription m_WValue;
            UxmlFloatAttributeDescription m_HValue;

            public RectFieldUxmlTraits()
            {
                m_XValue = new UxmlFloatAttributeDescription { name = "x" };
                m_YValue = new UxmlFloatAttributeDescription { name = "y" };
                m_WValue = new UxmlFloatAttributeDescription { name = "w" };
                m_HValue = new UxmlFloatAttributeDescription { name = "h" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_XValue;
                    yield return m_YValue;
                    yield return m_WValue;
                    yield return m_HValue;
                }
            }

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
        public class Vector2FieldFactory : UxmlFactory<Vector2Field, Vector2FieldUxmlTraits> {}

        public class Vector2FieldUxmlTraits : BaseCompoundFieldUxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue;
            UxmlFloatAttributeDescription m_YValue;

            public Vector2FieldUxmlTraits()
            {
                m_XValue = new UxmlFloatAttributeDescription { name = "x" };
                m_YValue = new UxmlFloatAttributeDescription { name = "y" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_XValue;
                    yield return m_YValue;
                }
            }

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
        public class Vector3FieldFactory : UxmlFactory<Vector3Field, Vector3FieldUxmlTraits> {}

        public class Vector3FieldUxmlTraits : BaseCompoundFieldUxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue;
            UxmlFloatAttributeDescription m_YValue;
            UxmlFloatAttributeDescription m_ZValue;

            public Vector3FieldUxmlTraits()
            {
                m_XValue = new UxmlFloatAttributeDescription { name = "x" };
                m_YValue = new UxmlFloatAttributeDescription { name = "y" };
                m_ZValue = new UxmlFloatAttributeDescription { name = "z" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_XValue;
                    yield return m_YValue;
                    yield return m_ZValue;
                }
            }

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
        public class Vector4FieldFactory : UxmlFactory<Vector4Field, Vector4FieldUxmlTraits> {}

        public class Vector4FieldUxmlTraits : BaseCompoundFieldUxmlTraits
        {
            UxmlFloatAttributeDescription m_XValue;
            UxmlFloatAttributeDescription m_YValue;
            UxmlFloatAttributeDescription m_ZValue;
            UxmlFloatAttributeDescription m_WValue;

            public Vector4FieldUxmlTraits()
            {
                m_XValue = new UxmlFloatAttributeDescription { name = "x" };
                m_YValue = new UxmlFloatAttributeDescription { name = "y" };
                m_ZValue = new UxmlFloatAttributeDescription { name = "z" };
                m_WValue = new UxmlFloatAttributeDescription { name = "w" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_XValue;
                    yield return m_YValue;
                    yield return m_ZValue;
                    yield return m_WValue;
                }
            }

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
