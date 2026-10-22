// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Build.Profile;

public class BuildProfilePostprocessBuild : ScriptableObject
{
    [SerializeReference]
    IBuildProfilePostprocessBuild[] m_Callbacks;

    /// <summary>
    /// The callbacks to run after a build that used the owning build profile.
    /// </summary>
    /// <remarks>
    /// The callbacks run in array order, starting at index 0.
    /// </remarks>
    public IBuildProfilePostprocessBuild[] callbacks
    {
        get => m_Callbacks;
        set => m_Callbacks = value;
    }
}
