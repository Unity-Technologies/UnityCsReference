// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.ShaderFoundry
{
    [FoundryAPI]
    internal interface ISyntaxNode
    {
        public SyntaxTree syntaxTree { get; }
        internal NodeHandle handle { get; }
        public bool IsValid => handle.IsValid;
    }

    [FoundryAPI]
    internal interface ISyntaxNode<T> : ISyntaxNode
    {
    }
}
