// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Profile;

/// <summary>
/// Enum for predefined verbage when defining package
/// owned footer actions. For any given package, any 
/// specific enum can only be used once.
/// 
/// Usage is currently limited to the platform SDK.
/// </summary>
public enum BuildProfileActionLabel
{
    Build,
    BuildAndRun,
    CloudBuild,
    Deploy
}

/// <summary>
/// The public interface for defining package owned 
/// footer actions. Stored internally as a 
/// BuildProfileWindowAction and displays as a footer button.
/// 
/// Usage is currently limited to the platform SDK.
/// </summary>
/// <code>
/// class MyBuildAction : IBuildProfileWindowAction
/// {
///     public BuildProfileActionLabel GetDisplayName() => BuildProfileActionLabel.BuildAndRun;
/// 
///     public bool IsClickable(BuildProfile profile) => true;
/// 
///     public void OnClick(BuildProfile profile)
///     {
///         UnityEngine.Debug.Log("Building this profile");
///     }
/// }
/// </code>
public interface IBuildProfileWindowAction
{
    /// <summary>
    /// The display name for the action as a BuildProfileActionLabel.
    /// </summary>
    public BuildProfileActionLabel GetDisplayName();

    /// <summary>
    /// Callback determining whether or not a footer action is clickable or not.
    /// </summary>
    public bool IsClickable(BuildProfile profile);

    /// <summary>
    /// Callback describing an OnClick action for the footer action button.
    /// </summary>
    public void OnClick(BuildProfile profile);
}
