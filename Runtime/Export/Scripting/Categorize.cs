// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Categorization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ElementInfoAttribute : Attribute
    {
        public int Order { get; set; } = int.MaxValue;
        public string Name { get; set; } = null;
    }
        
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CategoryInfoAttribute : Attribute
    { 
        public int Order { get; set; } = int.MaxValue;
        public string Name { get; set; } = null;
    }
}
