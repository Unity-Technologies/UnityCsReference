using System;

namespace UnityEngine.Scripting
{
    [System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property
        | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Event | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Enum, Inherited = false)]
    public class PreserveAttribute : Attribute
    {
    }
}
