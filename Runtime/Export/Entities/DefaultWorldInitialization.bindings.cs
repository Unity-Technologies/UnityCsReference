// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabase.h")]
    public static partial class DefaultWorldInitialization
    {
        [FreeFunction("AssetDatabase::IsAssetImportWorkerProcess")]
        static extern bool IsAssetImportWorkerProcess();
    }
}
