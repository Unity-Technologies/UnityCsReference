// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.IL2CPP.CompilerServices;

enum Option
{
    NullChecks = 1,
    ArrayBoundsChecks = 2,
    DivideByZeroChecks = 3,
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
class Il2CppSetOptionAttribute : Attribute
{
    public Option Option { get; private set; }
    public object Value { get; private set; }

    public Il2CppSetOptionAttribute(Option option, object value)
    {
        Option = option;
        Value = value;
    }
}
