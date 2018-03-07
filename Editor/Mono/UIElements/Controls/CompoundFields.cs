// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements
{
    public class RectField : BaseCompoundField<Rect>
    {
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
