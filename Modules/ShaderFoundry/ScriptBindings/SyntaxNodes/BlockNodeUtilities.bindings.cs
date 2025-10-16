// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Parser/BlockNodeUtilities.h")]
    internal static class BlockNodeUtilities
    {
        [FreeFunction("ShaderFoundry::BlockParser::GetNodeStringFromHandle", IsThreadSafe = true)]
        internal static extern string GetNodeStringFromHandle(SyntaxTree tree, NodeHandle node);
        internal static string GetNodeString(SyntaxTree tree, ISyntaxNode node)
        {
            return GetNodeStringFromHandle(tree, node.handle);
        }
    }
}
