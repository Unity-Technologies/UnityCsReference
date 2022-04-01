// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    // This should be deleted once it's possible to construct this in managed
    internal struct PassIdentifierInternal
    {
        public readonly uint SubShaderIndex;
        public readonly uint PassIndex;

        public PassIdentifierInternal(uint subShaderIndex, uint passIndex)
        {
            SubShaderIndex = subShaderIndex;
            PassIndex = passIndex;
        }
    };
}
