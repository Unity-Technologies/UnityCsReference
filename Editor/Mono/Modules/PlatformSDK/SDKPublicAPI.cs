// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor;

/// <summary>
/// Interface for Derived platform SDK platform providers to implement.
/// </summary>
public interface IPlatformProvider
{
    /// <summary>
    /// The version of the platform provider.
    /// </summary>
    int version { get; }
}
