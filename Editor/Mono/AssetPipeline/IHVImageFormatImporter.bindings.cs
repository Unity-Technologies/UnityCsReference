// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("DDSImporter is obsolete. Use IHVImageFormatImporter instead (UnityUpgradable) -> IHVImageFormatImporter", true)]
    [NativeClass(null)]
    public sealed class DDSImporter : AssetImporter
    {
        public bool isReadable { get {return false; } set {} }
    }

    [NativeHeader("Editor/Src/AssetPipeline/TextureImporting/IHVImageFormatImporter.h")]
    public sealed class IHVImageFormatImporter : AssetImporter
    {
        public extern bool isReadable
        {
            get;
            set;
        }

        public extern FilterMode filterMode
        {
            get;
            set;
        }

        // note: wrapMode getter returns U wrapping axis
        public extern TextureWrapMode wrapMode
        {
            [NativeName("GetWrapU")]
            get;
            [NativeName("SetWrapUVW")]
            set;
        }

        [NativeName("WrapU")]
        public extern TextureWrapMode wrapModeU
        {
            get;
            set;
        }

        [NativeName("WrapV")]
        public extern TextureWrapMode wrapModeV
        {
            get;
            set;
        }

        [NativeName("WrapW")]
        public extern TextureWrapMode wrapModeW
        {
            get;
            set;
        }

        [NativeConditional("ENABLE_TEXTURE_STREAMING")]
        public extern bool streamingMipmaps
        {
            get;
            set;
        }

        [NativeConditional("ENABLE_TEXTURE_STREAMING")]
        public extern int streamingMipmapsPriority
        {
            get;
            set;
        }
    }
}
