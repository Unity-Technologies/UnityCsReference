// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    namespace Shaders
    {
        public enum ChunkEvictionStrategy
        {
            Never,
            Slow,
            Medium,
            Aggressive
        }
    }

    [NativeHeader("Modules/ShaderRuntime/Public/ChunkEvictionStrategy.h")]
    public partial class Shader
    {
        [FreeFunction("ShaderRuntime::GetChunkEvictionStrategy")]
        private extern static Shaders.ChunkEvictionStrategy GetChunkEvictionStrategy();
        [FreeFunction("ShaderRuntime::SetChunkEvictionStrategy")]
        private extern static void SetChunkEvictionStrategy(Shaders.ChunkEvictionStrategy value);

        public static Shaders.ChunkEvictionStrategy chunkEvictionStrategy
        {
            get { return GetChunkEvictionStrategy(); }
            set { SetChunkEvictionStrategy(value); }
        }
    }
}
