// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public sealed partial class EditorSceneManager : SceneManager
    {
        [Obsolete("EditorSceneManager.loadedSceneCount has been deprecated. Please use SceneManager.loadedSceneCount (UnityUpgradable) -> [UnityEngine] UnityEngine.SceneManagement.SceneManager.loadedSceneCount")]
        new public static int loadedSceneCount
        {
            get => SceneManager.loadedSceneCount;
        }
    }
}
