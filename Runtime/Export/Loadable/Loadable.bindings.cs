// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Loading;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    [NativeHeader("Runtime/BaseClasses/Loadable.bindings.h")]
    internal sealed class LoadableManager
    {
        internal extern static ContentLoadOperation LoadObjectAsyncFromRef(in LoadableReference loadableReference);
        internal extern static void ReleaseObjectFromRef(in LoadableReference loadableReference);
        internal extern static void SyncAllOperations();

        // This is needed because the Release function is async but doesn't return any type of operation.
        // https://jira.unity3d.com/browse/CBD-1579
        // Mostly used in tests so that we can assert behavior after releasing
        internal static void ReleaseObjectFromRefSync(in LoadableReference loadableReference)
        {
            ReleaseObjectFromRef(loadableReference);
            SyncAllOperations();
        }
    }
}
