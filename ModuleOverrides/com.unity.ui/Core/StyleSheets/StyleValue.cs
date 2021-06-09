// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.StyleSheets
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("id = {id}, keyword = {keyword}, number = {number}, boolean = {boolean}, color = {color}, object = {resource}")]
    internal struct StyleValue
    {
        [FieldOffset(0)]
        public StylePropertyId id;

        [FieldOffset(4)]
        public StyleKeyword keyword;

        [FieldOffset(8)]
        public float number;   // float, int, enum
        [FieldOffset(8)]
        public Length length;
        [FieldOffset(8)]
        public Color color;
        [FieldOffset(8)]
        public GCHandle resource;
    }

    internal struct StyleValueManaged
    {
        public StylePropertyId id;
        public StyleKeyword keyword;
        public object value;
    }
}
