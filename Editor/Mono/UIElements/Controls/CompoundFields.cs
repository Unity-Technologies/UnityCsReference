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
                new FieldDescription("X", r => r.x, (ref Rect r, double v) => r.x = (float)v),
                new FieldDescription("Y", r => r.y, (ref Rect r, double v) => r.y = (float)v),
                new FieldDescription("W", r => r.width, (ref Rect r, double v) => r.width = (float)v),
                new FieldDescription("H", r => r.height, (ref Rect r, double v) => r.height = (float)v),
            };
        }
    }

    public class Vector2Field : BaseCompoundField<Vector2>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Vector2 r, double v) => r.x = (float)v),
                new FieldDescription("Y", r => r.y, (ref Vector2 r, double v) => r.y = (float)v),
            };
        }
    }

    public class Vector3Field : BaseCompoundField<Vector3>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Vector3 r, double v) => r.x = (float)v),
                new FieldDescription("Y", r => r.y, (ref Vector3 r, double v) => r.y = (float)v),
                new FieldDescription("Z", r => r.z, (ref Vector3 r, double v) => r.z = (float)v),
            };
        }
    }

    public class Vector4Field : BaseCompoundField<Vector4>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", r => r.x, (ref Vector4 r, double v) => r.x = (float)v),
                new FieldDescription("Y", r => r.y, (ref Vector4 r, double v) => r.y = (float)v),
                new FieldDescription("Z", r => r.z, (ref Vector4 r, double v) => r.z = (float)v),
                new FieldDescription("W", r => r.w, (ref Vector4 r, double v) => r.w = (float)v),
            };
        }
    }
}
