// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Build.Player;

namespace UnityEditor.Build.Content
{
    ///<summary>Build options for content.</summary>
    ///<remarks>Note: this enum and its values exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Flags]
    public enum ContentBuildFlags
    {
        ///<summary>Build content with no additional options.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ContentBuildFlags" />.</remarks>
        None = 0,
        ///<summary>Do not include type information within the built content.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ContentBuildFlags" />.</remarks>
        DisableWriteTypeTree = 1 << 0,
        ///<summary>Build Flag to indicate the Unity Version should not be written to the serialized file.</summary>
        StripUnityVersion = 1 << 1,
        ///<summary>Build a development version of the content files.</summary>
        DevelopmentBuild = 1 << 2,
        /// <summary>
        /// Build flag to indicate that TypeTree data is to be stripped from the serialized files and saved separately.  The file path of the extracted data is recorded in the UnityEditor.Build.Content.WriteResult in the field 'extractedTypeTreeDataPath'.
        /// </summary>
        ExtractTypeTree = 1 << 3,
        ///<summary>Suppress the error reported when a LoadableObjectId or LoadableSceneId is encountered while writing content.</summary>
        ///<remarks>Use this when migrating between build pipeline backends when assets legitimately have Loadable references, but the same content is also built through the legacy Player or AssetBundle pipelines. The references still resolve to null in the resulting content; this flag only silences the error log to keep the build usable.</remarks>
        SuppressLoadableErrors = 1 << 4,
    }

    ///<summary>Struct containing information on how to build content.</summary>
    ///<remarks>Note: this struct and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildSettings
    {
        [NativeName("typeDB")]
        internal TypeDB m_TypeDB;
        ///<summary>Type information to use for building content.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildSettings" />.</remarks>
        public TypeDB typeDB
        {
            get { return m_TypeDB; }
            set { m_TypeDB = value; }
        }

        [NativeName("target")]
        internal BuildTargetSelection m_Target;
        internal BuildTargetSelection buildTargetSelection
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        ///<summary>Platform target for which content will be built.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildSettings" />.</remarks>
        public BuildTarget target
        {
            get { return m_Target.platform; }
            set { m_Target.platform = value; }
        }

        ///<summary>Platform subtarget for which content will be built.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildSettings" />.</remarks>
        public int subtarget
        {
            get { return m_Target.subTarget; }
            set { m_Target.subTarget = value; }
        }

        [NativeName("group")]
        internal BuildTargetGroup m_Group;
        ///<summary>Platform group for which content will be built.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildSettings" />.</remarks>
        public BuildTargetGroup group
        {
            get { return m_Group; }
            set { m_Group = value; }
        }

        [NativeName("buildFlags")]
        internal ContentBuildFlags m_BuildFlags;
        ///<summary>Specific build options to use when building content.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.BuildSettings" />.</remarks>
        public ContentBuildFlags buildFlags
        {
            get { return m_BuildFlags; }
            set { m_BuildFlags = value; }
        }
    }
}
