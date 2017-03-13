// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Internal
{
    /// <summary>
    /// Adds default value information for optional parameters
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
    public class DefaultValueAttribute : Attribute
    {
        private object DefaultValue;

        public DefaultValueAttribute(string value)
        {
            DefaultValue = value;
        }

        public object Value
        {
            get { return DefaultValue; }
        }

        public override bool Equals(object obj)
        {
            DefaultValueAttribute dva = (obj as DefaultValueAttribute);
            if (dva == null)
                return false;

            if (DefaultValue == null)
                return (dva.Value == null);

            return DefaultValue.Equals(dva.Value);
        }

        public override int GetHashCode()
        {
            if (DefaultValue == null)
                return base.GetHashCode();
            return DefaultValue.GetHashCode();
        }
    }
}
