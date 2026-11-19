// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class AssetStoreInfo
    {
        [SerializeField]
        [NativeName("productId")]
        private string m_ProductId;

        public AssetStoreInfo() : this("") {}

        public AssetStoreInfo(string productId)
        {
            m_ProductId = productId;
        }

        public string productId { get { return m_ProductId;  } }
    }
}
