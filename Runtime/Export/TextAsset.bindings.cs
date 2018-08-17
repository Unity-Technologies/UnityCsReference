// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Scripting/TextAsset.h")]
    public class TextAsset : Object
    {
        // Used by MonoScript constructor to avoid creating native TextAsset object.
        internal enum CreateOptions
        {
            None = 0,
            CreateNativeObject = 1
        }

        // The text contents of the .txt file as a string. (RO)
        public extern string text { get; }

        // The raw bytes of the text asset. (RO)
        public extern byte[] bytes { get; }

        public override string ToString() { return text; }

        public TextAsset() : this(CreateOptions.CreateNativeObject, null)
        {
        }

        public TextAsset(string text) : this(CreateOptions.CreateNativeObject, text)
        {
        }

        internal TextAsset(CreateOptions options, string text)
        {
            if (options == CreateOptions.CreateNativeObject)
            {
                Internal_CreateInstance(this, text);
            }
        }

        extern static void Internal_CreateInstance([Writable] TextAsset self, string text);
    }
}
