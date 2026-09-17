// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.ObjectModel;

namespace UnityEditor.ShaderApiReflection
{
    [ShouldBePublic]
    internal interface IHasFunctionSignature
    {
        public ReadOnlyCollection<string> EnclosingNamespace { get; }
        public string ReturnTypeName { get; }
        public string Name { get; }
        public ReadOnlyCollection<ReflectedParameter> Parameters { get; }

        // Returns a string representation of the function signature.
        // E.g.: void MyNamespace::MyFunction(in float param0)
        public string GetSignature();
    }
}
