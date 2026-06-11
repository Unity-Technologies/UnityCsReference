// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.SceneManagement
{
    /// <summary>
    /// Description of a sub scene.
    /// </summary>
    [NativeHeader("Runtime/SceneManager/SubSceneDescription.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SubSceneDescription
    {
        /// <summary>
        /// The scene asset GUID of the sub scene.
        /// </summary>
        public GUID SceneGuid { get; set; }
    }
}
