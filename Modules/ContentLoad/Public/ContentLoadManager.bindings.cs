// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Loading
{


    /// <summary>
    /// The ContentLoadManager offers APIs for accessing content that has been built.  It is primarily used to registered extra content directories and access
    /// root content.
    /// </summary>
    /// <remarks>
    /// In the Editor this is normally not used, because the content is available directly in the project using [[AssetDatabase]] and [[EditorSceneManager]] calls.
    /// However, it can be useful in playmode or automated tests for the purpose of trying out the result of a build.
    ///
    /// Only content built using [[BuildPipeline.BuildContentDirectory]] can be accessed by the Editor.  E.g. content built as part of
    /// [[BuildPipeline.BuildPlayer]] cannot be loaded by the Editor.
    /// </remarks>
    /// <seealso cref="Loadable{T}"/>
    /// <seealso cref="LoadableScene"/>
    [ExcludeFromDocs]
    [NativeHeader("Modules/ContentLoad/Public/ContentLoadManager.bindings.h")]
    [StaticAccessor("ContentLoad", StaticAccessorType.DoubleColon)]
    internal static partial class ContentLoadManager
    {


        // For test and internal usage
        // This method loads the BuildManifest, which describe the content available inside a Content Directory.
        [ThreadSafe]
        internal static extern BuildManifest LoadBuildManifest(string path);
    }
}
