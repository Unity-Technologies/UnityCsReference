// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyName
    {
        internal int id;
        internal int conflictIndex;

        public PropertyName(string name)
            : this(PropertyNameUtils.PropertyNameFromString(name))
        {
        }

        public PropertyName(PropertyName other)
        {
            id = other.id;
            conflictIndex = other.conflictIndex;
        }

        public PropertyName(int id)
        {
            this.id = id;
            this.conflictIndex = 0;
        }

        public static bool IsNullOrEmpty(PropertyName prop) { return prop.id == 0; }

        public static bool operator==(PropertyName lhs, PropertyName rhs)
        {
            return lhs.id == rhs.id;
        }

        public static bool operator!=(PropertyName lhs, PropertyName rhs)
        {
            return lhs.id != rhs.id;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(object other)
        {
            return other is PropertyName && this == (PropertyName)other;
        }

        public static implicit operator PropertyName(string name)
        {
            return new PropertyName(name);
        }

        public static implicit operator PropertyName(int id)
        {
            return new PropertyName(id);
        }

        public override string ToString()
        {
            var conflictCount = PropertyNameUtils.ConflictCountForID(id);
            var msg = string.Format("{0}:{1}", PropertyNameUtils.StringFromPropertyName(this), id);
            if (conflictCount > 0)
            {
                StringBuilder sb = new StringBuilder(msg);
                sb.Append(" conflicts with ");
                for (int i = 0; i < conflictCount; i++)
                {
                    if (i == this.conflictIndex)
                        continue;

                    sb.AppendFormat("\"{0}\"", PropertyNameUtils.StringFromPropertyName(new PropertyName(id) {conflictIndex = i}));
                }
                msg = sb.ToString();
            }
            return msg;
        }

    }
}
