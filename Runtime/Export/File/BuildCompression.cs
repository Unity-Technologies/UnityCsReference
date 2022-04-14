// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine
{
    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
    }

    public enum CompressionLevel
    {
        None,
        Fastest,
        Fast,
        Normal,
        High,
        Maximum,
    }

    [Serializable]
    [UsedByNativeCode]
    public partial struct BuildCompression
    {
        public static readonly BuildCompression Uncompressed = new BuildCompression(CompressionType.None, CompressionLevel.Maximum, 128 * 1024);
        public static readonly BuildCompression LZ4 = new BuildCompression(CompressionType.Lz4HC, CompressionLevel.Maximum, 128 * 1024);
        public static readonly BuildCompression LZMA = new BuildCompression(CompressionType.Lzma, CompressionLevel.Maximum, 128 * 1024);

        //Supported [[BuildCompression]] modes for runtime recompression of [[AssetBundles]] in AssetBundle.RecompressAssetBundleAsync
        public static readonly BuildCompression UncompressedRuntime = Uncompressed;
        public static readonly BuildCompression LZ4Runtime = new BuildCompression(CompressionType.Lz4, CompressionLevel.Maximum, 128 * 1024);

        [NativeName("compression")]
        private CompressionType _compression;
        public CompressionType compression
        {
            get { return _compression; }
            private set { _compression = value; }
        }

        [NativeName("level")]
        private CompressionLevel _level;
        public CompressionLevel level
        {
            get { return _level; }
            private set { _level = value; }
        }

        [NativeName("blockSize")]
        private uint _blockSize;
        public uint blockSize
        {
            get { return _blockSize; }
            private set { _blockSize = value; }
        }

        //Custom versions of this struct are not currently supported
        private BuildCompression(CompressionType in_compression, CompressionLevel in_level, uint in_blockSize) : this()
        {
            compression = in_compression;
            level = in_level;
            blockSize = in_blockSize;
        }
    }
}
