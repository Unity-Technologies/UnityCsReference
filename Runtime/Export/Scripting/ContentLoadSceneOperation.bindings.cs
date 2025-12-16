// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace UnityEngine.Loading
{
    /// <summary>
    /// Represents a scene loading operation initiated by
    /// <see cref="SceneManagement.SceneManager.LoadSceneAsync(LoadableScene, SceneManagement.LoadSceneParameters)"/>.
    /// This class extends AsyncOperation to provide access to the loaded Scene.
    /// </summary>
    /// <remarks>
    /// ContentLoadSceneOperation can be yielded in a coroutine or awaited to
    /// track the asynchronous loading progress. Once the operation completes,
    /// use <see cref="GetScene"/> to retrieve the loaded Scene object.
    /// </remarks>
    /// <seealso cref="AsyncOperation"/>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/ContentLoad/ContentLoadSceneOperation.h")]
    /*UCBP-PUBLIC*/ internal sealed class ContentLoadSceneOperation : AsyncOperation
    {
        internal ContentLoadSceneOperation() { }

        private ContentLoadSceneOperation(IntPtr ptr) : base(ptr)
        { }

        /// <summary>
        /// Retrieves the Scene that was loaded by this operation.
        /// </summary>
        /// <returns>
        /// The Scene object that was loaded. Check Scene.isLoaded to verify the scene is ready.
        /// </returns>
        public extern Scene GetScene();

        new internal static class BindingsMarshaller
        {
            public static ContentLoadSceneOperation ConvertToManaged(IntPtr ptr) => new ContentLoadSceneOperation(ptr);
            public static IntPtr ConvertToUnmanaged(ContentLoadSceneOperation op) => op.m_Ptr;
        }
    }
}
