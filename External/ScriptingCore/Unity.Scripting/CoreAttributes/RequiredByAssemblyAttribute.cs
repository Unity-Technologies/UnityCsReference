using System;

namespace Unity.Scripting
{
    [System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property
        | AttributeTargets.Constructor | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Event | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Enum, Inherited = false)]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("CodeReloadSafety", "UAL0001:Unsealed Public Class", Justification = "Unsealed on purpose")]
    internal class RequiredByAssemblyAttribute : Attribute
    {
    }
}
