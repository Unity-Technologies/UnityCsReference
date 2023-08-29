// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Scripting/TextAsset.h")]
    public partial class TextAsset : Object
    {
        // The raw bytes of the text asset. (RO)
        public extern byte[] bytes { [return:Unmarshalled] get; }

        [return: Unmarshalled]
        extern byte[] GetPreviewBytes(int maxByteCount);

        extern static void Internal_CreateInstance([Writable] TextAsset self, string text);

        extern IntPtr GetDataPtr();
        extern long GetDataSize();

        static extern AtomicSafetyHandle GetSafetyHandle(TextAsset self);
    }
}
