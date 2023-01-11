// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetDatabase/Editor/ScriptBindings/CacheServer.bindings.h")]
    [StaticAccessor("CacheServerBindings", StaticAccessorType.DoubleColon)]
    public class CacheServer
    {
        private CacheServer() {}

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException)]
        extern public static void UploadArtifacts(GUID[] guids = null, bool uploadAllRevisions = false); 

        [PreventExecutionInState(AssetDatabasePreventExecution.kImportingAsset, PreventExecutionSeverity.PreventExecution_ManagedException)]
        extern public static void UploadShaderCache();
    }
}
