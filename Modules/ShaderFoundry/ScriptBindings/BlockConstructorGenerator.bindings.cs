// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Linker/Utilities/BlockConstructorGenerator.h")]
    internal struct BlockConstructorGeneratorInternal
    {
        internal struct ScriptingResult
        {
            public FoundryHandle functionHandle;
            public bool hasErrors;
        }
        // TODO @ SHADERS: Improve error handling. This currently just propagates back a bool as we don't have any bound error data yet.
        [NativeMethod(IsThreadSafe = true)] extern public ScriptingResult Build(ShaderContainer container, Span<FoundryHandle> fieldHandles);
    }

    internal class BlockConstructorGenerator
    {
        public readonly ShaderContainer container;
        public bool hasErrors = false;
        public BlockConstructorGenerator(ShaderContainer container)
        {
            this.container = container;
        }
        internal FoundryHandle BuildFunction(Span<FoundryHandle> fieldHandles)
        {
            BlockConstructorGeneratorInternal generator = new BlockConstructorGeneratorInternal();
            var result = generator.Build(container, fieldHandles);
            hasErrors = result.hasErrors;
            return result.functionHandle;
        }
    }
}
