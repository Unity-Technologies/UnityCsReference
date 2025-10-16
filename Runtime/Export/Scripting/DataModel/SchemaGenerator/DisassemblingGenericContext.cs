// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.DataModel;

// Auxiliary class for the visitor to keep track of the generic parameters
internal class DisassemblingGenericContext
{
    internal DisassemblingGenericContext(string[] typeParameters, string[] methodParameters)
    {
        MethodParameters = methodParameters;
        TypeParameters = typeParameters;
    }

    internal string[] MethodParameters { get; }
    internal string[] TypeParameters { get; }
}
