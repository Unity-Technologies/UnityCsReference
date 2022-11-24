// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System;

namespace UnityEditor
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetOrigin.h")]
    [NativeAsStruct]
    internal class AssetOrigin
    {
        [NativeName("productId")]
        public int productId = 0;

        [NativeName("packageVersion")]
        public string packageVersion = "";

        [NativeName("packageName")]
        public string packageName = "";

        [NativeName("assetPath")]
        public string assetPath = "";

        [NativeName("uploadId")]
        public int uploadId = 0;

        public AssetOrigin(int productId = 0, string packageName = "", string version = "", int uploadId = 0)
        {
            this.productId = productId;
            this.packageVersion = version;
            this.packageName = packageName;
            this.uploadId = uploadId;
        }

        public AssetOrigin() {}
    }
}
