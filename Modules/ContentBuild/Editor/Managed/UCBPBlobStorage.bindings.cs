// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

[NativeHeader("Modules/ContentBuild/Editor/Ucbp/UCBPStorageBackend.h")]
internal class UCBPBlobStorage
{
    [FreeFunction("BuildPipeline::UCBPBlobStorage_GetContentSize", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
    public extern static ulong GetContentSize(Hash128 contentHash);

    [FreeFunction("BuildPipeline::UCBPBlobStorage_GetContentBytes", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
    public extern static byte[] GetContentBytes(Hash128 contentHash);

    [FreeFunction("BuildPipeline::UCBPBlobStorage_GetContentVFSPath", IsFreeFunction = true, ThrowsException = false, IsThreadSafe = true)]
    public extern static string GetVFSPathForContentHash(Hash128 contentHash);
}
