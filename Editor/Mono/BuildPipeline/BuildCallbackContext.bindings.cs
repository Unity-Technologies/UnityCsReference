// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.Build.Reporting;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.Build
{
    ///<summary>Get a BuildCallbackContext object from a <see cref="Build.IPreprocessBuildWithContext.OnPreprocessBuild" /> or <see cref="Build.IPostprocessBuildWithContext.OnPostprocessBuild" /> callback.</summary>
    [RequiredByNativeCode]
    [NativeHeader("Modules/ContentBuild/Editor/Public/BuildCallbackContext.h")]
    [NativeClass("BuildPipeline::BuildCallbackContext")]
    public class BuildCallbackContext
    {
        // The bindings generator is setting the instance pointer in this field
        internal IntPtr m_Self;

        internal static class BindingsMarshaller // IS THIS NEEDED ??
        {
            public static IntPtr ConvertToNative(BuildCallbackContext ctx) => ctx?.m_Self ?? IntPtr.Zero;

            public static BuildCallbackContext ConvertToManaged(IntPtr ptr) =>
                ptr != IntPtr.Zero ? new BuildCallbackContext(ptr) : null;
        }

        // Constructor used for wrapping native instances
        private BuildCallbackContext(IntPtr nativePtr)
        {
            m_Self = nativePtr;
        }

        [FreeFunction("BuildCallbackContextBindings::GetReport")]
        private static extern BuildReport GetReportInternal(IntPtr self);

        ///<summary>The build report associated with this build.</summary>
        public BuildReport Report
        {
            get
            {
                if (m_Self != IntPtr.Zero)
                {
                    return GetReportInternal(m_Self);
                }
                return null;
            }
        }

        [FreeFunction("BuildCallbackContextBindings::IsPlayerBuild")]
        private static extern bool IsPlayerBuildInternal(IntPtr self);

        ///<summary>Returns true if the build is a player build type.</summary>
        public bool IsPlayerBuild
        {
            get
            {
                if (m_Self != IntPtr.Zero)
                {
                    return IsPlayerBuildInternal(m_Self);
                }
                return false;
            }
        }

        [FreeFunction("BuildCallbackContextBindings::IsContentOnlyBuild")]
        private static extern bool IsContentOnlyBuildInternal(IntPtr self);

        ///<summary>Returns true if the build is a content only build type like an AssetBundle build.</summary>
        public bool IsContentOnlyBuild
        {
            get
            {
                if (m_Self != IntPtr.Zero)
                {
                    return IsContentOnlyBuildInternal(m_Self);
                }
                return false;
            }
        }
    }
}
