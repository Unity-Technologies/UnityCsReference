// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Attribute to control the header UI of a Dictionary
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DictionaryHeaderAttribute : Attribute
    {
        public readonly float keyColumnFraction;
        public readonly string keyColumnLabel;
        public readonly string valueColumnLabel;

        public DictionaryHeaderAttribute(string keyColumnLabel = null, string valueColumnLabel = null, float keyColumnFraction = 0.5f)
        {
            this.keyColumnFraction = float.IsNaN(keyColumnFraction) ? 0.5f : Mathf.Clamp(keyColumnFraction, 0.01f, 0.99f);
            this.keyColumnLabel = keyColumnLabel;
            this.valueColumnLabel = valueColumnLabel;
        }
    }
}
