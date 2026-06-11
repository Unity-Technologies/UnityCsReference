// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Attribute to control the header UI of a Dictionary
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DictionaryHeaderAttribute : Attribute
    {
        public string keyColumnLabel { get; set; } = string.Empty;
        public string valueColumnLabel { get; set; } = string.Empty;
        public float keyColumnFraction { get; set; } = 0.5f;
    }
}
