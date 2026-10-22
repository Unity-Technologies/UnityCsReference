// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Implement this interface to run code after a build that used a specific <see cref="BuildProfile"/>.
    /// </summary>
    /// <remarks>
    /// This is the per-profile equivalent of <see cref="Build.IPostprocessBuildWithContext"/>. An
    /// <see cref="Build.IPostprocessBuildWithContext"/> callback runs for every build. An
    /// <see cref="IBuildProfilePostprocessBuild"/> callback runs only when the build profile it's attached to
    /// is the active build profile (see <see cref="BuildProfile.GetActiveBuildProfile"/>). Builds that target
    /// a classic platform, with no active build profile, don't invoke these callbacks.
    ///
    /// Like <see cref="Build.IPostprocessBuildWithContext"/>, these callbacks run for player, AssetBundle, and
    /// content directory builds, and they also run when the build fails or is cancelled. To act only on some
    /// builds, check <see cref="BuildCallbackContext.IsPlayerBuild"/> or the result in
    /// <see cref="BuildCallbackContext.Report"/>.
    ///
    /// Implement the interface on a plain, serializable class. The class doesn't need to derive from
    /// <see cref="UnityEngine.ScriptableObject"/>. To attach callbacks to a build profile, set
    /// <see cref="BuildProfilePostprocessBuild.callbacks"/> on the profile's
    /// <see cref="BuildProfilePostprocessBuild"/> component. The callback instances, including their
    /// serialized fields, are saved with the build profile.
    ///
    /// The callbacks attached to a profile run as one group, in the order of the
    /// <see cref="BuildProfilePostprocessBuild.callbacks"/> array, starting at index 0. To change the order,
    /// reorder the callbacks in the profile's Post-process Build section, or assign a reordered array.
    /// The group runs at the same point as an <see cref="Build.IPostprocessBuildWithContext"/> callback with a
    /// <c>callbackOrder</c> of <c>int.MaxValue</c>, so it runs after all <see cref="Build.IPostprocessBuildWithReport"/>
    /// callbacks and after <see cref="Build.IPostprocessBuildWithContext"/> callbacks with a lower order.
    /// The order relative to <see cref="Build.IPostprocessBuildWithContext"/> callbacks that also use
    /// <c>int.MaxValue</c> isn't defined.
    /// </remarks>
    /// <example>
    ///  <code><![CDATA[
    /// // Defines a post-build callback and a menu item that attaches it to a build profile.
    /// // Save this script in an Editor folder and set k_ProfilePath to the path of your build profile.
    /// using System;
    /// using UnityEditor;
    /// using UnityEditor.Build;
    /// using UnityEditor.Build.Profile;
    /// using UnityEngine;
    ///
    /// [Serializable]
    /// class MyProfilePostprocessBuild : IBuildProfilePostprocessBuild
    /// {
    ///     public void OnPostprocessBuild(BuildCallbackContext ctx)
    ///     {
    ///         Debug.Log($"Built with this profile at {ctx.Report.summary.outputPath}");
    ///     }
    /// }
    ///
    /// static class AttachProfilePostprocessBuild
    /// {
    ///     const string k_ProfilePath = "Assets/Settings/Build Profiles/MyProfile.asset";
    ///
    ///     [MenuItem("Tools/Attach Post-build Callback")]
    ///     static void Attach()
    ///     {
    ///         var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(k_ProfilePath);
    ///
    ///         // A build profile holds one component of each type, so reuse the component if it exists.
    ///         var component = profile.GetComponent<BuildProfilePostprocessBuild>();
    ///         if (component == null)
    ///         {
    ///             component = ScriptableObject.CreateInstance<BuildProfilePostprocessBuild>();
    ///             profile.AddComponent(component);
    ///         }
    ///
    ///         // Assigning the array replaces any callbacks already attached.
    ///         component.callbacks = new IBuildProfilePostprocessBuild[] { new MyProfilePostprocessBuild() };
    ///
    ///         // Mark the component dirty so the change is saved with the profile.
    ///         EditorUtility.SetDirty(component);
    ///         AssetDatabase.SaveAssetIfDirty(profile);
    ///     }
    /// }
    ///  ]]></code>
    /// </example>
    /// <seealso cref="Build.IPostprocessBuildWithContext"/>
    public interface IBuildProfilePostprocessBuild
    {
        /// <summary>
        /// Called after a build that used the build profile this callback is attached to.
        /// </summary>
        /// <param name="ctx">
        /// A <see cref="BuildCallbackContext"/> with information about the build, such as its build report.
        /// </param>
        void OnPostprocessBuild(BuildCallbackContext ctx);
    }
}
