// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // Helper class for constructing displayable names for objects.
    [VisibleToOtherModules]
    [NativeHeader("Runtime/NameFormatter/NameFormatter.bindings.h")]
    internal sealed class NameFormatter
    {
        // Make a displayable name for a variable.
        [FreeFunction(Name = "FormatVariableName_Binding")]
        public static extern string FormatVariableName(string name);
    }
}
