// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Build.Content
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("UnityEditor.Build.Content.CompressionType has been deprecated. Use UnityEngine.CompressionType instead (UnityUpgradable) -> [UnityEngine] UnityEngine.CompressionType", true)]
    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("UnityEditor.Build.Content.CompressionLevel has been deprecated. Use UnityEngine.CompressionLevel instead (UnityUpgradable) -> [UnityEngine] UnityEngine.CompressionLevel", true)]
    public enum CompressionLevel
    {
        None,
        Fastest,
        Fast,
        Normal,
        High,
        Maximum,
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("UnityEditor.Build.Content.BuildCompression has been deprecated. Use UnityEngine.BuildCompression instead (UnityUpgradable) -> [UnityEngine] UnityEngine.BuildCompression", true)]
    public partial struct BuildCompression
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DefaultUncompressed has been deprecated. Use Uncompressed instead (UnityUpgradable) -> [UnityEngine] UnityEngine.BuildCompression.Uncompressed", true)]
        public static readonly BuildCompression DefaultUncompressed;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DefaultLZ4 has been deprecated. Use LZ4 instead (UnityUpgradable) -> [UnityEngine] UnityEngine.BuildCompression.LZ4", true)]
        public static readonly BuildCompression DefaultLZ4;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DefaultLZMA has been deprecated. Use LZMA instead (UnityUpgradable) -> [UnityEngine] UnityEngine.BuildCompression.LZMA", true)]
        public static readonly BuildCompression DefaultLZMA;
    }
}
