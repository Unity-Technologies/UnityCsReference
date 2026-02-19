using System;

namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Constructor)]
    public class RequiredMemberAttribute : Attribute
    {
    }
}
