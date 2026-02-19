using System;

namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true)]
    public class RequiredInterfaceAttribute : Attribute
    {
        public RequiredInterfaceAttribute(Type interfaceType)
        {
        }
    }
}
