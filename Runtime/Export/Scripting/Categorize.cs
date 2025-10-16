// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Categorization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public abstract class InfoAttribute : Attribute
    {
        public int Order { get; set; } = int.MaxValue;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class ElementInfoAttribute : InfoAttribute
    {
    }
    
    public class CategoryInfoAttribute : InfoAttribute
    {
    }
}
