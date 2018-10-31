// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderTagId : IEquatable<ShaderTagId>
    {
        public static readonly ShaderTagId none = default(ShaderTagId);

        int m_Id;

        public ShaderTagId(string name)
        {
            m_Id = Shader.TagToID(name);
        }

        internal int id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        public string name
        {
            get { return Shader.IDToTag(id); }
        }

        public override bool Equals(object obj)
        {
            return obj is ShaderTagId && Equals((ShaderTagId)obj);
        }

        public bool Equals(ShaderTagId other)
        {
            return m_Id == other.m_Id;
        }

        public override int GetHashCode()
        {
            var hashCode = 2079669542;
            hashCode = hashCode * -1521134295 + m_Id.GetHashCode();
            return hashCode;
        }

        public static bool operator==(ShaderTagId tag1, ShaderTagId tag2)
        {
            return tag1.Equals(tag2);
        }

        public static bool operator!=(ShaderTagId tag1, ShaderTagId tag2)
        {
            return !(tag1 == tag2);
        }

        public static explicit operator ShaderTagId(string name)
        {
            return new ShaderTagId(name);
        }

        public static explicit operator string(ShaderTagId tagId)
        {
            return tagId.name;
        }
    }
}
